import argparse,hashlib,json,pathlib,shutil,subprocess
from PIL import Image
ROOT=pathlib.Path(__file__).resolve().parent
args=argparse.ArgumentParser();args.add_argument('--ffmpeg',default=shutil.which('ffmpeg'));cli=args.parse_args()
if not cli.ffmpeg:raise SystemExit('Supply --ffmpeg PATH or put ffmpeg on PATH')
FFMPEG=cli.ffmpeg
def counts(image):
    yellow=purple=blue=green=0;gx=0
    for i,(r,g,b) in enumerate(image.convert('RGB').getdata()):
        yellow+=r>100 and g>85 and b<75 and r>g*1.05 and g>b*1.7
        purple+=r>100 and b>85 and g<r*.5 and b>r*.6
        blue+=b>r*1.5 and b>g*1.25 and 15<g<100 and b>30
        isgreen=g>160 and r<100 and b<150
        green+=isgreen
        if isgreen:gx+=i%image.width
    return {'yellow':yellow,'purple':purple,'blue':blue,'green':green,'greenX':round(gx/green,2) if green else None,'pixels':image.width*image.height,'hash':hashlib.sha256(image.tobytes()).hexdigest()}
records=json.loads((ROOT/'evidence/detours-recordings.json').read_text())
results=[]
for item in records:
    frames=ROOT/'evidence/detours-frames'/item['case'];frames.mkdir(parents=True,exist_ok=True)
    subprocess.run([FFMPEG,'-hide_banner','-loglevel','error','-y','-i',str(ROOT/'recordings'/pathlib.Path(item['video']).name),'-vf','fps=2','-frames:v','12',str(frames/'frame-%03d.png')],check=True)
    values=[]
    for path in sorted(frames.glob('frame-*.png')):
        with Image.open(path) as image:value=counts(image)
        value['frame']=path.name;values.append(value)
    checks={'frameCount':len(values)>=10,'gameBackground':all(v['blue']>v['pixels']*.8 for v in values),'desktopExcluded':all(v['purple']<20 for v in values),'dynamicMarkerVisible':all(v['green']>=100 for v in values),'dynamicMarkerMoves':len(set(v['greenX'] for v in values))>=3,'actualFramesChange':len(set(v['hash'] for v in values))>=3}
    checks['rtssExpected']=all(v['yellow']>500 for v in values) if item['thirdPartyOverlays'] and item['hostMode']=='both' else all(v['yellow']<20 for v in values)
    result={'case':item['case'],'checks':checks,'pass':all(checks.values()),'details':values};results.append(result)
    print(json.dumps({'case':item['case'],'checks':checks,'first':values[0]},ensure_ascii=False),flush=True)
with Image.open(ROOT/'evidence/detours-on-both-desktop.png') as image:positive=counts(image)
positivepass=positive['yellow']>500 and positive['purple']>500
report={'videoCases':results,'desktopPositive':positive,'desktopPositivePass':positivepass,'pass':len(results)==3 and all(r['pass'] for r in results) and positivepass}
(ROOT/'evidence/detours-frame-analysis.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('PASS' if report['pass'] else 'FAIL')
raise SystemExit(0 if report['pass'] else 1)
