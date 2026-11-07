import {chromium} from 'playwright';
import assert from 'node:assert/strict';
import fs from 'node:fs';
const base=process.argv[2].replace(/\/$/,''),admin=process.env.PHEOC_EDIT_ADMIN,target=process.env.PHEOC_EDIT_TARGET,password=process.env.PHEOC_EDIT_PASSWORD;
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
try{
 const context=await browser.newContext({viewport:{width:1440,height:1000}}),page=await context.newPage(),errors=[],checks=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
 async function login(p,name){await p.goto(base+'/Account/Login');await p.locator('[name=UserName]').fill(name);await p.locator('[name=Password]').fill(password);await Promise.all([p.waitForURL(base+'/'),p.locator('button.primary').click()]);}
 async function save(){await Promise.all([page.waitForNavigation(),page.locator('[data-user-permissions-edit] button[type=submit]').click()]);assert.equal(page.url(),base+'/Admin/Users');}
 const box=p=>page.locator(`input[type=checkbox][name=Permissions][value="${p}"]`);
 await login(page,admin);await page.goto(base+'/Admin/Users');const row=page.locator('tbody tr').filter({has:page.locator('td').filter({hasText:target})});assert.equal(await row.count(),1);
 const edit=await row.locator('a[href^="/Admin/EditPermissions/"]').getAttribute('href');assert(edit);
 const second=await browser.newContext(),targetPage=await second.newPage();await login(targetPage,target);assert.equal((await targetPage.goto(base+'/Reports')).status(),200);
 await page.goto(base+edit);assert.equal(await box('Reports.View').isChecked(),true);assert.equal(await box('QIRA.Review').isChecked(),false);
 await box('QIRA.Review').check();await box('RiskAssessment.Approve').check();await box('Reports.View').uncheck();await save();await page.goto(base+edit);
 assert.equal(await box('QIRA.Review').isChecked(),true);assert.equal(await box('RiskAssessment.Approve').isChecked(),true);assert.equal(await box('Reports.View').isChecked(),false);checks.push('Role and user list retained; permission additions and removals persist after a fresh request');
 await targetPage.goto(base+'/Reports');assert(targetPage.url().includes('/Account/Login'));checks.push('Existing real cookie rejected after security stamp validation');
 await login(targetPage,target);const denied=await targetPage.goto(base+'/Reports');assert.equal(denied.status(),403);checks.push('New login uses the edited permissions');
 const stale=await page.locator('[name=Version]').inputValue();await box('Reports.View').check();await save();await page.goto(base+edit);
 const conflict=await page.evaluate(async version=>{const form=document.querySelector('[data-user-permissions-edit]');const body=new URLSearchParams({Version:version,__RequestVerificationToken:form.querySelector('[name=__RequestVerificationToken]').value});body.append('Permissions','Events.View');const r=await fetch(form.action,{method:'POST',body});return r.status;},stale);assert.equal(conflict,400);await page.reload();assert(await box('QIRA.Review').isChecked());checks.push('Stale form cannot overwrite a more recent permission update');
 await page.locator('[data-edit-restore]').click();assert(await box('RiskAssessment.Create').isChecked());assert.equal(await box('QIRA.Review').isChecked(),false);await page.reload();assert(await box('QIRA.Review').isChecked());checks.push('Role defaults are an explicit unsaved proposal');
 fs.mkdirSync('artifacts/browser',{recursive:true});await page.screenshot({path:'artifacts/browser/edit-user-permissions-desktop.png',fullPage:true});
 for(const width of [390,320]){await page.setViewportSize({width,height:844});await page.waitForTimeout(350);assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));await page.screenshot({path:`artifacts/browser/edit-user-permissions-${width}.png`,fullPage:true});}
 assert.deepEqual(errors,[]);fs.writeFileSync('artifacts/browser/edit-user-permissions-results.json',JSON.stringify({checks,errors,at:new Date().toISOString()},null,2));console.log(JSON.stringify({checks,errors}));
}finally{await browser.close();}
