import {chromium} from 'playwright';
import fs from 'node:fs';
import assert from 'node:assert/strict';

const base=(process.argv[2]||'').replace(/\/$/,'');
assert(base,'The isolated review host URL is required.');
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
const context=await browser.newContext({viewport:{width:1440,height:1000},locale:'ar-EG'});
await context.addInitScript(()=>{
  window.__arrivalAudio={starts:0,contexts:[]};
  const Native=window.AudioContext;
  if(Native)window.AudioContext=class extends Native {
    constructor(...args){super(...args);window.__arrivalAudio.contexts.push(this);}
    createOscillator(){const node=super.createOscillator();const start=node.start.bind(node);node.start=(...args)=>{window.__arrivalAudio.starts++;return start(...args);};return node;}
  };
});
const page=await context.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
const consoleErrors=[];page.on('console',message=>{if(message.type()==='error')consoleErrors.push({text:message.text(),url:message.location().url});});
page.on('response',response=>{if(response.status()>=400)console.error('HTTP '+response.status()+' '+response.url());});
fs.mkdirSync('artifacts/browser',{recursive:true});const checks=[];
async function receive(id,title){const response=await context.request.post(base+'/test-event-alerts/receive',{data:{id,title}});assert.equal(response.status(),200);}
try{
  await page.goto(base+'/test-event-alerts/page');
  await page.waitForFunction(()=>document.querySelector('#connection')?.textContent.includes('متصل'));
  await page.waitForFunction(()=>document.querySelector('#alerts-summary')?.textContent==='جميع التنبيهات مقروءة');
  assert(await page.locator('#arrival-toast').isHidden());assert.equal(await page.evaluate(()=>window.__arrivalAudio.starts),0);checks.push('Historical baseline is silent');
  await page.locator('#alerts-sound').click();
  await page.waitForFunction(()=>window.__arrivalAudio.starts===3&&window.__arrivalAudio.contexts.some(c=>c.state==='running'));
  checks.push('A user gesture activates the actual AudioContext and preview chime');
  await receive(1,'حدث جديد للتحقق من تنبيه الوصول');
  await page.waitForFunction(()=>document.querySelector('#alerts-count')?.textContent==='1'&&!document.querySelector('#arrival-toast').hidden);
  await page.waitForFunction(()=>window.__arrivalAudio.starts===6);
  assert(await page.locator('#alerts-toggle').evaluate(e=>e.classList.contains('new-arrival')));checks.push('Real SignalR arrival triggers header count, animation, toast and sound');
  await page.locator('#alerts-toggle').click();assert(await page.locator('#alerts-panel').isVisible());
  assert.equal(await page.locator('#alerts-list a').first().getAttribute('href'),'/Events/Details/1001');
  await page.waitForTimeout(350); // Capture the fully visible card after its entry animation.
  await page.screenshot({path:'artifacts/browser/event-arrival-desktop.png',fullPage:true});
  const started=await page.evaluate(()=>window.__arrivalAudio.starts);await receive(1,'نفس الحدث');await page.waitForTimeout(700);
  assert.equal(await page.locator('#alerts-count').innerText(),'1');assert.equal(await page.evaluate(()=>window.__arrivalAudio.starts),started);checks.push('Repeated delivery does not increment or replay sound');
  await page.locator('#alerts-sound').click();assert.equal(await page.locator('#alerts-sound').getAttribute('aria-pressed'),'false');
  await receive(2,'<img src=x onerror="window.__alertXss=1"> اختبار نص آمن');
  await page.waitForFunction(()=>document.querySelector('#alerts-count')?.textContent==='2');
  await page.waitForTimeout(500);assert.equal(await page.evaluate(()=>window.__arrivalAudio.starts),started);
  assert.equal(await page.evaluate(()=>window.__alertXss),undefined);assert.equal(await page.locator('#arrival-toast img').count(),0);checks.push('Mute preserves visual alerts and titles are rendered as text');
  await page.reload();await page.waitForFunction(()=>document.querySelector('#alerts-count')?.textContent==='2');
  assert(await page.locator('#arrival-toast').isHidden());assert.equal(await page.locator('#alerts-sound').getAttribute('aria-pressed'),'false');checks.push('Unread alerts and mute persist across page navigation');
  await page.setViewportSize({width:390,height:844});await page.locator('#alerts-toggle').click();
  assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
  const box=await page.locator('#alerts-panel').boundingBox();assert(box.x>=0&&box.x+box.width<=390);
  await page.screenshot({path:'artifacts/browser/event-arrival-mobile.png',fullPage:true});checks.push('RTL header and notification panel fit a 390px viewport');
  await page.locator('#alerts-read').click();await page.waitForFunction(()=>document.querySelector('#alerts-count')?.hidden);
  assert(await page.locator('#alerts-empty').isVisible());checks.push('Reading the displayed notifications clears the durable unread count');
  await page.keyboard.press('Escape');assert.equal(await page.locator('#alerts-toggle').getAttribute('aria-expanded'),'false');
  assert.deepEqual(errors,[]);assert.deepEqual(consoleErrors,[]);checks.push('Keyboard dismissal and no JavaScript page or console errors');
  fs.writeFileSync('artifacts/browser/event-arrival-results.json',JSON.stringify({checks,scope:'Isolated Kestrel/SignalR/browser host with in-memory alert storage; SQL persistence is tested separately.'},null,2));
  console.log(JSON.stringify({passed:checks.length,checks}));
}catch(error){await page.screenshot({path:'artifacts/browser/event-arrival-failure.png',fullPage:true});throw error;}
finally{await browser.close();}
