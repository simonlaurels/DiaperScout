"""Read-only comparison of Azure GET snapshots; never emits secret/env values."""
import copy,json,sys
output={'applicationImageOnly':True,'apps':{}}
for name in ('web','api'):
    before=json.load(open(sys.argv[1]+'/'+name+'-before.json'))
    after=json.load(open(sys.argv[1]+'/'+name+'-after.json'))
    a=before['properties'];b=after['properties']
    ta=copy.deepcopy(a['template']);tb=copy.deepcopy(b['template'])
    for t in (ta,tb):
        t.pop('revisionSuffix',None)
        for c in t['containers']:c.pop('image',None)
    checks={
        'configuration':a['configuration']==b['configuration'],
        'templateExceptImageAndRevisionSuffix':ta==tb,
        'identity':before.get('identity')==after.get('identity'),
        'environment':all(a.get(k)==b.get(k) for k in ('environmentId','managedEnvironmentId','workloadProfileName')),
        'scaling':a['template']['scale']==b['template']['scale'],
        'secretReferences':a['configuration'].get('secrets')==b['configuration'].get('secrets')
    }
    output['apps'][name]={
        'checks':checks,'beforeImage':a['template']['containers'][0]['image'],
        'afterImage':b['template']['containers'][0]['image'],
        'readyRevision':b['latestReadyRevisionName'],
        'latestRevision':b['latestRevisionName'],
        'provisioningState':b['provisioningState'],
        'scale':b['template']['scale'],
        'revisionMode':b['configuration']['activeRevisionsMode'],
        'stickySessionAffinity':b['configuration']['ingress'].get('stickySessions'),
        'traffic':b['configuration']['ingress']['traffic']
    }
    if not all(checks.values()):output['applicationImageOnly']=False
json.dump(output,sys.stdout,indent=2);print()
if not output['applicationImageOnly']:sys.exit(1)
