import argparse,hashlib,json,pathlib,shutil,subprocess
from PIL import Image
args=argparse.ArgumentParser();args.add_argument('--ffmpeg',default=shutil.which('ffmpeg'));cli=args.parse_args();
if not cli.ffmpeg:raise SystemExit('Supply --ffmpeg PATH or put ffmpeg on PATH')
R=pathlib.Path(__file__).resolve().parent; frames=R/'evidence/detours-baseline-frames';frames.mkdir(exist_ok=True)
subprocess.run([cli.ffmpeg,'-hide_banner','-loglevel','error','-y','-i',str(R/'recordings/detours-baseline.mkv'),'-vf','fps=2','-frames:v','12',str(frames/'frame-%03d.png')],check=True)
def score(path):
    im=Image.open(path).convert('RGB');blue=green=gx=0
    for i,(r,g,b) in enumerate(im.getdata()):
        blue+=b>r*1.5 and b>g*1.25 and 15<g<100 and b>30
        if g>160 and r<100 and b<160:green+=1;gx+=i%im.width
    return {'file':path.name,'blue':blue,'green':green,'greenX':gx/green if green else None,'hash':hashlib.sha256(im.tobytes()).hexdigest()}
values=[score(p) for p in sorted(frames.glob('*.png'))];desktop=[score(R/'evidence'/('detours-baseline-desktop-'+str(i)+'.png')) for i in range(2)]
checks={'frameCount':len(values)>=10,'gameBlue':all(v['blue']>256000 for v in values),'greenPresent':all(v['green']>100 for v in values),'greenMoves':len(set(v['greenX'] for v in values))>=3,'framesChange':len(set(v['hash'] for v in values))>=3}
report={'pass':all(checks.values()),'checks':checks,'frames':values,'physicalDesktop':desktop};(R/'evidence/detours-baseline-analysis.json').write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps({'pass':report['pass'],'checks':checks,'first':values[0],'last':values[-1],'desktop':desktop}),flush=True)
raise SystemExit(0 if report['pass'] else 1)

