import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
const base='http://localhost:5268';
const settings=JSON.parse(fs.readFileSync(path.join(process.env.APPDATA,'Microsoft/UserSecrets/PHEOC-OMS-PHEOC2-Development/secrets.json'),'utf8').replace(/^\uFEFF/,''));
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:1000}});page.setDefaultNavigationTimeout(90000);page.setDefaultTimeout(60000);
const errors=[];let reviewing=false;page.on('pageerror',e=>{if(reviewing)errors.push(e.message);});page.on('console',m=>{if(reviewing&&m.type()==='error')errors.push(m.text());});
fs.mkdirSync('artifacts/browser',{recursive:true});
const ids=()=>page.locator('#ebs-signals [data-signal-id]').evaluateAll(rows=>rows.map(r=>r.dataset.signalId));
async function apply(){await Promise.all([page.waitForNavigation({waitUntil:'domcontentloaded'}),page.locator('#ebs-filters button[type=submit]').click()]);}
try{
 await page.goto(base+'/Account/Login');await page.locator('[name=UserName]').fill(settings['Bootstrap:UserName']);await page.locator('[name=Password]').fill(settings['Bootstrap:Password']);await page.locator('button.primary').click();await page.waitForURL(base+'/');
 reviewing=true;await page.goto(base+'/EgyptEbs?PageSize=20&From=&To=');assert.equal((await ids()).length,20);const first=await ids();
 await page.locator('.ebs-pagination [rel=next]').click();await page.waitForURL(url=>url.searchParams.get('Page')==='2');
 const second=await ids();assert.equal(second.length,20);assert(!second.some(id=>first.includes(id)));assert.equal(await page.locator('[name=PageSize]').inputValue(),'20');
 const target=second[0];await page.locator('#ebs-filters [name=Search]').fill(target);await apply();assert.deepEqual(await ids(),[target]);
 await page.locator('#ebs-filters [name=Search]').fill("%'_no-result-"+Date.now());await apply();assert(await page.locator('#ebs-empty-results').isVisible());
 await page.getByRole('link',{name:'مسح الفلاتر',exact:true}).click();await page.waitForURL(base+'/EgyptEbs');
 const governorate=(await page.locator('#ebs-filters [name=Governorate] option').nth(1).getAttribute('value'));
 await page.locator('[name=Governorate]').selectOption(governorate);await page.locator('[name=PageSize]').selectOption('20');await page.locator('[name=From]').fill('');await page.locator('[name=To]').fill('');await apply();
 const next=page.locator('.ebs-pagination [rel=next]');assert.equal(await next.count(),1);
 const nextUrl=new URL(await next.getAttribute('href'),base);assert.equal(nextUrl.searchParams.get('Governorate'),governorate);assert.equal(nextUrl.searchParams.get('PageSize'),'20');
 await next.click();await page.waitForURL(url=>url.searchParams.get('Page')==='2');assert.equal(await page.locator('[name=Governorate]').inputValue(),governorate);
 const expected=new URL(page.url()).searchParams;const requested=page.waitForRequest(r=>new URL(r.url()).pathname==='/EgyptEbs/Signals');
 await page.locator('h1').click();await page.evaluate(()=>document.dispatchEvent(new CustomEvent('pheoc:event-received')));
 const request=await requested;const sent=new URL(request.url()).searchParams;for(const key of ['Governorate','Receipt','PageSize','Page'])assert.equal(sent.get(key),expected.get(key));
 await page.locator('[name=Receipt]').selectOption('received');await apply();
 assert.equal(await page.locator('[name=Receipt]').inputValue(),'received');assert.equal(new URL(page.url()).searchParams.get('Page'),'1');
 assert.equal(await page.locator('#ebs-signals [data-imported=false]').count(),0);
 await page.screenshot({path:'artifacts/browser/egypt-ebs-browse-desktop.png'});
 await page.setViewportSize({width:390,height:844});await page.waitForTimeout(300);assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
 await page.screenshot({path:'artifacts/browser/egypt-ebs-browse-mobile.png'});assert.deepEqual(errors,[]);
 const result={numberedPaging:'PASS',searchBeyondFirstPage:'PASS',emptyResults:'PASS',clearFilters:'PASS',governorateAndReceipt:'PASS',pagingRetainsFilters:'PASS',liveRefreshRetainsFilters:'PASS',mobile:'PASS',javascriptErrors:0,mode:'Read-only against the current source; no events imported or settings changed'};
 fs.writeFileSync('artifacts/browser/egypt-ebs-browse-results.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result));
}finally{await browser.close();}
