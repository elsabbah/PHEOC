import {chromium} from 'playwright';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
const base='http://localhost:5268';
const settings=JSON.parse(fs.readFileSync(path.join(process.env.APPDATA,'Microsoft/UserSecrets/PHEOC-OMS-PHEOC2-Development/secrets.json'),'utf8').replace(/^\uFEFF/,''));
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
try{
 const page=await browser.newPage({viewport:{width:1440,height:1000}}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
 await page.goto(base+'/Account/Login');await page.locator('[name=UserName]').fill(settings['Bootstrap:UserName']);await page.locator('[name=Password]').fill(settings['Bootstrap:Password']);
 await Promise.all([page.waitForURL(base+'/'),page.locator('button.primary').click()]);
 assert.equal((await page.goto(base+'/Admin/Users')).status(),200);
 const edits=page.locator('a[href^="/Admin/EditPermissions/"]'),deletes=page.locator('a[href^="/Admin/DeleteUser/"]');
 const editCount=await edits.count(),deleteCount=await deletes.count();assert(editCount>0);assert(deleteCount>0);
 fs.mkdirSync('artifacts/browser',{recursive:true});await page.screenshot({path:'artifacts/browser/user-management-live-desktop.png',fullPage:true});
 await edits.first().click();assert(await page.locator('[data-user-permissions-edit]').isVisible());await page.goto(base+'/Admin/Users');
 await deletes.first().click();assert(await page.locator('[data-user-delete]').isVisible());await page.getByRole('link',{name:'إلغاء والعودة للمستخدمين'}).click();
 for(const width of [390,320]){await page.setViewportSize({width,height:844});await page.waitForTimeout(350);assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));await page.screenshot({path:`artifacts/browser/user-management-live-${width}.png`,fullPage:true});}
 assert.deepEqual(errors,[]);
 fs.writeFileSync('artifacts/browser/user-management-live-results.json',JSON.stringify({editCount,deleteCount,errors,readOnly:true,at:new Date().toISOString()},null,2));
 console.log(JSON.stringify({editCount,deleteCount,errors,readOnly:true}));
}finally{await browser.close();}
