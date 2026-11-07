import {chromium} from 'playwright';
import fs from 'node:fs';
import assert from 'node:assert/strict';
const base=(process.argv[2]||'').replace(/\/$/,'');assert(base,'An isolated review host URL is required');
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:1000}}),errors=[];
page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});fs.mkdirSync('artifacts/browser',{recursive:true});
try{
 await page.goto(base+'/test-event-alerts/timeline');
 const rows=page.locator('.event-timeline-item');assert.equal(await rows.count(),6);
 assert.deepEqual(await rows.evaluateAll(items=>items.map(i=>i.dataset.timelineId)),['6','5','4','3','2','1']);
 const first=rows.first(),marker=await first.locator('.timeline-marker').boundingBox(),entry=await first.locator('.timeline-entry').boundingBox(),stamp=await first.locator('.timeline-stamp').boundingBox();
 assert(marker.x>entry.x+entry.width,'Markers must sit on the right');assert(stamp.x<entry.x+entry.width/2,'Date rail must sit on the left');
 assert((await first.locator('.timeline-stamp-date').innerText()).includes('2026/11/06'));assert((await first.locator('.timeline-stamp-time').innerText()).includes('12:04'));assert((await first.locator('.timeline-stamp-time').innerText()).includes('م'));
 assert((await rows.nth(1).locator('.timeline-stamp-time').innerText()).includes('ص'));
 assert(await rows.nth(2).evaluate(e=>e.classList.contains('timeline-tone-green')));assert(await rows.nth(3).evaluate(e=>e.classList.contains('timeline-tone-gold')));
 assert.equal(await page.locator('.event-timeline img').count(),0);assert.equal(await page.evaluate(()=>window.__timelineXss),undefined);assert((await rows.last().innerText()).includes('<img'));
 await page.screenshot({path:'artifacts/browser/event-timeline-desktop.png'});
 await page.setViewportSize({width:390,height:844});await page.waitForTimeout(300);assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
 const mobileText=await first.locator('.timeline-entry-content').boundingBox(),mobileDate=await first.locator('.timeline-stamp').boundingBox();assert(mobileDate.y>=mobileText.y+mobileText.height-1);
 await page.screenshot({path:'artifacts/browser/event-timeline-mobile.png'});
 await page.setViewportSize({width:320,height:844});await page.waitForTimeout(100);assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
 await page.emulateMedia({media:'print'});assert.equal(await first.locator('.timeline-entry').evaluate(e=>getComputedStyle(e).boxShadow),'none');
 await page.emulateMedia({media:'screen'});await page.goto(base+'/test-event-alerts/timeline?empty=true');assert.equal(await page.locator('.event-timeline-item').count(),0);assert((await page.locator('main').innerText()).includes('لا توجد إجراءات مسجلة'));
 assert.deepEqual(errors,[]);
 const result={rtlMarkerAndDateRail:'PASS',newestOrderPreserved:'PASS',egyptDateAndAmPm:'PASS',assignmentIcons:'PASS',sourceTextEncoded:'PASS',mobile390And320:'PASS',print:'PASS',emptyState:'PASS',javascriptErrors:0,mode:'Production timeline partial rendered in an isolated host; no operational data changed'};
 fs.writeFileSync('artifacts/browser/event-timeline-results.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result));
}finally{await browser.close();}
