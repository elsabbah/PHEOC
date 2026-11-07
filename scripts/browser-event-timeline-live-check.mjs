import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
const base='http://localhost:5268';
const settings=JSON.parse(fs.readFileSync(path.join(process.env.APPDATA,'Microsoft/UserSecrets/PHEOC-OMS-PHEOC2-Development/secrets.json'),'utf8').replace(/^\uFEFF/,''));
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});const page=await browser.newPage();const errors=[];let reviewing=false;
page.on('pageerror',e=>{if(reviewing)errors.push(e.message);});page.on('console',m=>{if(reviewing&&m.type()==='error')errors.push(m.text());});
try{
 await page.goto(base+'/Account/Login');await page.locator('[name=UserName]').fill(settings['Bootstrap:UserName']);await page.locator('[name=Password]').fill(settings['Bootstrap:Password']);await page.locator('button.primary').click();await page.waitForURL(base+'/');
 await page.goto(base+'/Events');const href=await page.locator('a[href^="/Events/Details/"]').first().getAttribute('href');assert(href);
 reviewing=true;const response=await page.goto(base+href);assert.equal(response.status(),200);assert(await page.locator('#timeline h2').isVisible());
 assert(await page.locator('#timeline .event-timeline-item').count()>0);assert.equal(await page.locator('#timeline .activity-list').count(),0);
 assert(await page.locator('#timeline time').first().getAttribute('datetime'));assert.deepEqual(errors,[]);
 const result={realEventDetails:200,timeline:'PASS',javascriptErrors:0,mode:'Read-only; no event or audit entries modified'};
 fs.writeFileSync('artifacts/browser/event-timeline-live-results.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result));
}finally{await browser.close();}
