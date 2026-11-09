import numpy as np, pickle, sys, collections
src = sys.argv[1]
V=[]; VT=[]; parts=collections.OrderedDict(); cur=None; mtl=None; g='default'
def key(): return (g, mtl)
with open(src) as f:
    for line in f:
        if line.startswith('v '): V.append([float(x) for x in line.split()[1:4]])
        elif line.startswith('vt '): VT.append([float(x) for x in line.split()[1:3]])
        elif line.startswith('g '): g=line[2:].strip()
        elif line.startswith('usemtl '): mtl=line[7:].strip()
        elif line.startswith('f '):
            idx=[]; uvi=[]
            for t in line.split()[1:]:
                a=t.split('/'); idx.append(int(a[0])-1); uvi.append(int(a[1])-1 if len(a)>1 and a[1] else -1)
            k=key(); p=parts.setdefault(k, {'f':[], 'uv':[]})
            for i in range(1,len(idx)-1):
                p['f'].append((idx[0],idx[i],idx[i+1])); p['uv'].append((uvi[0],uvi[i],uvi[i+1]))
V=np.array(V,np.float32); VT=np.array(VT,np.float32) if VT else np.zeros((1,2),np.float32)
print('verts',len(V),'bbox',V.min(0),V.max(0))
out=[]
for (gn,m),p in parts.items():
    F=np.array(p['f'],np.int64); P=V[F.reshape(-1)]
    out.append(dict(g=gn,m=m,f=F,uv=np.array(p['uv'],np.int64),bmin=P.min(0),bmax=P.max(0)))
pickle.dump(dict(V=V,VT=VT,parts=out),open('rx7_parts.pkl','wb'))
for i,o in enumerate(out):
    c=(o['bmin']+o['bmax'])/2; s=o['bmax']-o['bmin']
    print(f"{i:3d} tris {len(o['f']):6d} c({c[0]:6.2f},{c[1]:5.2f},{c[2]:6.2f}) s({s[0]:5.2f},{s[1]:5.2f},{s[2]:5.2f})  {o['m'][:38]:38s} {o['g'][:60]}")
