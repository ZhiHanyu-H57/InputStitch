import base64, hashlib, json, pathlib, sys, time
import websocket

ROOT=pathlib.Path(__file__).resolve().parent
class Obs:
    def __init__(self):
        cfg=json.loads((ROOT/'portable/config/obs-studio/plugin_config/obs-websocket/config.json').read_text(encoding='utf-8-sig'))
        self.ws=websocket.create_connection('ws://127.0.0.1:'+str(cfg['server_port']),timeout=10)
        hello=json.loads(self.ws.recv())['d']; identify={'rpcVersion':1,'eventSubscriptions':0}
        if 'authentication' in hello:
            auth=hello['authentication']; secret=base64.b64encode(hashlib.sha256((cfg['server_password']+auth['salt']).encode()).digest()).decode()
            identify['authentication']=base64.b64encode(hashlib.sha256((secret+auth['challenge']).encode()).digest()).decode()
        self.ws.send(json.dumps({'op':1,'d':identify})); accepted=json.loads(self.ws.recv())
        if accepted['op']!=2: raise RuntimeError('OBS authentication failed')
    def call(self,request,data=None):
        ident=str(time.time_ns()); self.ws.send(json.dumps({'op':6,'d':{'requestType':request,'requestId':ident,'requestData':data or {}}}))
        while True:
            reply=json.loads(self.ws.recv())
            if reply.get('op')==7 and reply['d']['requestId']==ident:
                d=reply['d']
                if not d['requestStatus']['result']: raise RuntimeError(request+': '+str(d['requestStatus']))
                return d.get('responseData',{})
    def close(self): self.ws.close()

def setup(obs):
    version=obs.call('GetVersion'); print(json.dumps({k:version[k] for k in ['obsVersion','obsWebSocketVersion','platform']}))
    for name in obs.call('GetSpecialInputs').values():
        if name: obs.call('SetInputMute',{'inputName':name,'inputMuted':True})
    obs.call('SetVideoSettings',{'baseWidth':800,'baseHeight':400,'outputWidth':800,'outputHeight':400,'fpsNumerator':30,'fpsDenominator':1})
    scenes=obs.call('GetSceneList')
    if not any(s['sceneName']=='Acceptance' for s in scenes['scenes']): obs.call('CreateScene',{'sceneName':'Acceptance'})
    obs.call('SetCurrentProgramScene',{'sceneName':'Acceptance'})
    inputs=obs.call('GetInputList')['inputs']
    if not any(i['inputName']=='GameOnly' for i in inputs):
        obs.call('CreateInput',{'sceneName':'Acceptance','inputName':'GameOnly','inputKind':'game_capture','inputSettings':{'capture_mode':'window','capture_overlays':True,'capture_cursor':False,'anti_cheat_hook':True},'sceneItemEnabled':True})
    windows=obs.call('GetInputPropertiesListPropertyItems',{'inputName':'GameOnly','propertyName':'window'})
    candidates=[i for i in windows['propertyItems'] if 'obscapturehost.exe' in str(i['itemValue']).lower()]
    if not candidates: raise RuntimeError('Host not in OBS game-capture window list; entries='+str(len(windows['propertyItems'])))
    target=candidates[0]
    print(json.dumps({'target':target},ensure_ascii=False))
    obs.call('SetInputSettings',{'inputName':'GameOnly','inputSettings':{'capture_mode':'window','window':target['itemValue'],'priority':1,'capture_overlays':True,'capture_cursor':False,'anti_cheat_hook':True,'sli_compatibility':True},'overlay':True})
    print(json.dumps(obs.call('GetInputSettings',{'inputName':'GameOnly'}),ensure_ascii=False))
    (ROOT/'evidence/game-input.json').write_text(json.dumps(obs.call('GetInputSettings',{'inputName':'GameOnly'}),indent=2),encoding='utf-8')
    print(json.dumps(obs.call('GetVideoSettings')))
    obs.call('SetRecordDirectory',{'recordDirectory':str(ROOT/'recordings')})
    for category,name,value in [('Output','Mode','Simple'),('SimpleOutput','RecFormat2','mkv'),('SimpleOutput','RecEncoder','x264'),('SimpleOutput','RecQuality','Small'),('SimpleOutput','VBitrate','8000')]:
        obs.call('SetProfileParameter',{'parameterCategory':category,'parameterName':name,'parameterValue':value})
    print(json.dumps(obs.call('GetRecordDirectory')))

if __name__=='__main__':
    obs=Obs()
    try:
        command=sys.argv[1]
        if command=='setup': setup(obs)
        elif command=='overlays': print(json.dumps(obs.call('SetInputSettings',{'inputName':'GameOnly','inputSettings':{'capture_overlays':sys.argv[2]=='on'},'overlay':True})))
        elif command=='record': print(json.dumps(obs.call('StartRecord')))
        elif command=='stop': print(json.dumps(obs.call('StopRecord')))
        elif command=='screenshot':
            print(json.dumps(obs.call('SaveSourceScreenshot',{'sourceName':'GameOnly','imageFormat':'png','imageFilePath':str(ROOT/'evidence'/sys.argv[2]),'imageWidth':800,'imageHeight':400})))
        else: print(json.dumps(obs.call(command),ensure_ascii=False))
    finally: obs.close()

