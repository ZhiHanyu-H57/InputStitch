import argparse,ctypes,json,pathlib,subprocess,time
from ctypes import wintypes as W
from PIL import ImageGrab
from obs_rpc import Obs
R=pathlib.Path(__file__).resolve().parent;u=ctypes.windll.user32;u.SetProcessDpiAwarenessContext(ctypes.c_void_p(-4))
args=argparse.ArgumentParser();args.add_argument('--target',type=pathlib.Path,default=R/'native-game/InputStitchNativeD3D9Target.exe');cli=args.parse_args();TARGET=cli.target.resolve()
def hwnd_for(pid):
 out=[]
 @ctypes.WINFUNCTYPE(W.BOOL,W.HWND,W.LPARAM)
 def f(h,l):
  p=W.DWORD();u.GetWindowThreadProcessId(h,ctypes.byref(p))
  if p.value==pid:
   t=ctypes.create_unicode_buffer(500);u.GetWindowTextW(h,t,500)
   if t.value=='InputStitch Native D3D9 Target':out.append(h)
  return True
 u.EnumWindows(f,0);return out[0] if out else 0

def command(s):
 p=R/'detours-producer-command.next';p.write_text(s,encoding='ascii');p.replace(R/'detours-producer-command.txt')
obs=Obs();results=[];target=producer=None;hwnd=0
try:
 for case,overlays,mode in [('on-both',True,'both'),('off-both',False,'both'),('on-desktop',True,'desktop')]:
  obs.call('SetInputSettings',{'inputName':'GameOnly','inputSettings':{'capture_overlays':overlays},'overlay':True})
  target=subprocess.Popen([str(TARGET),'--seconds','30'],cwd=TARGET.parent,creationflags=subprocess.CREATE_NO_WINDOW)
  for _ in range(50):
   hwnd=hwnd_for(target.pid)
   if hwnd:break
   time.sleep(.1)
  if not hwnd:raise RuntimeError('native window missing')
  u.ShowWindow(hwnd,4);u.SetForegroundWindow(hwnd);time.sleep(6)
  (R/'detours-producer-ready.json').unlink(missing_ok=True);command(mode)
  producer=subprocess.Popen([str(R/'TimerOsdProducer.exe')],cwd=R,creationflags=subprocess.CREATE_NO_WINDOW)
  for _ in range(50):
   if (R/'detours-producer-ready.json').exists():break
   if producer.poll() is not None:raise RuntimeError('producer exited early')
   time.sleep(.1)
  time.sleep(2)
  pt=W.POINT(0,0);u.ClientToScreen(hwnd,ctypes.byref(pt));ImageGrab.grab().crop((pt.x,pt.y,pt.x+800,pt.y+400)).save(R/'evidence'/('detours-'+case+'-desktop.png'))
  obs.call('SaveSourceScreenshot',{'sourceName':'GameOnly','imageFormat':'png','imageFilePath':str(R/'evidence'/('detours-'+case+'-preview.png')),'imageWidth':800,'imageHeight':400})
  print('START '+case,flush=True);obs.call('StartRecord');time.sleep(6);record=obs.call('StopRecord');src=pathlib.Path(record['outputPath']);dest=R/'recordings'/('detours-'+case+'.mkv')
  for _ in range(50):
   try:src.replace(dest);break
   except PermissionError:time.sleep(.1)
  results.append({'case':case,'thirdPartyOverlays':overlays,'hostMode':mode,'video':str(dest),'bytes':dest.stat().st_size,'nativePid':target.pid,'producerPid':producer.pid,'producerReady':json.loads((R/'detours-producer-ready.json').read_text()),'producerStatus':json.loads((R/'detours-producer-status.json').read_text()),'input':obs.call('GetInputSettings',{'inputName':'GameOnly'}),'stats':obs.call('GetStats')})
  (R/'evidence/detours-recordings.json').write_text(json.dumps(results,indent=2),encoding='utf8')
  command('stop');producer.wait(5);producer=None;u.PostMessageW(hwnd,0x10,0,0);target.wait(5);target=None;hwnd=0
  (R/'evidence'/('detours-'+case+'-target.log')).write_bytes((TARGET.parent/'native-target-run.log').read_bytes());print('STOP '+case,flush=True);time.sleep(1)
finally:
 command('stop')
 if hwnd:u.PostMessageW(hwnd,0x10,0,0)
 for p in [producer,target]:
  if p:
   try:p.wait(5)
   except subprocess.TimeoutExpired:pass
 try:
  if obs.call('GetRecordStatus')['outputActive']:obs.call('StopRecord')
 finally:obs.close()
