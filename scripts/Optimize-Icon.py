from pathlib import Path
import struct,zlib,json
# Optional maintenance tool; ordinary builds consume the committed icon directly.
# Python standard library only; preserves every decoded pixel and PNG metadata.
import argparse
args=argparse.ArgumentParser();args.add_argument('--icon',type=Path,default=Path(__file__).resolve().parent.parent/'InputStitch.ico');args.add_argument('--report',type=Path);options=args.parse_args()
p=options.icon;before=p.read_bytes()
def chunks(png):
 a=8;out=[]
 while a<len(png):
  n=struct.unpack_from('>I',png,a)[0];out.append((png[a+4:a+8],png[a+8:a+8+n]));a+=n+12
 return out
def paeth(a,b,c):
 p=a+b-c; ds=[abs(p-a),abs(p-b),abs(p-c)];return [a,b,c][ds.index(min(ds))]
def predict(f,left,up,corner):return [0,left,up,(left+up)//2,paeth(left,up,corner)][f]
def pixels(png):
 c=chunks(png);width,height,depth,color,comp,filt,inter=struct.unpack('>IIBBBBB',c[0][1]);assert depth==8 and color in (2,6) and inter==0
 bpp={2:3,6:4}[color];stride=width*bpp;raw=zlib.decompress(b''.join(d for t,d in c if t==b'IDAT'));rows=[];prev=bytearray(stride)
 for y in range(height):
  off=y*(stride+1);f=raw[off];row=bytearray(raw[off+1:off+1+stride])
  for x in range(stride):row[x]=(row[x]+predict(f,row[x-bpp] if x>=bpp else 0,prev[x],prev[x-bpp] if x>=bpp else 0))&255
  rows.append(row);prev=row
 return rows,bpp,raw
def chunk(t,d):return struct.pack('>I',len(d))+t+d+struct.pack('>I',zlib.crc32(t+d)&0xffffffff)
count=struct.unpack_from('<H',before,4)[0];entries=[];images=[];report=[];offset=6+16*count
for i in range(count):
 entry=list(struct.unpack_from('<BBBBHHII',before,6+16*i));old=before[entry[7]:entry[7]+entry[6]];rows,bpp,raw=pixels(old)
 adaptive=bytearray();prev=bytearray(len(rows[0]))
 for row in rows:
  candidates=[]
  for f in range(5):
   data=bytes((v-predict(f,row[x-bpp] if x>=bpp else 0,prev[x],prev[x-bpp] if x>=bpp else 0))&255 for x,v in enumerate(row))
   candidates.append((sum(min(x,256-x) for x in data),f,data))
  _,f,data=min(candidates);adaptive+=bytes([f])+data;prev=row
 compressed=min((zlib.compress(raw,9),zlib.compress(adaptive,9)),key=len)
 new=bytearray(old[:8]);written=False
 for t,d in chunks(old):
  if t==b'IDAT':
   if written:continue
   d=compressed;written=True
  new+=chunk(t,d)
 new=bytes(new)
 if len(new)>len(old):new=old
 assert pixels(new)[0]==rows
 report.append(dict(size=[entry[0] or 256,entry[1] or 256],beforeBytes=len(old),afterBytes=len(new)))
 entry[6]=len(new);entry[7]=offset;entries.append(struct.pack('<BBBBHHII',*entry));images.append(new);offset+=len(new)
after=before[:6]+b''.join(entries)+b''.join(images)
p.write_bytes(after)
if options.report: options.report.write_text(json.dumps(dict(beforeBytes=len(before),afterBytes=len(after),allPixelRowsIdentical=True,images=report),indent=2),encoding='utf-8')
print('LOSSLESS ICON',len(before),'->',len(after))
