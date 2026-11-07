'use strict';
(() => {
  const panel = document.querySelector('[data-kpi-source]');
  if (!panel) return;
  const status = panel.querySelector('[data-kpi-source-status]');
  const refresh = panel.querySelector('[data-kpi-refresh]');
  const apply = panel.querySelector('[data-kpi-apply]');
  let source;
  async function load() {
    refresh.disabled = true;
    if (apply) apply.disabled = true;
    status.textContent = 'جارٍ قراءة تواريخ الإشارة المرتبطة...';
    try {
      const response = await fetch(panel.dataset.sourceUrl, {headers: {'Accept': 'application/json'}, cache: 'no-store'});
      if (!response.ok) throw new Error('source unavailable');
      const data = await response.json();
      source = data;
      panel.querySelector('[data-kpi-emergence]').textContent = data.emergence || 'غير متاح في المصدر';
      panel.querySelector('[data-kpi-detection]').textContent = data.detection || 'غير متاح في المصدر';
      panel.querySelector('[data-kpi-notification]').textContent = data.notification || 'غير متاح؛ لا توجد استجابة مقروءة في المصدر';
      panel.querySelector('[data-kpi-response]').textContent = data.firstResponse ? data.firstResponse.createdAtLocal.replace('T', ' ') + ' · #' + data.firstResponse.id : 'غير متاح في المصدر';
      panel.querySelector('[data-kpi-response-state]').textContent = data.firstResponse ? (data.firstResponse.isConfirmed ? 'مؤكدة في المصدر' : 'غير مؤكدة في المصدر؛ تاريخ تسجيل فقط') : 'لا توجد استجابة مقروءة';
      panel.querySelector('[data-kpi-source-notice]').textContent = data.notice;
      panel.querySelector('[data-kpi-source-result]').hidden = false;
      status.textContent = 'تمت قراءة الإشارة #' + data.signalId + ' · ' + new Intl.DateTimeFormat('ar-EG', {timeZone: 'Africa/Cairo', dateStyle: 'short', timeStyle: 'medium'}).format(new Date(data.readAtUtc)) + ' بتوقيت مصر. لم تُحفظ تغييرات.';
      if (apply) apply.disabled = false;
    } catch {
      source = null;
      panel.querySelector('[data-kpi-source-result]').hidden = true;
      status.textContent = 'تعذر جلب تواريخ EgyptEBS. يمكنك إعادة المحاولة أو توثيقها يدوياً؛ التواريخ المحفوظة لم تتغير.';
    } finally { refresh.disabled = false; }
  }
  refresh.addEventListener('click', load);
  apply?.addEventListener('click', () => {
    const form = document.querySelector('[data-kpi-dates]');
    if (!source || !form) return;
    const changes = [['Emergence', source.emergence], ['Detection', source.detection], ['Notification', source.notification]]
      .filter(([name, value]) => value && !form.elements.namedItem(name).value);
    if (!changes.length) {
      status.textContent = 'لم تتغير الحقول: القيم موجودة بالفعل أو غير متاحة في المصدر.';
      return;
    }
    const evidence = form.elements.namedItem('Evidence');
    const combined = [evidence.value.trim(), source.evidence].filter(Boolean).join('\n');
    if (combined.length > evidence.maxLength) {
      status.textContent = 'مصدر التواريخ يتجاوز الحد المتاح للنص. راجع وصف الدليل قبل استخدام التواريخ.';
      return;
    }
    for (const [name, value] of changes) form.elements.namedItem(name).value = value;
    evidence.value = combined;
    status.textContent = 'أُدخلت التواريخ المتاحة ومصدرها دون استبدال القيم الموجودة. الإخطار من أقدم Responses.CreatedAt غير محذوف عند توفره. راجعها ثم اضغط «حفظ قيد التواريخ».';
    form.scrollIntoView({block: 'start', behavior: 'auto'});
  });
  load();
})();
