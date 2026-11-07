'use strict';
(async()=>{
 const form=document.querySelector('[data-field-form]');if(!form||!crypto.subtle)return;
 const subject=form.dataset.subject;const storageKey='pheoc.field.'+subject;const cryptoKey='pheoc.key.'+subject;
 const label=document.querySelector('#draft-status');const consent=document.querySelector('#draft-consent');
 const read=()=>{try{return JSON.parse(localStorage.getItem(storageKey)||'[]').filter(d=>d.expires>Date.now());}catch{return [];}};
 const store=list=>localStorage.setItem(storageKey,JSON.stringify(list));
 let raw=sessionStorage.getItem(cryptoKey);if(!raw){store([]);const bytes=crypto.getRandomValues(new Uint8Array(32));raw=btoa(String.fromCharCode(...bytes));sessionStorage.setItem(cryptoKey,raw);}
 const key=await crypto.subtle.importKey('raw',Uint8Array.from(atob(raw),c=>c.charCodeAt(0)),{name:'AES-GCM'},false,['encrypt','decrypt']);
 function status(){const drafts=read();store(drafts);label.textContent=`مسودات محفوظة في هذا المتصفح: ${drafts.length} · ${navigator.onLine?'متصل':'دون اتصال'}`;}
 form.addEventListener('submit',async event=>{
  if(navigator.onLine&&!consent.checked)return;
  event.preventDefault();if(!consent.checked){label.textContent='للحفظ دون اتصال، فعّل حفظ المسودة محلياً ثم أعد المحاولة.';return;}
  if(!form.checkValidity())return;
  if(navigator.onLine){
   try{
    const response=await fetch(form.action,{method:'POST',body:new FormData(form)});
    if(response.ok&&!new URL(response.url).pathname.startsWith('/Account/')){location.assign(response.url);return;}
    form.querySelectorAll('button').forEach(button=>{button.disabled=false;});
    label.textContent='رفض الخادم الإرسال أو انتهت الجلسة. البيانات باقية للمراجعة؛ لم تُسجل كمُرسلة.';return;
   }catch{/* A failed network request is queued with the same client request ID. */}
  }
  const drafts=read();if(drafts.length>=10){label.textContent='الحد الأقصى 10 مسودات؛ زامن المحفوظ أولاً.';return;}
  const fields=Object.fromEntries(new FormData(form));delete fields.__RequestVerificationToken;
  const iv=crypto.getRandomValues(new Uint8Array(12));const encrypted=await crypto.subtle.encrypt({name:'AES-GCM',iv},key,new TextEncoder().encode(JSON.stringify(fields)));
  drafts.push({expires:Date.now()+86400000,iv:Array.from(iv),data:Array.from(new Uint8Array(encrypted))});store(drafts);form.reset();form.querySelector('[name=ClientRequestId]').value=crypto.randomUUID();form.querySelectorAll('button').forEach(b=>b.disabled=false);status();
 });
 document.querySelector('#sync-drafts').addEventListener('click',async()=>{
  if(!navigator.onLine){status();return;}
  const pending=read();let sent=0;
  while(pending.length){try{
   const entry=pending[0];const plain=await crypto.subtle.decrypt({name:'AES-GCM',iv:new Uint8Array(entry.iv)},key,new Uint8Array(entry.data));const data=JSON.parse(new TextDecoder().decode(plain));
   const body=new URLSearchParams(data);body.set('__RequestVerificationToken',form.querySelector('[name=__RequestVerificationToken]').value);
   const response=await fetch(form.action,{method:'POST',body});if(!response.ok||new URL(response.url).pathname.startsWith('/Account/'))throw new Error('إجراء غير مقبول');pending.shift();store(pending);sent++;
  }catch{label.textContent=`تمت مزامنة ${sent} مسودة. توجد مسودة تحتاج مراجعة الصلاحية أو حالة التكليف؛ لم تُحذف.`;return;}}
  status();if(sent)location.reload();
 });
 document.querySelector('#clear-drafts').addEventListener('click',()=>{store([]);status();});
 addEventListener('online',status);addEventListener('offline',status);status();
})();
