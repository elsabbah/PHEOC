'use strict';
(() => {
  const root = document.querySelector('#ebs-reception');
  if (!root) return;
  const status = root.querySelector('#ebs-live-status');
  const signals = document.querySelector('#ebs-signals');
  const format = new Intl.DateTimeFormat('ar-EG', { timeZone: 'Africa/Cairo', dateStyle: 'short', timeStyle: 'medium' });
  let inFlight = false, lastListVersion = Date.parse(root.dataset.listVersion || '') || 0, serverOffset = 0, haveClock = false, queued = false;
  let listRevision = 0, renderedRevision = 0, arrivalDebounce;
  const fadeDuration = 5000;
  const baseline = Date.parse(signals.querySelector('[data-rendered-at]')?.dataset.renderedAt || '');
  const seen = new Map(Array.from(signals.querySelectorAll('[data-signal-id]'), row => [row.dataset.signalId, row.dataset.imported === 'true']));
  const highlights = new Map();
  let cardStarted = -Infinity, expiryTimer;
  function seekAnimations(element, elapsed, subtree = false) {
    for (const animation of element.getAnimations({ subtree })) {
      if (animation.animationName?.startsWith('ebs-')) animation.currentTime = elapsed;
    }
  }
  function paintArrivals() {
    clearTimeout(expiryTimer);
    const now = performance.now();
    for (const [id, started] of highlights) if (now - started >= fadeDuration) highlights.delete(id);
    for (const row of signals.querySelectorAll('[data-signal-id]')) {
      const started = highlights.get(row.dataset.signalId);
      row.classList.toggle('ebs-received-highlight', started !== undefined);
      if (started !== undefined) seekAnimations(row, now - started, true);
    }
    const card = signals.querySelector('[data-ebs-signals-card]');
    const cardActive = now - cardStarted < fadeDuration;
    card?.classList.toggle('ebs-arrival-card', cardActive);
    if (cardActive && card) seekAnimations(card, now - cardStarted);
    const remaining = [...highlights.values(), ...(cardActive ? [cardStarted] : [])].map(started => fadeDuration - (now - started));
    if (remaining.length) expiryTimer = setTimeout(paintArrivals, Math.max(1, Math.min(...remaining)) + 10);
  }
  function showSignals(html) {
    signals.innerHTML = html;
    const now = performance.now();
    let arrivals = 0;
    for (const row of signals.querySelectorAll('[data-signal-id]')) {
      const id = row.dataset.signalId, imported = row.dataset.imported === 'true', previous = seen.get(id);
      const receivedAt = Date.parse(row.dataset.receivedAt || '');
      // An old imported row entering this page's window is not a new receipt.
      if (imported && previous !== true && (previous === false || receivedAt >= baseline)) {
        highlights.set(id, now); arrivals++;
      }
      seen.set(id, previous === true || imported);
    }
    if (arrivals) {
      cardStarted = now;
      document.querySelector('#ebs-arrival-status').textContent = arrivals === 1 ? 'تم استقبال حدث جديد.' : 'تم استقبال ' + arrivals + ' أحداث جديدة.';
    }
    paintArrivals();
  }
  function clock() {
    if (haveClock) root.querySelector('#ebs-clock').textContent = 'الوقت الحالي بتوقيت مصر: ' + format.format(new Date(Date.now() + serverOffset));
  }
  async function get(url) {
    const response = await fetch(url, { cache: 'no-store', headers: { 'X-Requested-With': 'XMLHttpRequest' } });
    if (!response.ok || new URL(response.url).pathname.startsWith('/Account/')) throw new Error('Reception status unavailable');
    return response;
  }
  async function refresh() {
    if (inFlight) { queued = true; return; }
    if (document.hidden) return;
    inFlight = true;
    try {
      const data = await (await get('/EgyptEbs/Status')).json();
      const now = new Date(data.serverUtcNow), last = data.lastSuccessAt ? new Date(data.lastSuccessAt) : null;
      serverOffset = now.valueOf() - Date.now(); haveClock = true; clock();
      root.querySelector('#ebs-last-success').textContent = last ? format.format(last) : 'لم يكتمل فحص ناجح بعد';
      root.querySelector('#ebs-enabled').textContent = data.automatic ? 'مفعّل' : 'متوقف';
      const form = root.querySelector('form');
      // Do not alter a submission or a button currently being used.
      if (!form.contains(document.activeElement) && !form.querySelector('button').disabled) {
        form.querySelector('[name=enabled]').value = data.automatic ? 'false' : 'true';
        form.querySelector('button').textContent = data.automatic ? 'إيقاف الاستقبال التلقائي' : 'تفعيل الاستقبال من الآن';
      }
      const error = root.querySelector('#ebs-error'); error.hidden = !data.lastError; error.textContent = data.lastError;
      const stale = data.automatic && (!last || now - last > 180000);
      status.classList.toggle('notice', stale || !!data.lastError);
      status.textContent = !data.automatic ? 'الاستقبال التلقائي متوقف. حالة الصفحة تتحدث كل 15 ثانية.'
        : data.lastError ? 'تعذر إكمال فحص المصدر؛ سيعيد العامل المحاولة تلقائياً.'
        : stale ? 'لم يُسجل فحص ناجح خلال آخر 3 دقائق؛ راجع حالة العامل والاتصال بالمصدر.'
        : 'الفحص التلقائي يعمل كل دقيقة تقريباً؛ حالة الصفحة تتحدث كل 15 ثانية. نجاح الفحص لا يعني وجود أحداث جديدة.';
      const version = Date.parse(data.lastSuccessAt || '') || 0;
      if ((version !== lastListVersion || listRevision > renderedRevision) && !signals.contains(document.activeElement)) {
        const revision = listRevision;
        const query = new URL(location.href).searchParams;
        // Keep the dates applied when this page opened, including across Egypt's midnight.
        if (!query.has('From') && !query.has('To')) {
          query.set('From', root.dataset.filterFrom || '');
          query.set('To', root.dataset.filterTo || '');
        }
        const response = await get('/EgyptEbs/Signals' + (query.size ? '?' + query.toString() : ''));
        const html = await response.text();
        // A user may have focused an intake button while the request was in flight.
        if (!signals.contains(document.activeElement)) { showSignals(html); lastListVersion = version; renderedRevision = revision; }
      }
    } catch {
      status.classList.add('notice');
      status.textContent = 'تعذر تحديث الشاشة؛ الحالة المعروضة قديمة. ستُعاد المحاولة تلقائياً، وهذا وحده لا يثبت توقف استقبال الخادم.';
    } finally { inFlight = false; if (queued) { queued = false; setTimeout(() => void refresh(), 100); } }
  }
  function received() {
    listRevision++;
    clearTimeout(arrivalDebounce);
    arrivalDebounce = setTimeout(() => void refresh(), 300);
  }
  document.addEventListener('pheoc:event-received', received);
  document.addEventListener('pheoc:events-changed', received);
  document.addEventListener('pheoc:reconnected', received);
  setInterval(() => void refresh(), 15000);
  setInterval(clock, 1000);
  document.addEventListener('visibilitychange', () => { if (!document.hidden) void refresh(); });
  window.addEventListener('online', () => void refresh());
  void refresh();
})();
