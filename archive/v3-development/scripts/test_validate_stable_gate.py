import contextlib
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import validate_stable_gate as gate


def fixture():
    base = {k: 'synthetic evidence for test only' for k in gate.EVIDENCE_FIELDS}
    items = [dict(base, featureId='fixture', requirementId=f'fixture.{i}',
                  mandatory=True, status=gate.COMPLETE) for i in range(gate.MINIMUM_MANDATORY)]
    return {'schemaVersion': 1, 'inventory': dict(gate.REQUIRED_INVENTORY),
            'summary': {'requirementCount': len(items), 'statusCounts': {gate.COMPLETE: len(items)},
                        'mandatoryNotVerified': 0, 'stableReady': True}, 'requirements': items}


class GateTests(unittest.TestCase):
    def execute(self, payload, success=False):
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / 'matrix.json'
            p.write_text(json.dumps(payload), encoding='utf-8')
            baseline = Path(tmp) / 'required.json'
            baseline.write_text(json.dumps({'schemaVersion': 1, 'mandatoryRequirementIds':
                                            [r['requirementId'] for r in fixture()['requirements']]}),
                                encoding='utf-8')
            with patch.object(gate, 'MATRIX', p), patch.object(gate, 'BASELINE', baseline), contextlib.redirect_stdout(io.StringIO()):
                if success:
                    gate.main()
                else:
                    with self.assertRaises(SystemExit):
                        gate.main()

    def test_valid(self):
        self.execute(fixture(), True)

    def test_empty(self):
        d = fixture(); d['requirements'] = []; d['summary']['requirementCount'] = 0
        self.execute(d)

    def test_removed(self):
        d = fixture(); d['requirements'].pop(); d['summary']['requirementCount'] -= 1
        self.execute(d)

    def test_demoted(self):
        d = fixture(); d['requirements'][0]['mandatory'] = False
        self.execute(d)

    def test_string_flag(self):
        d = fixture(); d['requirements'][0]['mandatory'] = 'false'
        self.execute(d)

    def test_missing_evidence(self):
        d = fixture(); d['requirements'][0]['evidence'] = ' NOT_RUN '
        self.execute(d)

    def test_wrong_evidence_type(self):
        d = fixture(); d['requirements'][0]['evidence'] = []
        self.execute(d)

    def test_partial(self):
        d = fixture(); d['requirements'][0]['status'] = 'PARTIAL_VERIFIED'
        self.execute(d)

    def test_duplicate(self):
        d = fixture(); d['requirements'][1]['requirementId'] = d['requirements'][0]['requirementId']
        self.execute(d)

    def test_summary_mismatch(self):
        d = fixture(); d['summary']['requirementCount'] += 1
        self.execute(d)

    def test_reference_mismatch(self):
        d = fixture(); d['inventory']['technicalReferenceAssignments'] = 0
        self.execute(d)

    def test_wrong_schema(self):
        d = fixture(); d['schemaVersion'] = 2
        self.execute(d)

    def test_replaced_requirement(self):
        d = fixture(); d['requirements'][0]['requirementId'] = 'replacement'
        self.execute(d)

    def test_stale_status_counts(self):
        d = fixture(); d['summary']['statusCounts'] = {gate.COMPLETE: 1}
        self.execute(d)

    def test_false_readiness(self):
        d = fixture(); d['summary']['stableReady'] = False
        self.execute(d)
