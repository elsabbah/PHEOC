import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const settings=JSON.parse(fs.readFileSync(path.join(process.env.APPDATA,'Microsoft/UserSecrets/PHEOC-OMS-PHEOC2-Development/secrets.json'),'utf8').replace(/^\uFEFF/,''));
const base=process.env.PHEOC_URL||'http://localhost:5268';const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});const errors=[];
const context=await browser.newContext({viewport:{width:1440,height:1000}});const page=await context.newPage();page.on('pageerror',e=>errors.push(e.message));
async function login(p,user,password){await p.goto(base+'/Account/Login');await p.locator('[name=UserName]').fill(user);await p.locator('[name=Password]').fill(password);await p.locator('button.primary').click();await p.waitForTimeout(200);}
async function post(url,fields){return page.evaluate(async({url,fields})=>{const token=document.querySelector('[name=__RequestVerificationToken]').value;const response=await fetch(url,{method:'POST',body:new URLSearchParams({...fields,__RequestVerificationToken:token})});return response.status;},{url,fields});}
try{
 await login(page,settings['Bootstrap:UserName'],settings['Bootstrap:Password']);await page.goto(base+'/Admin/Users');
 const row=page.locator('tbody tr').filter({hasText:'qa.reviewer.'}).first();const name=(await row.locator('td').first().innerText()).trim();const target=(await row.locator('form[action^="/Admin/ResetPassword/"]').getAttribute('action')).split('/').pop();
 const newPassword='Qa!8aA'+crypto.randomBytes(20).toString('hex');assert.equal(await post('/Admin/ResetPassword/'+target,{Password:newPassword}),200);
 const reviewerContext=await browser.newContext();const reviewer=await reviewerContext.newPage();await login(reviewer,name,newPassword);assert.equal(new URL(reviewer.url()).pathname,'/');
 assert.equal(await post('/Admin/DisableUser/'+target,{disabled:'true'}),200);await reviewerContext.clearCookies();await login(reviewer,name,newPassword);assert.equal(new URL(reviewer.url()).pathname,'/Account/Login');assert((await reviewer.locator('.validation').innerText()).includes('تعذر تسجيل الدخول'));
 await page.goto(base+'/Field');assert.equal(await page.locator('[data-sos]').count(),1);assert.equal(await page.locator('[data-field-form]').count(),1);
 await page.goto(base+'/');await page.screenshot({path:'artifacts/browser/dashboard-desktop.png',fullPage:true});await page.setViewportSize({width:390,height:844});assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));await page.screenshot({path:'artifacts/browser/dashboard-mobile.png',fullPage:true});assert.deepEqual(errors,[]);
 console.log('PASS: password reset; reviewer login; account disable; disabled login denied; SOS/field forms; updated desktop/mobile rendering.');
}finally{await browser.close();}
