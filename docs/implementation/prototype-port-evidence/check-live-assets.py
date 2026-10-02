import hashlib,json,urllib.request
from pathlib import Path
checks=[]
for relative in ('css/product-prototype.css','js/product-prototype.js','service-worker.js'):
    local=Path('src/DiaperScout.Web/wwwroot')/relative
    with urllib.request.urlopen('https://diaperscout.app/'+relative,timeout=90) as response:
        content=response.read();status=response.status
    digest=lambda data:hashlib.sha256(data).hexdigest()
    check={'path':'/'+relative,'status':status,'localSha256':digest(local.read_bytes()),'productionSha256':digest(content)}
    check['matched']=status==200 and check['localSha256']==check['productionSha256'];checks.append(check)
Path('docs/implementation/prototype-port-evidence/live-assets.json').write_text(json.dumps(checks,indent=2)+'\n')
assert all(c['matched'] for c in checks)
print('New CSS/module and unchanged service worker match committed production asset bytes.')
