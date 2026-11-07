'use strict';
(() => {
  const root = document.querySelector('#event-alerts');
  if (!root) return;
  const trigger = root.querySelector('#alerts-toggle');
  const counter = root.querySelector('#alerts-count');
  const panel = root.querySelector('#alerts-panel');
  const list = root.querySelector('#alerts-list');
  const status = root.querySelector('#alerts-status');
  const readButton = root.querySelector('#alerts-read');
  const soundButton = root.querySelector('#alerts-sound');
  const toast = document.querySelector('#arrival-toast');
  const subject = root.dataset.user;
  const preferenceKey = 'pheoc.alerts.sound.' + subject;
  const announcedKey = 'pheoc.alerts.announced.' + subject;
  const readStore = (key, fallback) => { try { return localStorage.getItem(key) ?? fallback; } catch { return fallback; } };
  const writeStore = (key, value) => { try { localStorage.setItem(key, String(value)); } catch { /* Storage may be restricted. */ } };
  let soundEnabled = readStore(preferenceKey, 'off') === 'on';
  let audio = null, pendingSound = false, initialized = false, latest = 0, inFlight = false, queued = false, items = [], hideTimer;
  const formatter = new Intl.DateTimeFormat('ar-EG', { dateStyle: 'short', timeStyle: 'short', timeZone: 'Africa/Cairo' });

  function updateSoundButton() {
    const active = soundEnabled && audio?.state === 'running';
    soundButton.textContent = active ? 'الصوت مفعّل' : soundEnabled ? 'تنشيط الصوت' : 'تفعيل الصوت';
    soundButton.setAttribute('aria-pressed', String(soundEnabled));
    soundButton.title = active ? 'انقر لكتم صوت تنبيه الأحداث' : 'انقر للسماح للمتصفح بتشغيل صوت تنبيه الأحداث';
  }
  function chime() {
    if (!soundEnabled) return;
    if (!audio || audio.state !== 'running') { pendingSound = true; updateSoundButton(); return; }
    pendingSound = false;
    [659.25, 830.61, 987.77].forEach((frequency, index) => {
      const start = audio.currentTime + index * 0.17;
      const oscillator = audio.createOscillator(), gain = audio.createGain();
      oscillator.type = 'sine'; oscillator.frequency.setValueAtTime(frequency, start);
      gain.gain.setValueAtTime(0, start); gain.gain.linearRampToValueAtTime(0.13, start + 0.025);
      gain.gain.exponentialRampToValueAtTime(0.001, start + 0.35);
      oscillator.connect(gain); gain.connect(audio.destination); oscillator.start(start); oscillator.stop(start + 0.37);
      oscillator.onended = () => { oscillator.disconnect(); gain.disconnect(); };
    });
  }
  async function unlockAudio(preview = false) {
    if (!soundEnabled) return;
    const Audio = window.AudioContext || window.webkitAudioContext;
    if (!Audio) { status.textContent = 'الصوت غير مدعوم في هذا المتصفح؛ التنبيه المرئي متاح.'; return; }
    try {
      audio ??= new Audio();
      if (audio.state !== 'running') await audio.resume();
      updateSoundButton();
      if (pendingSound || preview) chime();
    } catch { updateSoundButton(); }
  }
  soundButton.addEventListener('click', () => {
    if (soundEnabled && audio?.state === 'running') { soundEnabled = false; pendingSound = false; }
    else { soundEnabled = true; void unlockAudio(true); }
    writeStore(preferenceKey, soundEnabled ? 'on' : 'off'); updateSoundButton();
  });
  document.addEventListener('pointerdown', event => {
    if (!soundButton.contains(event.target) && soundEnabled && audio?.state !== 'running') void unlockAudio();
  }, { passive: true });
  document.addEventListener('keydown', () => { if (soundEnabled && audio?.state !== 'running') void unlockAudio(); });
  updateSoundButton();

  function openPanel(open) {
    panel.hidden = !open; trigger.setAttribute('aria-expanded', String(open));
    if (open) { void refresh(); root.querySelector('#alerts-close').focus(); }
  }
  trigger.addEventListener('click', () => openPanel(panel.hidden));
  root.querySelector('#alerts-close').addEventListener('click', () => { openPanel(false); trigger.focus(); });
  document.addEventListener('click', event => { if (!root.contains(event.target)) openPanel(false); });
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && !panel.hidden) { openPanel(false); trigger.focus(); }
  });
  function scheduleHide() {
    clearTimeout(hideTimer);
    hideTimer = setTimeout(() => {
      if (toast.matches(':hover') || toast.contains(document.activeElement)) { scheduleHide(); return; }
      toast.hidden = true;
    }, 14000);
  }
  document.querySelector('#arrival-toast-close').addEventListener('click', () => { toast.hidden = true; clearTimeout(hideTimer); });
  const eventUrl = id => /^\d+$/.test(String(id)) ? '/Events/Details/' + id : '/Events';
  async function acknowledge(ids) {
    const token = document.querySelector('#alerts-token [name=__RequestVerificationToken]')?.value;
    const body = new URLSearchParams({ __RequestVerificationToken: token || '' });
    ids.forEach(id => body.append('ids', String(id)));
    const response = await fetch('/EventAlerts/Read', { method: 'POST', body, cache: 'no-store' });
    if (!response.ok || new URL(response.url).pathname.startsWith('/Account/')) throw new Error('Read rejected');
    await refresh();
  }
  function render(feed) {
    items = feed.items || [];
    counter.hidden = feed.unreadCount === 0;
    counter.textContent = feed.unreadCount > 99 ? '99+' : String(feed.unreadCount);
    trigger.classList.toggle('has-unread', feed.unreadCount > 0);
    trigger.setAttribute('aria-label', 'تنبيهات الأحداث الجديدة: ' + feed.unreadCount + ' غير مقروء');
    root.querySelector('#alerts-summary').textContent = feed.unreadCount ? feed.unreadCount + ' تنبيه غير مقروء' : 'جميع التنبيهات مقروءة';
    list.replaceChildren();
    for (const item of items) {
      const li = document.createElement('li'), link = document.createElement('a'), title = document.createElement('strong');
      const location = document.createElement('span'), time = document.createElement('time');
      title.textContent = item.title; location.textContent = item.governorate;
      const date = new Date(item.receivedAtUtc); time.textContent = Number.isNaN(date.valueOf()) ? '' : formatter.format(date);
      time.dateTime = item.receivedAtUtc; link.href = eventUrl(item.eventId); link.append(title, location, time); li.append(link); list.append(li);
      link.addEventListener('click', event => {
        if (event.ctrlKey || event.metaKey || event.shiftKey || event.button !== 0) return;
        event.preventDefault();
        acknowledge([item.id]).catch(() => {}).finally(() => locationAssign(link.href));
      });
    }
    root.querySelector('#alerts-empty').hidden = items.length !== 0;
    readButton.disabled = items.length === 0;
    status.textContent = feed.unreadCount > items.length ? 'تُعرض أحدث 20 رسالة؛ بقية التنبيهات في مركز الإشعارات.' : 'تحديث مباشر مع متابعة تلقائية عند عودة الاتصال.';
  }
  function locationAssign(url) { window.location.assign(url); }
  function announce(feed) {
    const first = items.find(item => item.id > latest);
    document.querySelector('#arrival-toast-heading').textContent = feed.newCount > 1 ? 'وصول ' + feed.newCount + ' أحداث جديدة' : 'وصول حدث جديد';
    document.querySelector('#arrival-toast-text').textContent = first ? first.title + ' — ' + first.governorate : 'توجد أحداث جديدة بانتظار الاطلاع.';
    const link = document.querySelector('#arrival-toast-link'); link.href = first ? eventUrl(first.eventId) : '/Events';
    link.onclick = event => {
      if (!first || event.ctrlKey || event.metaKey || event.shiftKey || event.button !== 0) return;
      event.preventDefault(); acknowledge([first.id]).catch(() => {}).finally(() => locationAssign(link.href));
    };
    toast.hidden = false; toast.classList.remove('show-arrival'); trigger.classList.remove('new-arrival');
    void toast.offsetWidth;
    toast.classList.add('show-arrival'); trigger.classList.add('new-arrival'); scheduleHide();
    const playOnce = () => {
      if (!soundEnabled) return;
      if (Number(readStore(announcedKey, '0')) >= feed.latestId) return;
      writeStore(announcedKey, feed.latestId); chime();
    };
    if (navigator.locks?.request) void navigator.locks.request('pheoc-event-audio-' + subject, playOnce).catch(() => {});
    else playOnce();
  }
  async function refresh() {
    if (inFlight) { queued = true; return; }
    inFlight = true;
    try {
      const response = await fetch('/EventAlerts/Feed?afterId=' + latest, { cache: 'no-store', headers: { Accept: 'application/json' } });
      if (!response.ok || new URL(response.url).pathname.startsWith('/Account/')) throw new Error('Feed unavailable');
      const feed = await response.json();
      render(feed);
      if (initialized && feed.latestId > latest && feed.newCount > 0) announce(feed);
      latest = Math.max(latest, feed.latestId); initialized = true;
    } catch { status.textContent = 'تعذر تحديث التنبيهات؛ ستُعاد المحاولة تلقائياً.'; }
    finally { inFlight = false; if (queued) { queued = false; setTimeout(() => void refresh(), 250); } }
  }
  readButton.addEventListener('click', async () => {
    const ids = items.map(item => item.id); if (!ids.length) return;
    readButton.disabled = true;
    try { await acknowledge(ids); }
    catch { status.textContent = 'تعذر تحديد التنبيهات كمقروءة. حاول مرة أخرى.'; readButton.disabled = false; }
  });
  let debounce;
  const receive = () => { clearTimeout(debounce); debounce = setTimeout(() => void refresh(), 150); };
  document.addEventListener('pheoc:event-received', receive);
  document.addEventListener('pheoc:events-changed', receive);
  document.addEventListener('pheoc:reconnected', receive);
  document.addEventListener('visibilitychange', () => { if (!document.hidden) void refresh(); });
  window.addEventListener('online', receive);
  window.addEventListener('storage', event => { if (event.key === preferenceKey) { soundEnabled = event.newValue === 'on'; updateSoundButton(); } });
  setInterval(() => void refresh(), 30000);
  void refresh();
})();
