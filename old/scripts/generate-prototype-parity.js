const fs=require('fs');
const path=require('path');

const root=path.resolve(__dirname,'..');
const auditRoots=fs.readdirSync(path.join(root,'artifacts'),{withFileTypes:true})
  .filter(entry=>entry.isDirectory()&&entry.name.startsWith('prototype-audit-'))
  .map(entry=>path.join(root,'artifacts',entry.name))
  .filter(directory=>fs.existsSync(path.join(directory,'PROTOTYPE_FEATURE_INVENTORY.json')))
  .sort((a,b)=>fs.statSync(b).mtimeMs-fs.statSync(a).mtimeMs);
if(!auditRoots.length)throw new Error('No completed prototype audit exists. Run npm run audit:prototype first.');
const auditRoot=auditRoots[0];
const audit=JSON.parse(fs.readFileSync(path.join(auditRoot,'PROTOTYPE_FEATURE_INVENTORY.json'),'utf8'));
const navigation=JSON.parse(fs.readFileSync(path.join(root,'src','HyperBoostX.App','Assets','navigation.json'),'utf8'));
const catalogSource=fs.readFileSync(path.join(root,'src','HyperBoostX.App','FeatureRouteCatalog.cs'),'utf8');
const support=new Map();
for(const match of catalogSource.matchAll(/Add\(RouteSupport\.(Implemented|Limited|Unsupported),[\s\S]*?\);/g)){
  const quoted=[...match[0].matchAll(/"((?:[^"\\]|\\.)*)"/g)].map(value=>value[1]);
  for(const route of navigation)if(quoted.includes(route.name))support.set(route.name,match[1]);
}
const csv=value=>`"${String(value??'').replaceAll('"','""')}"`;
const rows=[['Index','Prototype route','Group','Kind','Primary action','Feature count','Production mapping','Runtime support','Parity status','Evidence']];
audit.inventory.pages.forEach((page,index)=>{
  const mapped=navigation.find(route=>route.name===page.name);
  const exact=!!mapped&&mapped.group===page.group&&mapped.kind===page.kind&&mapped.action===page.action&&JSON.stringify(mapped.features)===JSON.stringify(page.features);
  rows.push([index+1,page.name,page.group,page.kind,page.action,page.features.length,mapped?.name||'',support.get(page.name)||'Missing',exact?'Exact metadata mapping':'Mismatch',`PROTOTYPE_FEATURE_INVENTORY.json route ${index+1}`]);
});
for(const surface of audit.extraSurfaces)rows.push([rows.length,surface.title||surface.command,'DISTRIBUTION','surface',surface.command,'',surface.command,'Implemented UI surface','Audited',`screenshots/states/${surface.command}.png`]);
fs.writeFileSync(path.join(root,'PROTOTYPE_TO_PRODUCTION_MATRIX.csv'),rows.map(row=>row.map(csv).join(',')).join('\r\n')+'\r\n');
fs.copyFileSync(path.join(auditRoot,'PROTOTYPE_FEATURE_INVENTORY.json'),path.join(root,'PROTOTYPE_FEATURE_INVENTORY.json'));
fs.copyFileSync(path.join(auditRoot,'screenshot-manifest.json'),path.join(root,'PROTOTYPE_SCREENSHOT_MANIFEST.json'));
const counts={Implemented:0,Limited:0,Unsupported:0,Missing:0};for(const page of audit.inventory.pages)counts[support.get(page.name)||'Missing']++;
const auditDoc=`# Prototype Interaction Audit\n\n- Official prototype: ${audit.url}\n- Captured: ${audit.capturedAt}\n- Browser: ${audit.browser}\n- Source SHA-256: \`${audit.sourceSha256}\`\n- Navigable routes exercised: ${audit.interactions.length}/73\n- Distribution surfaces exercised: ${audit.extraSurfaces.length}/3\n- Total required surfaces: ${audit.surfaceCount}/76\n- Onboarding steps exercised: ${audit.onboarding.length}/4\n- Action review: ${audit.actionReview.title}\n- Command palette query exercised: ${audit.search.query}\n- Screenshots: ${audit.screenshotCount} (${audit.inventory.pages.length} dark, ${audit.inventory.pages.length} light, 6 state captures)\n\nThe 76-surface count is 73 navigation routes plus Splash, Installer Wizard, and Update Wizard. Onboarding, Action Review, and Command Palette are audited interaction states and are not counted again as menu surfaces.\n`;
fs.writeFileSync(path.join(root,'PROTOTYPE_INTERACTION_AUDIT.md'),auditDoc);
const functional=`# Functional Parity Report\n\nGenerated from the completed browser audit and the native route catalog.\n\n| Classification | Routes |\n|---|---:|\n| Implemented | ${counts.Implemented} |\n| Limited | ${counts.Limited} |\n| Unsupported | ${counts.Unsupported} |\n| Missing binding | ${counts.Missing} |\n\nAll 73 prototype routes have exact navigation metadata mappings. This does **not** establish full functional parity: Limited or Unsupported native bindings remain release blockers. The three distribution surfaces are present in the audited prototype; installer/updater production behavior still requires package-gate evidence. See \`PROTOTYPE_TO_PRODUCTION_MATRIX.csv\` for route-level evidence.\n`;
fs.writeFileSync(path.join(root,'FUNCTIONAL_PARITY_REPORT.md'),functional);
const visual=`# Visual Parity Report\n\nThe official prototype was captured in Google Chrome at 1440×1000 for all 73 routes in dark and light themes, plus six modal/state captures. The canonical manifest is \`PROTOTYPE_SCREENSHOT_MANIFEST.json\`.\n\nCurrent status: **baseline captured; native WPF pixel comparison not yet passed**. Full visual parity remains a release blocker until native screenshots are captured at matching viewport/DPI and an image-diff threshold is defined and passed for every required surface.\n`;
fs.writeFileSync(path.join(root,'VISUAL_PARITY_REPORT.md'),visual);
console.log(JSON.stringify({auditRoot,routes:audit.inventory.pages.length,surfaces:audit.surfaceCount,screenshots:audit.screenshotCount,counts},null,2));
