import json,re
from pathlib import Path
root=Path(__file__).resolve().parents[1]
catalog=json.loads((root/'docs/registry/runtime.json').read_text(encoding='utf-8-sig'))
legacy=json.loads((root/'docs/audit/legacy-actions.json').read_text(encoding='utf-8'))
rules=[('restore|backup','Restore'),('report|history','Reports'),('startup','Startup'),('network|dns','Network'),('gpu|display','GPU'),('cpu|hardware|rgb|lcd|msi|ram','Hardware'),('process|background|balance','Processes'),('game|gaming|overlay','Gaming'),('creator|stream|voice|mic|camera|webcam','Creator'),('storage|cleanup','Storage'),('privacy|security|defender|protect|reality','Security'),('driver|device','Devices'),('power|battery','Power'),('repair','Repair'),('appsuninstaller|uninstall|appsmanager','Apps'),('knowledge','Knowledge Base'),('setting|about','Settings'),('scan|recommend|score','Smart Scan'),('tweak|visual|performance|boost','Optimize'),('windows|update|service','Windows')]
for item in legacy:
    menu=item['legacy_menu'].lower()
    destination=next((center for pattern,center in rules if re.search(pattern,menu)),'Unimplemented workflow')
    item['proposed_v3_center']=destination
    item['implementation_status']='NOT_STARTED'
    item['mapping_note']='Category destination only; this is not a claim of equivalent execution, verification or rollback.'
    item['v3_action_ids']=[]
(root/'docs/registry/legacy-parity.json').write_text(json.dumps(legacy,indent=2,ensure_ascii=False),encoding='utf-8')
lines=['# Master feature registry','','Generated from the runtime registry, not menu-button counts. These classifications describe the implemented scope; Stable promotion remains blocked.','','| Center | Classification | Evidence |','| --- | --- | --- |']
for center in catalog['DiagnosticCenters']:
    lines.append(f'| {center} | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |')
lines.extend(['','## Registered reversible configuration actions','','| ID | Scope | Risk | Verification |','| --- | --- | --- | --- |'])
for action in catalog['Actions']:
    lines.append(f"| {action['Id']} | {action['Scope']} | {action['Risk']} | Typed native read-back; production target hardware qualification pending |")
lines.extend(['','## External Windows handoffs','','Launching these tools is the verified behavior. Their operations are not counted as native HyperBoostX mutations.',''])
for entry in catalog['Handoffs']:
    for tool in entry['Tools']:lines.append(f"- {entry['Center']}: {tool['Name']} (`{tool['Destination']}`)")
lines.extend(['','## Historical coverage','','The raw inventory includes 606 action-map entries and XAML button labels from 35 tags. All action-map entries have a proposed destination or explicitly unimplemented destination. They remain NOT_STARTED at individual legacy-behavior granularity until an equivalence test exists; overlapping modern functionality does not imply parity.',''])
(root/'docs/MASTER_FEATURE_REGISTRY.md').write_text('\n'.join(lines),encoding='utf-8')
matrix=['# Legacy parity matrix','','Generated destination mapping is not functional parity. Every historical action remains individually unverified. See MASTER_FEATURE_REGISTRY.md for currently implemented behavior.','','| Legacy ID | Feature | Proposed v3 destination | Equivalence status |','| --- | --- | --- | --- |']
matrix += [f"| {i['legacy_id']} | {i['name'].replace('|','/')} | {i['proposed_v3_center']} | {i['implementation_status']} |" for i in legacy]
(root/'docs/LEGACY_PARITY_MATRIX.md').write_text('\n'.join(matrix),encoding='utf-8')
print(f"Generated {len(catalog['Actions'])} action definitions, {len(catalog['DiagnosticCenters'])} diagnostic centers and {len(legacy)} destination mappings.")
