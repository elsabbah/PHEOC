'use strict';
document.querySelector('.menu-toggle')?.addEventListener('click', event => {
  const open = document.querySelector('#sidebar').classList.toggle('open');
  event.currentTarget.setAttribute('aria-expanded', String(open));
});
document.addEventListener('click', event => {
  const nav = document.querySelector('#sidebar');
  if (nav && !nav.contains(event.target) && !event.target.closest('.menu-toggle')) {
    nav.classList.remove('open'); document.querySelector('.menu-toggle')?.setAttribute('aria-expanded', 'false');
  }
});
const segment = location.pathname.split('/')[1] || 'Dashboard';
document.querySelectorAll('[data-nav]').forEach(a => { if(a.dataset.nav === segment) a.classList.add('active'); });
document.querySelectorAll('[data-confirm]').forEach(button => button.addEventListener('click', event => { if (!confirm(button.dataset.confirm)) event.preventDefault(); }));
document.querySelectorAll('[data-print]').forEach(button => button.addEventListener('click', () => window.print()));
document.querySelectorAll('form').forEach(form => form.addEventListener('submit', () => {
  if (!form.checkValidity() || ((form.hasAttribute('data-field-form') || form.hasAttribute('data-sos')) && !navigator.onLine)) return;
  const button = form.querySelector('button[type="submit"], button:not([type])');
  if (button) { setTimeout(() => { button.disabled = true; button.textContent = 'جارٍ الحفظ…'; }, 0); }
}));
if (document.querySelector('[data-wall]')) setInterval(() => location.reload(), 60000);
if(document.querySelector('#connection') && window.signalR) {
  const label = document.querySelector('#connection');
  const hub = new signalR.HubConnectionBuilder().withUrl('/hubs/operations').withAutomaticReconnect().configureLogging(signalR.LogLevel.Error).build();
  hub.on('Changed', () => {
    label.textContent = '● توجد تحديثات جديدة';
    document.dispatchEvent(new CustomEvent('pheoc:events-changed'));
  });
  hub.on('EventReceived', () => {
    document.dispatchEvent(new CustomEvent('pheoc:event-received'));
  });
  hub.onreconnecting(() => { label.textContent = '● جارٍ إعادة الاتصال'; });
  hub.onreconnected(() => { label.textContent = '● متصل · تحديثات مباشرة'; document.dispatchEvent(new CustomEvent('pheoc:reconnected')); });
  hub.onclose(() => { label.textContent = '● التحديث المباشر غير متصل'; });
  hub.start().then(() => { label.textContent = '● متصل · تحديثات مباشرة'; }).catch(() => { label.textContent = '● التحديث المباشر غير متصل'; });
}

if ('serviceWorker' in navigator && window.isSecureContext) navigator.serviceWorker.register('/sw.js').catch(() => {});

document.querySelector('[data-sos]')?.addEventListener('submit', event => { if(!navigator.onLine){event.preventDefault();document.querySelector('#sos-status').textContent='إرسال SOS يتطلب اتصالاً؛ لم يتم إرساله.';} });

document.querySelector('[data-signout]')?.addEventListener('submit', event => {const subject=event.currentTarget.dataset.subject;localStorage.removeItem('pheoc.field.'+subject);sessionStorage.removeItem('pheoc.key.'+subject);});
