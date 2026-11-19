const {test,expect,chromium}=require('@playwright/test');
const fs=require('fs');
const path=require('path');
const crypto=require('crypto');

const prototypeUrl='https://nexoraid-id.github.io/hyperboostx-v3-prototype/';
const root=path.resolve(__dirname,'..');
const output=path.resolve(process.env.HBX_PROTOTYPE_AUDIT_DIR||path.join(root,'artifacts','prototype-audit'));
const chromePath='C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';

test('inventory and exercise the official interactive prototype',async()=>{
  test.setTimeout(30*60*1000);
  fs.mkdirSync(path.join(output,'screenshots','dark'),{recursive:true});
  fs.mkdirSync(path.join(output,'screenshots','light'),{recursive:true});
  const browser=await chromium.launch({headless:process.env.HBX_AUDIT_HEADED!=='1',executablePath:chromePath});
  const context=await browser.newContext({viewport:{width:1440,height:1000},colorScheme:'dark'});
  const page=await context.newPage();
  const response=await page.goto(prototypeUrl,{waitUntil:'networkidle',timeout:60000});
  expect(response&&response.ok()).toBeTruthy();
  await page.evaluate(()=>localStorage.clear());
  await page.reload({waitUntil:'networkidle'});
  const source=await page.content();
  const inventory=await page.evaluate(()=>({
    title:document.title,
    advertisedModuleCount:document.querySelector('[data-command="search"] span')?.textContent||'',
    pages:pages.map((item,index)=>({...item,index})),
    groups:[...new Set(pages.map(item=>item.group))],
    navigationButtons:[...document.querySelectorAll('#nav button[data-go]')].map(button=>({name:button.textContent.trim(),target:Number(button.dataset.go)})),
    globalControls:[...document.querySelectorAll('header button,header select')].map(element=>({tag:element.tagName,label:element.getAttribute('aria-label')||element.textContent.trim(),command:element.dataset.command||'',target:element.dataset.go||''})),
    extraSurfaces:[...document.querySelectorAll('[data-command="splash"],[data-command="installer"],[data-command="updateWizard"]')].map(element=>({label:element.textContent.trim(),command:element.dataset.command})),
  }));
  const interactions=[];const screenshots=[];
  expect(inventory.pages.length).toBe(73);
  expect(inventory.navigationButtons.length).toBe(73);
  for(const item of inventory.pages){
    await page.evaluate(index=>go(index),item.index);
    await expect(page.locator('main h1')).toHaveText(item.name);
    const state=await page.evaluate(()=>({
      heading:document.querySelector('main h1')?.textContent||'',
      tabs:[...document.querySelectorAll('.tabs button')].map(button=>button.textContent.trim()),
      buttons:[...document.querySelectorAll('main button')].map(button=>({label:button.textContent.trim(),command:button.dataset.command||'',target:button.dataset.go||'',disabled:button.disabled})),
      inputs:[...document.querySelectorAll('main input,main select,main textarea')].map(element=>({tag:element.tagName,type:element.type||'',name:element.name||element.id||'',value:element.type==='password'?'[REDACTED]':element.value,checked:element.checked||false})),
      notices:[...document.querySelectorAll('main .notice,main .empty,main .result')].map(element=>element.textContent.trim()),
    }));
    const safeName=String(item.index+1).padStart(2,'0')+'-'+item.name.toLowerCase().replace(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,'');
    const darkFile=path.join(output,'screenshots','dark',`${safeName}.png`);
    if(!fs.existsSync(darkFile))await page.screenshot({path:darkFile,fullPage:true});screenshots.push(path.relative(output,darkFile));
    interactions.push({index:item.index,name:item.name,group:item.group,expectedAction:item.action,kind:item.kind,...state});
  }
  await page.locator('#themeBtn').click();
  await expect(page.locator('html')).toHaveClass(/light/);
  for(const item of inventory.pages){
    await page.evaluate(index=>go(index),item.index);
    await expect(page.locator('main h1')).toHaveText(item.name);
    const safeName=String(item.index+1).padStart(2,'0')+'-'+item.name.toLowerCase().replace(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,'');
    const lightFile=path.join(output,'screenshots','light',`${safeName}.png`);
    if(!fs.existsSync(lightFile))await page.screenshot({path:lightFile,fullPage:true});screenshots.push(path.relative(output,lightFile));
  }
  await page.selectOption('#mode','Expert');
  await page.evaluate(()=>go(0));
  await expect(page.locator('.tabs button')).toHaveCount(5);
  await page.locator('[data-command="search"]').click();
  await expect(page.locator('#modal')).toBeVisible();
  await page.locator('#cmdInput').fill('Network Profiles');
  const searchResults=await page.locator('#cmdResults button').allTextContents();
  await page.locator('#closeModal').click();
  const stateDir=path.join(output,'screenshots','states');fs.mkdirSync(stateDir,{recursive:true});
  const extra=[];
  await page.evaluate(()=>go(72));
  for(const command of ['splash','installer','updateWizard']){
    const locator=page.locator(`[data-command="${command}"]`).first();
    if(await locator.count()){await locator.click();await expect(page.locator('#modal')).toBeVisible();const file=path.join(stateDir,`${command}.png`);await page.screenshot({path:file,fullPage:true});screenshots.push(path.relative(output,file));extra.push({command,title:await page.locator('#modalTitle').textContent(),body:await page.locator('#modalBody').innerText()});await page.locator('#closeModal').click();}
  }
  await page.evaluate(()=>go(71));
  await page.locator('[data-command="onboarding"]').first().click();
  await expect(page.locator('#modal')).toBeVisible();
  const onboardingFile=path.join(stateDir,'onboarding.png');await page.screenshot({path:onboardingFile,fullPage:true});screenshots.push(path.relative(output,onboardingFile));
  const onboarding=[];for(let step=0;step<4;step++){onboarding.push({title:await page.locator('#modalTitle').textContent(),body:await page.locator('#modalBody').innerText()});if(step<3)await page.locator('#onNext').click();}await page.locator('#closeModal').click();
  await page.evaluate(()=>go(2));
  await page.locator('main input[type="checkbox"]').first().check();await page.locator('[data-command="queuePreview"]').first().click();await expect(page.locator('#modal')).toBeVisible();
  const reviewFile=path.join(stateDir,'action-review.png');await page.screenshot({path:reviewFile,fullPage:true});screenshots.push(path.relative(output,reviewFile));const actionReview={title:await page.locator('#modalTitle').textContent(),body:await page.locator('#modalBody').innerText()};await page.locator('#closeModal').click();
  await page.locator('[data-command="search"]').click();await page.locator('#cmdInput').fill('game');const paletteFile=path.join(stateDir,'command-palette.png');await page.screenshot({path:paletteFile,fullPage:true});screenshots.push(path.relative(output,paletteFile));await page.locator('#closeModal').click();
  const result={schemaVersion:1,url:prototypeUrl,capturedAt:new Date().toISOString(),viewport:{width:1440,height:1000},browser:'Google Chrome via Playwright',httpStatus:response.status(),sourceSha256:crypto.createHash('sha256').update(source).digest('hex'),inventory,interactions,search:{query:'Network Profiles',results:searchResults},extraSurfaces:extra,onboarding,actionReview,surfaceCount:inventory.pages.length+extra.length,screenshotCount:screenshots.length,screenshots};
  fs.writeFileSync(path.join(output,'PROTOTYPE_FEATURE_INVENTORY.json'),JSON.stringify(result,null,2));
  fs.writeFileSync(path.join(output,'screenshot-manifest.json'),JSON.stringify({capturedAt:result.capturedAt,viewport:result.viewport,themeCounts:{dark:inventory.pages.length,light:inventory.pages.length},files:screenshots},null,2));
  await browser.close();
});
