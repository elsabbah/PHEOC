import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
const base='http://localhost:5268';
const settings=JSON.parse(fs.readFileSync(path.join(process.env.APPDATA,'Microsoft/UserSecrets/PHEOC-OMS-PHEOC2-Development/secrets.json'),'utf8').replace(/^\uFEFF/,''));
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:1000}});page.setDefaultNavigationTimeout(90000);page.setDefaultTimeout(60000);
const errors=[];let reviewing=false;page.on('pageerror',e=>{if(reviewing)errors.push(e.message);});page.on('console',m=>{if(reviewing&&m.type()==='error')errors.push(m.text());});
const ids=()=>page.locator('#ebs-signals [data-signal-id]').evaluateAll(rows=>rows.map(r=>r.dataset.signalId));
async function apply(){await Promise.all([page.waitForNavigation({waitUntil:'domcontentloaded'}),page.locator('#ebs-filters button[type=submit]').click()]);}
async function mode(display){await Promise.all([page.waitForNavigation({waitUntil:'domcontentloaded'}),page.locator('[data-ebs-display="'+display+'"]').click()]);}
fs.mkdirSync('artifacts/browser',{recursive:true});
try{
 await page.goto(base+'/Account/Login');await page.locator('[name=UserName]').fill(settings['Bootstrap:UserName']);await page.locator('[name=Password]').fill(settings['Bootstrap:Password']);await page.locator('button.primary').click();await page.waitForURL(base+'/');
 reviewing=true;await page.goto(base+'/EgyptEbs');
 const today=await page.evaluate(()=>new Intl.DateTimeFormat('en-CA',{timeZone:'Africa/Cairo',year:'numeric',month:'2-digit',day:'2-digit'}).format(new Date()));
 const previous=new Date(today+'T12:00:00Z');previous.setUTCDate(previous.getUTCDate()-1);const yesterday=previous.toISOString().slice(0,10);
 assert.equal(await page.locator('[name=From]').inputValue(),yesterday);assert.equal(await page.locator('[name=To]').inputValue(),today);
 const liveRequest=page.waitForRequest(r=>new URL(r.url()).pathname==='/EgyptEbs/Signals');
 await page.evaluate(()=>document.dispatchEvent(new CustomEvent('pheoc:event-received')));
 const liveUrl=new URL((await liveRequest).url());assert.equal(liveUrl.searchParams.get('From'),yesterday);assert.equal(liveUrl.searchParams.get('To'),today);
 await mode('cards');assert.equal(await page.locator('[data-ebs-display=cards]').getAttribute('aria-current'),'true');
 await page.locator('[name=From]').fill('');await page.locator('[name=To]').fill('');await page.locator('[name=PageSize]').selectOption('20');await apply();
 assert.equal(await page.locator('.ebs-signal-card').count(),20);assert.equal(await page.locator('#ebs-signals tbody').count(),0);const first=await ids();
 await mode('table');assert.deepEqual(await ids(),first);assert.equal(await page.locator('[name=From]').inputValue(),'');assert.equal(await page.locator('[name=To]').inputValue(),'');
 await mode('cards');assert.deepEqual(await ids(),first);assert.equal(await page.locator('[name=Display]').inputValue(),'cards');
 await page.locator('.ebs-pagination [rel=next]').click();await page.waitForURL(u=>u.searchParams.get('Page')==='2');assert.equal(await page.locator('.ebs-signal-card').count(),20);
 assert.equal(new URL(page.url()).searchParams.get('Display'),'cards');assert.equal(await page.locator('[name=From]').inputValue(),'');
 await page.locator('h1').click();const refresh=page.waitForRequest(r=>new URL(r.url()).pathname==='/EgyptEbs/Signals');await page.evaluate(()=>document.dispatchEvent(new CustomEvent('pheoc:event-received')));
 const refreshUrl=new URL((await refresh).url());assert.equal(refreshUrl.searchParams.get('Display'),'cards');assert.equal(refreshUrl.searchParams.get('Page'),'2');assert.equal(refreshUrl.searchParams.get('From'),'');
 await page.locator('[name=From]').fill('2026-01-01');await page.locator('[name=To]').fill('2026-12-31');await apply();await mode('table');await mode('cards');
 assert.equal(await page.locator('[name=From]').inputValue(),'2026-01-01');assert.equal(await page.locator('[name=To]').inputValue(),'2026-12-31');
 const action=await page.locator('.ebs-signal-card form').first().getAttribute('action');assert(action.startsWith('/EgyptEbs/Import/'));
 await page.evaluate(()=>document.querySelector('[data-ebs-signals-card]').scrollIntoView({block:'start'}));
 await page.screenshot({path:'artifacts/browser/egypt-ebs-cards-desktop.png',mask:[page.locator('.ebs-signal-description,.ebs-signal-address')],maskColor:'#edf4f6'});
 await page.setViewportSize({width:390,height:844});await page.waitForTimeout(300);assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
 await page.screenshot({path:'artifacts/browser/egypt-ebs-cards-mobile.png',mask:[page.locator('.ebs-signal-description,.ebs-signal-address')],maskColor:'#edf4f6'});assert.deepEqual(errors,[]);
 const result={egyptDefaultDates:'PASS',defaultDatesPinnedDuringRefresh:'PASS',tableCardsSamePageAndData:'PASS',emptyDatesRetained:'PASS',customDatesRetained:'PASS',cardPaginationAndRefresh:'PASS',intakeAction:'PASS',mobile:'PASS',javascriptErrors:0,mode:'Read-only; no events received or settings changed; source text masked in screenshots'};
 fs.writeFileSync('artifacts/browser/egypt-ebs-display-results.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result));
}finally{await browser.close();}
