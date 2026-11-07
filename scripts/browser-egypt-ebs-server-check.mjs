import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
const base='http://localhost:5268';
const secrets=JSON.parse(fs.readFileSync(path.join(process.env.APPDATA,'Microsoft/UserSecrets/PHEOC-OMS-PHEOC2-Development/secrets.json'),'utf8').replace(/^\uFEFF/,''));
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:1000}});
const errors=[];let reviewing=false;
page.on('pageerror',e=>{if(reviewing)errors.push(e.message);});
page.on('console',m=>{if(reviewing&&m.type()==='error')errors.push(m.text());});
fs.mkdirSync('artifacts/browser',{recursive:true});
try{
 await page.goto(base+'/Account/Login');await page.locator('[name=UserName]').fill(secrets['Bootstrap:UserName']);await page.locator('[name=Password]').fill(secrets['Bootstrap:Password']);await page.locator('button.primary').click();await page.waitForURL(base+'/');
 await page.goto(base+'/Admin/Settings');
 await page.getByRole('link',{name:'خادم EgyptEBS',exact:true}).click();reviewing=true;
 assert.equal(new URL(page.url()).pathname,'/EgyptEbsServer');
 const current=await page.locator('#ebs-current-server').innerText();assert(current);
 assert.equal(await page.locator('input[name=Server]').inputValue(),current);
 assert.equal(await page.locator('input[type=password]').inputValue(),'');
 assert(!(await page.content()).includes(secrets['ConnectionStrings:EgyptEbsReader']));
 await page.getByRole('button',{name:'اختبار الاتصال',exact:true}).click();
 await page.getByRole('status').filter({hasText:'نجح الاتصال وقراءة مخطط جداول المصدر'}).waitFor();
 assert.equal(await page.locator('#ebs-current-server').innerText(),current);
 await page.screenshot({path:'artifacts/browser/egypt-ebs-server-desktop.png'});
 if(process.env.PHEOC_EBS_DIAG_TARGET){
   await page.locator('[name=Server]').fill(process.env.PHEOC_EBS_DIAG_TARGET);
   await page.getByRole('button',{name:'اختبار الاتصال',exact:true}).click();
   await page.getByRole('alert').waitFor();
   const diagnostic=await page.getByRole('alert').innerText();
   assert(diagnostic.includes('18456')&&diagnostic.includes('رفض حساب القراءة'),diagnostic);
   assert.equal(await page.locator('#ebs-current-server').innerText(),current);
   await page.screenshot({path:'artifacts/browser/egypt-ebs-auth-failure.png'});
   await page.goto(base+'/EgyptEbsServer');
 }
 await page.locator('[name=Server]').fill('host;Database=other');
 await page.getByRole('button',{name:'اختبار وحفظ العنوان',exact:true}).click();
 await page.getByRole('alert').filter({hasText:'أدخل اسم خادم SQL'}).waitFor();
 assert.equal(await page.locator('#ebs-current-server').innerText(),current);
 await page.goto(base+'/EgyptEbsServer');assert.equal(await page.locator('[name=Server]').inputValue(),current);
 await page.setViewportSize({width:390,height:844});await page.waitForTimeout(300);
 assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
 assert((await page.locator('[type=checkbox][name=UseOriginalAccount]').boundingBox()).width<=24);
 await page.screenshot({path:'artifacts/browser/egypt-ebs-server-mobile.png'});
 assert.deepEqual(errors,[]);
 const result={settingsLink:'PASS',currentServer:'PASS',realReadOnlySqlProbe:'PASS',credentialsNotRendered:'PASS',invalidTargetNotSaved:'PASS',mobile:'PASS',javascriptErrors:0,mode:'No valid target saved and no source data modified; persistence is covered by isolated HTTP/file tests'};
 fs.writeFileSync('artifacts/browser/egypt-ebs-server-results.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result));
}finally{await browser.close();}
