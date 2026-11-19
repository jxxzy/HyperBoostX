import json
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MATRIX = ROOT / 'docs' / 'registry' / 'requirement-traceability.json'
COMPLETE = 'IMPLEMENTED_VERIFIED'
# Audited 2026-11-16 mandatory baseline. Reconcile explicitly before reducing.
MINIMUM_MANDATORY = 4056
BASELINE = ROOT / 'docs' / 'registry' / 'required-stable-ids.json'
REQUIRED_INVENTORY = {
    'primaryRoutes': 73, 'prototypeSubfeatures': 376,
    'galleryImageReferences': 81, 'legacyActionMapEntries': 606,
    'technicalReferenceMenus': 73, 'technicalReferenceAssignments': 365,
    'technicalReferenceDistinctUrls': 158,
}
EVIDENCE_FIELDS = ('uiComponent', 'command', 'backendHandler', 'provider',
                   'verification', 'test', 'evidence')
PLACEHOLDERS = {'', 'NOT_MAPPED', 'NOT_VERIFIED', 'NOT_RECORDED', 'NOT RUN',
                'NOT_RUN', 'TBD', 'TODO', 'N/A'}


def fail(message):
    raise SystemExit('Stable gate failed: ' + message)


def main():
    try:
        matrix = json.loads(MATRIX.read_text(encoding='utf-8-sig'))
    except (OSError, ValueError) as exc:
        fail(f'cannot read valid traceability matrix: {exc}')
    if not isinstance(matrix, dict) or matrix.get('schemaVersion') != 1:
        fail('unsupported matrix schema.')
    inventory = matrix.get('inventory')
    if not isinstance(inventory, dict):
        fail('missing source inventory.')
    mismatches = {k: (v, inventory.get(k)) for k, v in REQUIRED_INVENTORY.items()
                  if type(inventory.get(k)) is not int or inventory[k] != v}
    if mismatches:
        fail(f'source inventory mismatch: {mismatches}')
    requirements = matrix.get('requirements')
    if not isinstance(requirements, list) or len(requirements) < MINIMUM_MANDATORY:
        fail('requirement inventory below audited baseline.')
    if any(not isinstance(r, dict) or type(r.get('mandatory')) is not bool for r in requirements):
        fail('explicit boolean mandatory flags required.')
    mandatory = [r for r in requirements if r['mandatory']]
    if len(mandatory) < MINIMUM_MANDATORY:
        fail('mandatory count below audited baseline.')
    summary = matrix.get('summary')
    if not isinstance(summary, dict) or summary.get('requirementCount') != len(requirements):
        fail('summary count disagrees with inventory.')
    required = {'requirementId', 'featureId', 'status', *EVIDENCE_FIELDS}
    malformed = [r for r in requirements if not required.issubset(r) or
                 any(not isinstance(r.get(k), str) or not r[k].strip()
                     for k in ('requirementId', 'featureId', 'status'))]
    if malformed:
        fail(f'{len(malformed)} requirements lack valid traceability fields.')
    ids = [r['requirementId'].strip() for r in requirements]
    if len(set(ids)) != len(ids):
        fail('duplicate requirement IDs.')
    try:
        baseline = json.loads(BASELINE.read_text(encoding='utf-8-sig'))
    except (OSError, ValueError) as exc:
        fail(f'cannot read mandatory ID baseline: {exc}')
    if not isinstance(baseline, dict) or baseline.get('schemaVersion') != 1:
        fail('unsupported mandatory ID baseline schema.')
    baseline_ids = baseline.get('mandatoryRequirementIds')
    if (not isinstance(baseline_ids, list) or len(baseline_ids) < MINIMUM_MANDATORY
            or any(not isinstance(i, str) or not i.strip() or i != i.strip() for i in baseline_ids)
            or len(set(baseline_ids)) != len(baseline_ids)):
        fail('invalid mandatory ID baseline.')
    lost = set(baseline_ids) - {r['requirementId'] for r in mandatory}
    if lost:
        fail(f'{len(lost)} baseline requirements missing or demoted: {sorted(lost)[:10]}')
    if summary.get('statusCounts') != dict(Counter(r['status'] for r in requirements)):
        fail('summary status counts disagree with inventory.')
    pending = sum(r['status'] != COMPLETE for r in mandatory)
    if (type(summary.get('mandatoryNotVerified')) is not int
            or summary['mandatoryNotVerified'] != pending
            or type(summary.get('stableReady')) is not bool
            or summary['stableReady'] != (pending == 0)):
        fail('summary readiness disagrees with inventory.')
    incomplete = [r for r in mandatory if r['status'] != COMPLETE or
                  any(not isinstance(r[k], str) or r[k].strip().upper() in PLACEHOLDERS
                      for k in EVIDENCE_FIELDS)]
    if incomplete:
        counts = Counter(r['status'] for r in incomplete)
        print(f'Stable gate FAILED: {len(incomplete)} mandatory requirements lack verified implementation/evidence.')
        print('Incomplete status counts: ' + ', '.join(f'{k}={v}' for k, v in sorted(counts.items())))
        for r in incomplete[:20]:
            print(f"- {r['requirementId']}: {r['status']} — {str(r.get('requirement', ''))[:160]}")
        raise SystemExit(1)
    print(f'Stable requirement gate PASSED: {len(mandatory)} mandatory records have populated evidence fields.')
    print('This structural gate alone does not verify evidence authenticity, hardware, installer lifecycle or signing.')


if __name__ == '__main__':
    main()
