import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
const base='http://localhost:5268';
const display=process.env.PHEOC_EBS_HIGHLIGHT_VIEW||'table',artifact=display==='cards'?'egypt-ebs-card-highlight':'egypt-ebs-highlight';
const secrets=JSON.parse(fs.readFileSync(path.join(process.env.APPDATA,'Microsoft/UserSecrets/PHEOC-OMS-PHEOC2-Development/secrets.json'),'utf8').replace(/^\uFEFF/,''));
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:1000}});const errors=[];let reviewing=false,phase=0,loads=0;
page.on('pageerror',e=>{if(reviewing)errors.push(e.message);});page.on('console',m=>{if(reviewing&&m.type()==='error')errors.push(m.text());});
const old='2020-01-01T00:00:00Z',fresh=new Date(Date.now()+60000).toISOString();
const row=(id,imported,date)=>{
 const attributes=`data-signal-id="${id}" data-imported="${imported}" data-received-at="${date}"`;
 return display==='cards'?`<article class="ebs-signal-card" ${attributes}><h3>حدث اختبار بصري ${id}</h3><p>القاهرة</p><button type="button">${imported?'فتح الحدث المستلم':'استقبال الحدث'}</button></article>`:`<tr ${attributes}><td>#${id}</td><td>حدث اختبار بصري ${id}</td><td>القاهرة</td><td><button type="button">${imported?'فتح الحدث المستلم':'استقبال الحدث'}</button></td></tr>`;
};
function fragment(){
 const rows=row(2000000001,true,old)+row(2000000002,phase>=1,phase>=1?fresh:'')+(phase>=1?row(2000000003,true,fresh)+row(2000000004,false,''):'')+(phase>=3?row(2000000005,true,old):'')+(phase>=4?row(2000000006,true,fresh):'')+(phase>=5?row(2000000007,true,fresh):'');
 const content=display==='cards'?`<div class="ebs-signal-grid">${rows}</div>`:`<div class="table-scroll"><table><thead><tr><th>الإشارة</th><th>النوع</th><th>المحافظة</th><th>الاستقبال</th></tr></thead><tbody>${rows}</tbody></table></div>`;
 return `<section class="panel" data-ebs-signals-card data-review-load="${loads+1}" data-rendered-at="${new Date().toISOString()}"><h2>الأحداث المستقبلة — اختبار بصري</h2>${content}</section>`;
}
const highlighted=()=>page.locator('#ebs-signals tr.ebs-received-highlight');
const cell=()=>display==='cards'?page.locator('[data-signal-id="2000000002"]'):page.locator('[data-signal-id="2000000002"] td').first();
const alpha=async()=>cell().evaluate(e=>{const text=getComputedStyle(e).backgroundColor;return text.startsWith('rgba')?Number(text.slice(text.lastIndexOf(',')+1,-1)):1;});
async function refresh(){const previous=loads;await page.evaluate(()=>document.dispatchEvent(new CustomEvent('pheoc:event-received')));await page.waitForFunction(n=>Number(document.querySelector('[data-review-load]')?.dataset.reviewLoad)>n,previous);}
fs.mkdirSync('artifacts/browser',{recursive:true});
try{
 await page.goto(base+'/Account/Login');await page.locator('[name=UserName]').fill(secrets['Bootstrap:UserName']);await page.locator('[name=Password]').fill(secrets['Bootstrap:Password']);await page.locator('button.primary').click();await page.waitForURL(base+'/');
 const success=new Date().toISOString();
 await page.route('**/EgyptEbs/Status',r=>r.fulfill({json:{automatic:true,lastSuccessAt:success,lastError:'',serverUtcNow:new Date().toISOString()}}));
 await page.route('**/EgyptEbs/Signals*',async r=>{await r.fulfill({contentType:'text/html; charset=utf-8',body:fragment()});loads++;});
 reviewing=true;await page.goto(base+'/EgyptEbs?Display='+display);
 await page.locator('[data-signal-id="2000000002"]').waitFor();assert.equal(await highlighted().count(),0);
 phase=1;await refresh();await page.waitForFunction(()=>document.querySelectorAll('.ebs-received-highlight').length===2);
 const started=await page.evaluate(()=>performance.now()),firstAlpha=await alpha();assert(firstAlpha>0.1);
 assert(await page.locator('[data-ebs-signals-card]').evaluate(e=>e.getAnimations().some(a=>a.animationName==='ebs-card-arrival')));
 await page.screenshot({path:'artifacts/browser/'+artifact+'-desktop.png'});
 await page.waitForTimeout(1400);assert((await alpha())<firstAlpha,'The highlight must fade gradually');
 phase=2;await refresh();
 const elapsed=await cell().evaluate(e=>e.getAnimations().find(a=>a.animationName==='ebs-received-fade')?.currentTime);
 assert(elapsed>1200,'Refreshing the table must preserve fade progress');
 await page.waitForFunction(()=>document.querySelectorAll('.ebs-received-highlight').length===0,{},{timeout:7000});
 const duration=await page.evaluate(t=>performance.now()-t,started);assert(duration>=4700&&duration<6500);
 await refresh();assert.equal(await highlighted().count(),0);assert.equal(await page.locator('.ebs-arrival-card').count(),0);
 phase=3;await refresh();assert.equal(await highlighted().count(),0,'An old imported row entering the list must remain silent');
 phase=4;await refresh();await page.locator('[data-signal-id="2000000006"].ebs-received-highlight').waitFor();
 await page.setViewportSize({width:390,height:844});await page.waitForTimeout(300);
 assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
 await page.evaluate(()=>document.querySelector('[data-ebs-signals-card]').scrollIntoView({block:'start'}));await page.screenshot({path:'artifacts/browser/'+artifact+'-mobile.png'});
 await page.emulateMedia({reducedMotion:'reduce'});phase=5;await refresh();
 assert(await page.locator('[data-ebs-signals-card]').evaluate(e=>e.getAnimations().some(a=>a.animationName==='ebs-card-fade')&&getComputedStyle(e).transform==='none'));
 assert.deepEqual(errors,[]);
 const result={baselineSilent:'PASS',receivedRowsOnly:'PASS',cardAnimation:'PASS',gradualFade:'PASS',durationMilliseconds:Math.round(duration),refreshDoesNotRestartFade:'PASS',repeatedDeliverySilent:'PASS',oldImportedRowSilent:'PASS',mobile:'PASS',reducedMotion:'PASS',javascriptErrors:0,mode:'Browser-only mocked list/status responses; no operational events created or source data changed'};
 fs.writeFileSync('artifacts/browser/'+artifact+'-results.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result));
}finally{await browser.close();}
