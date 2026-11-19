"""Reproducible inventory of source evidence; registration never implies implementation."""
import json, re, subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
legacy = root / 'old'
out = root / 'docs' / 'audit'
out.mkdir(parents=True, exist_ok=True)
def git(*args):
    return subprocess.run(['git', '-C', str(root.parent), *args], capture_output=True, encoding='utf-8', errors='replace').stdout
actionmap = json.loads((legacy / 'wpf/Data/ui_action_map_v2_10.json').read_text(encoding='utf-8-sig'))
entries = []
for menu in actionmap['menus']:
    for action in menu['actions']:
        entries.append(dict(legacy_version='2.10.0', legacy_id=action['id'], name=action['label'],
            legacy_menu=menu['key'], source='old/wpf/Data/ui_action_map_v2_10.json',
            endpoint=action.get('path'), center=menu.get('category'), status='NOT_STARTED',
            verification='Not yet established', rollback='Not yet established'))
history = []
for tag in git('tag','--list').splitlines():
    paths = git('ls-tree','-r','--name-only',tag).splitlines()
    xamls = [p for p in paths if p.endswith('.xaml') and ('MainWindow' in p or '/Views/' in p)]
    labels = set()
    for path in xamls:
        source = git('show', f'{tag}:{path}')
        labels.update(re.findall(r'<Button\b[^>]*\bContent="([^"{][^"]*)"', source))
    history.append(dict(tag=tag, xaml_sources=xamls, button_labels=sorted(labels)))
(out / 'legacy-actions.json').write_text(json.dumps(entries, indent=2, ensure_ascii=False), encoding='utf-8')
(out / 'historical-xaml.json').write_text(json.dumps(history, indent=2, ensure_ascii=False), encoding='utf-8')
routes=[]
for p in (legacy / 'app').rglob('*.py'):
    for route in re.findall(r'@\w+\.(?:route|get|post|put|delete)\(\s*[\'\"]([^\'\"]+)',p.read_text(encoding='utf-8-sig',errors='replace')):
        routes.append(dict(source=str(p.relative_to(legacy)),route=route))
(out / 'legacy-routes.json').write_text(json.dumps(routes,indent=2),encoding='utf-8')
lines=['# Legacy parity inventory', '', 'Mapping is inventory coverage, not proof of behavior or feature parity.', '',
       f'{len(entries)} action-map entries; {len(history)} historical tags inspected; {len(routes)} route declarations.', '',
       '| Legacy ID | Feature | Menu | State |', '| --- | --- | --- | --- |']
lines += [f"| {e['legacy_id']} | {e['name'].replace('|','/')} | {e['legacy_menu']} | {e['status']} |" for e in entries]
(root / 'docs/LEGACY_PARITY_MATRIX.md').write_text('\n'.join(lines),encoding='utf-8')
print(f'Inventoried {len(entries)} actions, {len(history)} tags, {len(routes)} route declarations.')
