"""Exercise the actual policy script against an isolated ARM/CLI stub."""
import copy
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / "apply-web-reliability-policy.sh"
FAKE_AZ = r'''#!/usr/bin/env python3
import json, os, sys
from pathlib import Path
p=Path(os.environ['POLICY_TEST_STATE'])
s=json.loads(p.read_text()); args=sys.argv[1:]; s['commands'].append(args)
if args[:2]==['containerapp','show']:
    out=s['resource']
elif args[:2]==['rest','--method'] and args[2]=='get':
    out=s['resource']
elif args[:2]==['rest','--method'] and args[2]=='patch':
    body=json.loads(Path(args[args.index('--body')+1][1:]).read_text())
    s['patches'].append(body)
    s['resource']['properties']['template'].update(body['properties']['template'])
    suffix=body['properties']['template']['revisionSuffix']
    for k in ['latestRevisionName','latestReadyRevisionName']:
        s['resource']['properties'][k]='diaperscout-web-vnet--'+suffix
    s['resource']['properties']['provisioningState']='Succeeded'
    out=None
elif args[:4]==['containerapp','ingress','sticky-sessions','set']:
    assert args[args.index('--affinity')+1]=='sticky'
    s['resource']['properties']['configuration']['ingress']['stickySessions']={'affinity':'sticky'}
    out=None
else:
    raise Exception('Unexpected command: '+repr(args))
p.write_text(json.dumps(s))
if out is not None: print(json.dumps(out))
'''

class PolicyTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        az = self.root / 'az'
        az.write_text(FAKE_AZ)
        az.chmod(0o755)
        self.resource = {
            'id': '/subscriptions/test/resourceGroups/rg-diaperscout-prod/providers/Microsoft.App/containerApps/diaperscout-web-vnet',
            'identity': {'type': 'SystemAssigned'},
            'properties': {
                'configuration': {'activeRevisionsMode': 'Single', 'ingress': {
                    'stickySessions': {'affinity': 'sticky'}, 'allowInsecure': False}},
                'provisioningState': 'Succeeded',
                'latestRevisionName': 'existing', 'latestReadyRevisionName': 'existing',
                'template': {'revisionSuffix': 'existing', 'containers': [{
                    'image': 'immutable@sha256:existing', 'env': [{'name': 'Secret', 'secretRef': 'existing'}],
                    'resources': {'cpu': 0.5, 'memory': '1Gi'}}],
                    'scale': {'minReplicas': 1, 'maxReplicas': 1,
                              'cooldownPeriod': 300, 'pollingInterval': 30, 'rules': None}}
            }
        }
        self.state = self.root / 'state.json'
        self.env = dict(os.environ, PATH=str(self.root)+os.pathsep+os.environ['PATH'], POLICY_TEST_STATE=str(self.state))
        self.reset()

    def tearDown(self):
        self.tmp.cleanup()

    def reset(self):
        self.state.write_text(json.dumps({'resource': self.resource, 'commands': [], 'patches': []}))

    def run_policy(self, *args):
        return subprocess.run(['bash', str(SCRIPT), *args], env=self.env, capture_output=True, text=True)

    def saved(self):
        return json.loads(self.state.read_text())

    def test_scale_zero_changes_only_allowed_template_fields(self):
        before = copy.deepcopy(self.resource)
        result = self.run_policy()
        self.assertEqual(result.returncode, 0, result.stderr)
        saved = self.saved()
        self.assertEqual(len(saved['patches']), 1)
        self.assertEqual(list(saved['patches'][0]['properties']), ['template'])
        after = saved['resource']
        self.assertEqual(after['identity'], before['identity'])
        self.assertEqual(after['properties']['configuration'], before['properties']['configuration'])
        self.assertEqual(after['properties']['template']['containers'], before['properties']['template']['containers'])
        self.assertEqual(after['properties']['template']['scale'], {
            'minReplicas': 0, 'maxReplicas': 2, 'cooldownPeriod': 600, 'pollingInterval': 30, 'rules': None})
        self.assertNotEqual(after['properties']['template']['revisionSuffix'], 'existing')
        self.assertTrue(all('diaperscout-api-vnet' not in command for command in saved['commands']))

    def test_idempotent_policy_creates_no_revision(self):
        self.assertEqual(self.run_policy().returncode, 0)
        revision = self.saved()['resource']['properties']['latestRevisionName']
        self.assertEqual(self.run_policy().returncode, 0)
        saved = self.saved()
        self.assertEqual(len(saved['patches']), 1)
        self.assertEqual(saved['resource']['properties']['latestRevisionName'], revision)

    def test_warm_rollback_retains_affinity_and_image(self):
        self.resource['properties']['template']['scale'].update(minReplicas=0, maxReplicas=2, cooldownPeriod=600)
        self.reset()
        result = self.run_policy('--rollback-warm')
        self.assertEqual(result.returncode, 0, result.stderr)
        props = self.saved()['resource']['properties']
        self.assertEqual(props['template']['scale'], {'minReplicas': 1, 'maxReplicas': 1,
                         'cooldownPeriod': 300, 'pollingInterval': 30, 'rules': None})
        self.assertEqual(props['configuration']['ingress']['stickySessions']['affinity'], 'sticky')
        self.assertEqual(props['template']['containers'], self.resource['properties']['template']['containers'])

    def test_multiple_revision_mode_refuses_mutation(self):
        self.resource['properties']['configuration']['activeRevisionsMode'] = 'Multiple'
        self.reset()
        self.assertNotEqual(self.run_policy().returncode, 0)
        self.assertEqual(self.saved()['patches'], [])
        self.assertTrue(all(c[:2] == ['containerapp', 'show'] for c in self.saved()['commands']))

if __name__ == '__main__':
    unittest.main()
