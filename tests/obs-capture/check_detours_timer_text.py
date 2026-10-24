import hashlib,json,pathlib
from PIL import Image
R=pathlib.Path(__file__).resolve().parent
values=[]
for p in sorted((R/'evidence/detours-frames/on-both').glob('frame-*.png')):
 im=Image.open(p).convert('RGB').crop((30,40,750,230))
 mask=bytes(255 if r>100 and g>85 and b<75 and r>g*1.05 and g>b*1.7 else 0 for r,g,b in im.getdata())
 values.append({'frame':p.name,'yellowPixels':sum(mask)//255,'yellowTextMaskSha256':hashlib.sha256(mask).hexdigest()})
report={'region':[30,40,750,230],'method':'yellow text mask only; excludes green native motion and white native frame counter','frames':len(values),'distinctTextMasks':len(set(v['yellowTextMaskSha256'] for v in values)),'pass':len(values)==12 and all(v['yellowPixels']>500 for v in values) and len(set(v['yellowTextMaskSha256'] for v in values))>=3,'details':values}
(R/'evidence/detours-timer-text-analysis.json').write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in report.items() if k!='details'}));raise SystemExit(0 if report['pass'] else 1)
