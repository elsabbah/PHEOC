import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
const base='http://localhost:5268',id=process.argv[2]||'239';
const settings=JSON.parse(fs.readFileSync(path.join(process.env.APPDATA,'Microsoft/UserSecrets/PHEOC-OMS-PHEOC2-Development/secrets.json'),'utf8').replace(/^\uFEFF/,''));
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
try{
 const page=await browser.newPage({viewport:{width:1440,height:1000},timezoneId:'America/Los_Angeles'}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
 await page.goto(base+'/Account/Login');await page.locator('[name=UserName]').fill(settings['Bootstrap:UserName']);await page.locator('[name=Password]').fill(settings['Bootstrap:Password']);await Promise.all([page.waitForURL(base+'/'),page.locator('button.primary').click()]);
 assert.equal((await page.goto(base+'/Analytics/Event/'+id)).status(),200);await page.locator('[data-kpi-source-result]').waitFor({state:'visible',timeout:45000});
 const form=page.locator('[data-kpi-dates]');const read=async()=>({emergence:await form.locator('[name=Emergence]').inputValue(),detection:await form.locator('[name=Detection]').inputValue(),notification:await form.locator('[name=Notification]').inputValue(),evidence:await form.locator('[name=Evidence]').inputValue(),excluded:await form.locator('[name=Excluded]').isChecked()});
 const before=await read();await page.locator('[data-kpi-apply]').click();const filled=await read();if(before.notification)assert.equal(filled.notification,before.notification);else{const value=await page.locator("[data-kpi-notification]").innerText();assert.equal(filled.notification,/^[0-9]{4}-[0-9]{2}-[0-9]{2}$/.test(value)?value:"");}assert.equal(filled.excluded,before.excluded);if(before.emergence)assert.equal(filled.emergence,before.emergence);if(before.detection)assert.equal(filled.detection,before.detection);
 const sourceEmergence=await page.locator('[data-kpi-emergence]').innerText(),sourceDetection=await page.locator('[data-kpi-detection]').innerText();
 fs.mkdirSync('artifacts/browser',{recursive:true});for(const width of [1440,390,320]){await page.setViewportSize({width,height:1000});await page.waitForTimeout(350);assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));await page.screenshot({path:`artifacts/browser/kpi-source-live-${width}.png`,fullPage:true});}
 await page.reload();await page.locator('[data-kpi-source-result]').waitFor({state:'visible',timeout:45000});assert.deepEqual(await read(),before);assert.deepEqual(errors,[]);
 fs.writeFileSync('artifacts/browser/kpi-source-live-results.json',JSON.stringify({eventId:id,sourceEmergence,sourceDetection,readOnly:true,errors,checks:['Actual source read','Form-only fill preserves notification and exclusion','Reload proves no writes','1440/390/320 RTL without horizontal overflow','No browser errors'],at:new Date().toISOString()},null,2));console.log(JSON.stringify({eventId:id,sourceEmergence,sourceDetection,readOnly:true,errors}));
}finally{await browser.close();}
