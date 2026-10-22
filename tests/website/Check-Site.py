"""Static website checks. Standard library only; no desktop application tests."""
from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import urlsplit, unquote
import argparse, json, re, urllib.request, xml.etree.ElementTree as ET
parser=argparse.ArgumentParser()
parser.add_argument('--online',action='store_true')
parser.add_argument('--report',type=Path)
args=parser.parse_args()
root=Path(__file__).resolve().parents[2]/'website'
class Document(HTMLParser):
    def __init__(self,text):
        super().__init__(convert_charrefs=True)
        self.elements=[]; self.ids=[]; self.feed(text)
    def handle_starttag(self,tag,attrs):
        attrs=dict(attrs); self.elements.append((tag,attrs))
        if 'id' in attrs: self.ids.append(attrs['id'])
checks=0
def check(value,message):
    global checks
    if not value: raise AssertionError(message)
    checks+=1
documents={path.name:Document(path.read_text(encoding='utf-8-sig')) for path in root.glob('*.html')}
for name,document in documents.items():
    text=(root/name).read_text(encoding='utf-8-sig')
    check(len(document.ids)==len(set(document.ids)),name+' duplicate IDs')
    for tag in ['h1','main','title']:
        check(sum(t==tag for t,_ in document.elements)==1,name+' one '+tag)
    check('lang-en' in text and 'lang-zh' in text,name+' bilingual content')
    check(not re.search(r'1\.4\.3 Stable|v1\.4\.3|Historical Beta|1\.3\.1-beta',text),name+' stale release copy')
    check(not re.search(r'Output Ownership|RunId|SourceId|验收|审查|事务|完整硬件|not trying|more than recording',text,re.I),name+' internal/defensive copy')
    for tag,attrs in document.elements:
        if tag=='img': check('alt' in attrs,name+' image alt')
        if tag=='button': check(attrs.get('type')=='button',name+' explicit button type')
        if attrs.get('target')=='_blank': check('noopener' in attrs.get('rel',''),name+' external link isolation')
        for field in (['href'] if tag=='a' else ['src'] if tag in ['img','script'] else ['href'] if tag=='link' else []):
            value=attrs.get(field,'')
            if not value: continue
            url=urlsplit(value)
            if url.scheme: check(url.scheme=='https',name+' external HTTPS'); continue
            relative=unquote(url.path).lstrip('/')
            target=root/(relative or name)
            if url.path=='/': target=root/'index.html'
            if target.suffix=='': target=target.with_suffix('.html')
            check(target.is_file(),name+' missing resource '+value)
            if url.fragment and target.suffix=='.html':
                check(url.fragment in documents[target.name].ids,name+' broken anchor '+value)
    check('data-release-version' in text if name in ('index.html','download.html') else True,name+' release binding')
ET.parse(root/'assets'/'diamond.svg'); checks+=1
release=json.loads((root/'assets'/'release.json').read_text())
check(bool(re.fullmatch(r'\d+\.\d+\.\d+',release['version'])),'stable semantic version')
for arch in ('x64','x86'): check(release['assets'][arch]['size']>0,'asset size '+arch)
if args.online:
    headers={'User-Agent':'InputStitch-Website-Verification/1.0'}
    with urllib.request.urlopen(urllib.request.Request('https://download.zhihanyu.com/latest/InputStitch-update.xml',headers=headers),timeout=20) as response:
        manifest=ET.fromstring(response.read())
    check(manifest.findtext('Version')==release['version'],'snapshot matches published Stable version')
    for arch in ('x64','x86'):
        with urllib.request.urlopen(urllib.request.Request(f'https://download.zhihanyu.com/latest/InputStitch-Windows-{arch}.exe',headers=headers,method='HEAD'),timeout=20) as response:
            check(int(response.headers['Content-Length'])==release['assets'][arch]['size'],'snapshot matches published size '+arch)
home_bytes=sum((root/name).stat().st_size for name in ['index.html','assets/style.css','assets/site.js','assets/diamond.svg','assets/app-zh.webp','assets/release.json'])
report=dict(status='PASS',checks=checks,pages=len(documents),online=args.online,release=release['version'],homeFirstPartyUncompressedBytes=home_bytes,scope='HTML routes/anchors/assets, bilingual structure, metadata, copy, published version/sizes and actual screenshot resources; desktop runtime is untouched.')
if args.report: args.report.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
