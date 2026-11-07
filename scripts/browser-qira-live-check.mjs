import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
const base=process.env.PHEOC_URL||'http://localhost:5268';
const secrets=JSON.parse(fs.readFileSync(path.join(process.env.APPDATA,'Microsoft/UserSecrets/PHEOC-OMS-PHEOC2-Development/secrets.json'),'utf8').replace(/^\uFEFF/,''));
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
try{
 const context=await browser.newContext({viewport:{width:1440,height:1000}}),page=await context.newPage(),errors=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push({message:m.text(),location:m.location(),url:page.url()});});
 await page.goto(base+'/Account/Login');await page.locator('[name=UserName]').fill(secrets['Bootstrap:UserName']);await page.locator('[name=Password]').fill(secrets['Bootstrap:Password']);
 await Promise.all([page.waitForURL(base+'/'),page.locator('button.primary').click()]);
 for(const url of ['/Admin/Settings','/Qira/Configuration','/Admin/Users']){const response=await page.goto(base+url);assert.equal(response.status(),200,url);}
 assert.equal(await page.locator('[name=Permissions][value="QIRA.Review"]').count(),1);assert.equal(await page.locator('[name=Permissions][value="QIRA.Reassess"]').count(),1);
 await page.goto(base+'/Events');const link=await page.locator('a[href^="/Events/Details/"]').first().getAttribute('href');assert(link);assert.equal((await page.goto(base+link)).status(),200);
 assert.equal(await page.locator('#risk').getByText('التقييم الأولي السريع للمخاطر',{exact:true}).count(),1);assert.equal(await page.locator('[name=Likelihood],[name=Impact]').count(),0);
 const history=await page.locator('a[href^="/Qira/Event/"]').first().getAttribute('href');assert.equal((await page.goto(base+history)).status(),200);assert.equal(await page.locator('html').getAttribute('dir'),'rtl');
 const assessment=page.locator('a[href^="/Qira/Details/"]').first();let liveTree=false;
 if(await assessment.count()){
  assert.equal((await page.goto(base+await assessment.getAttribute('href'))).status(),200);
  await page.waitForFunction(()=>document.querySelector('[data-qira-tree]')?.dataset.ready==='true');
  assert.equal(await page.locator('[data-tree-id]').count(),21);assert.equal(await page.locator('.tree-edge').count(),27);liveTree=true;
 }
 assert.equal((await context.request.get(base+'/js/qira-tree.js')).status(),200);assert.equal((await context.request.get(base+'/css/qira-tree.css')).status(),200);
 await page.setViewportSize({width:390,height:844});assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
 await page.goto(base+'/Qira/Configuration');assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
 await page.waitForTimeout(700);assert.deepEqual(errors,[]);
 fs.mkdirSync('artifacts/browser',{recursive:true});fs.writeFileSync('artifacts/browser/qira-live-results.json',JSON.stringify({status:'passed',liveTree,checks:['Fresh identity login','National settings and new permissions','Real event QIRA integration','Tree assets served','Old matrix controls absent','History and mobile configuration','No browser errors'],errors,at:new Date().toISOString()},null,2));
 console.log('Live QIRA pages passed. No assessment, policy or operational record created.');
}finally{await browser.close();}
