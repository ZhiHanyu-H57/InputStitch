import argparse,ctypes,hashlib,json,pathlib,subprocess,time
from ctypes import wintypes as W
from PIL import Image,ImageGrab
from obs_rpc import Obs
R=pathlib.Path(__file__).resolve().parent;u=ctypes.windll.user32
args=argparse.ArgumentParser();args.add_argument('--target',type=pathlib.Path,default=R/'native-game/InputStitchNativeD3D9Target.exe');cli=args.parse_args();TARGET=cli.target.resolve()
def hwnd_for(pid):
    result=[]
    @ctypes.WINFUNCTYPE(W.BOOL,W.HWND,W.LPARAM)
    def find(h,l):
        p=W.DWORD();u.GetWindowThreadProcessId(h,ctypes.byref(p))
        if p.value==pid:
            t=ctypes.create_unicode_buffer(500);u.GetWindowTextW(h,t,500)
            if t.value=='InputStitch Native D3D9 Target':result.append(h)
        return True
    u.EnumWindows(find,0);return result[0] if result else 0
obs=None;p=None;h=0
try:
    for _ in range(30):
        try:obs=Obs();break
        except OSError:time.sleep(.2)
    if not obs:raise RuntimeError('isolated OBS did not start')
    for name in obs.call('GetSpecialInputs').values():
        if name:obs.call('SetInputMute',{'inputName':name,'inputMuted':True})
    obs.call('SetInputSettings',{'inputName':'GameOnly','inputSettings':{'window':'InputStitch Native D3D9 Target:InputStitchNativeD3D9TargetClass:InputStitchNativeD3D9Target.exe','capture_mode':'window','priority':1,'capture_overlays':True,'capture_cursor':False,'anti_cheat_hook':True,'sli_compatibility':False},'overlay':True})
    p=subprocess.Popen([str(TARGET),'--seconds','35'],cwd=TARGET.parent,creationflags=subprocess.CREATE_NO_WINDOW)
    for _ in range(50):
        h=hwnd_for(p.pid)
        if h:break
        time.sleep(.1)
    if not h:raise RuntimeError('native target window unavailable')
    u.ShowWindow(h,4);u.SetForegroundWindow(h);time.sleep(7)
    for i in range(2):
        obs.call('SaveSourceScreenshot',{'sourceName':'GameOnly','imageFormat':'png','imageFilePath':str(R/'evidence'/('detours-baseline-preview-'+str(i)+'.png')),'imageWidth':800,'imageHeight':400})
        pt=W.POINT(0,0);u.ClientToScreen(h,ctypes.byref(pt));ImageGrab.grab().crop((pt.x,pt.y,pt.x+800,pt.y+400)).save(R/'evidence'/('detours-baseline-desktop-'+str(i)+'.png'));time.sleep(.7)
    obs.call('StartRecord');time.sleep(6);result=obs.call('StopRecord');src=pathlib.Path(result['outputPath']);dest=R/'recordings/detours-baseline.mkv'
    for _ in range(30):
        try:src.replace(dest);break
        except PermissionError:time.sleep(.1)
    data={'case':'detours-baseline','noInputStitchProducer':True,'pid':p.pid,'video':str(dest),'input':obs.call('GetInputSettings',{'inputName':'GameOnly'}),'stats':obs.call('GetStats'),'targetSha256':hashlib.sha256(TARGET.read_bytes()).hexdigest()}
    (R/'evidence/detours-baseline.json').write_text(json.dumps(data,indent=2),encoding='utf8');print(json.dumps(data),flush=True)
finally:
    if h:u.PostMessageW(h,0x10,0,0)
    if p:
        try:p.wait(5)
        except subprocess.TimeoutExpired:pass
    if (TARGET.parent/'native-target-run.log').exists():(R/'evidence/detours-baseline-target.log').write_bytes((TARGET.parent/'native-target-run.log').read_bytes())
    if obs:
        try:
            if obs.call('GetRecordStatus')['outputActive']:obs.call('StopRecord')
        finally:obs.close()
