'use strict';
(() => {
  const form = document.querySelector('[data-user-create]');
  if (!form) return;
  const role = form.querySelector('[name=Role]');
  const scope = form.querySelector('[name=GovernorateId]');
  const boxes = Array.from(form.querySelectorAll('[name=Permissions]'));
  const restore = form.querySelector('[data-restore-role]');
  const summary = form.querySelector('[data-permission-summary]');
  function update() {
    const administrator = role.value === 'System Administrator';
    if (administrator) scope.value = '';
    scope.disabled = administrator;
    for (const box of boxes) {
      box.disabled = !!scope.value && box.dataset.central === 'true';
      if (box.disabled) box.checked = false;
    }
    restore.disabled = !role.value;
    summary.textContent = 'الصلاحيات المختارة: ' + boxes.filter(box => box.checked).length + ' من ' + boxes.length + (scope.value ? ' · الصلاحيات الإدارية والتدقيق تتطلب نطاقاً مركزياً.' : '');
  }
  function applyDefaults() {
    const defaults = new Set((role.selectedOptions[0]?.dataset.permissions || '').split(','));
    for (const box of boxes) box.checked = defaults.has(box.value);
    update();
  }
  role.addEventListener('change', applyDefaults);
  restore.addEventListener('click', applyDefaults);
  scope.addEventListener('change', update);
  boxes.forEach(box => box.addEventListener('change', update));
  update();
})();

(() => {
 const form=document.querySelector('[data-user-permissions-edit]');if(!form)return;
 const boxes=[...form.querySelectorAll('input[type=checkbox][name=Permissions]')];
 const summary=form.querySelector('[data-edit-permission-summary]');
 const required=new Set(['Events.View',...(form.dataset.self==='true'?['Admin.ManageUsers']:[])]);
 function update(){summary.textContent='الصلاحيات المختارة: '+boxes.filter(b=>b.checked).length+' من '+boxes.length;}
 boxes.forEach(box=>box.addEventListener('change',()=>{if(required.has(box.value)&&!box.checked)box.checked=true;update();}));
 form.querySelector('[data-edit-restore]').addEventListener('click',()=>{const defaults=new Set(form.dataset.defaults.split(','));boxes.forEach(b=>b.checked=!b.disabled&&(defaults.has(b.value)||required.has(b.value)));update();});
 update();
})();
