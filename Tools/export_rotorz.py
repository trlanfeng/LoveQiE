"""Run from project root with UnityPy and Pillow installed. Reads immutable backup."""
import hashlib, io, json, zipfile
from pathlib import Path
import UnityPy
from PIL import Image
ROOT = Path(__file__).resolve().parents[1]
def export():
 with zipfile.ZipFile(ROOT/'MigrationBackup/before-native-tilemap.zip') as archive:
  levels=[]
  for name in sorted(archive.namelist()):
   if not (name.startswith('Assets/Resources/Maps/Scenes/') and name.endswith('.prefab')): continue
   raw=archive.read(name); env=UnityPy.load(raw)
   objects={o.path_id:o.read_typetree() for o in env.objects}
   systems=[d for d in objects.values() if '_cellSize' in d]; assert len(systems)==1
   s=systems[0]
   tr=next(d for d in objects.values() if 'm_LocalPosition' in d and d['m_GameObject']==s['m_GameObject'])
   assert s['_tilesFacing']==0 and tr['m_LocalScale']==dict(x=1.,y=1.,z=1.) and tr['m_LocalRotation']==dict(x=0.,y=0.,z=0.,w=1.)
   cells=[]
   for ref in s['_chunks']:
    if not ref['m_PathID']: continue
    ch=objects[ref['m_PathID']]
    for i,t in enumerate(ch['tiles']):
     if t['_flags']==0: continue # Verified against TileData.Empty IL.
     row=ch['_firstRow']+i//s['_chunkWidth']; col=ch['_firstColumn']+i%s['_chunkWidth']
     assert 0<=row<s['_rows'] and 0<=col<s['_columns']
     assert t['gameObject']['m_PathID']==0 and 0<=t['tilesetIndex']<64
     rotation=(t['_flags'] & 6291456)>>21
     assert rotation==0, 'Rotated tiles need a tested conversion'
     cells.append(dict(row=row,column=col,tile=t['tilesetIndex'],flags=t['_flags'],orientation=t['orientationMask'],variation=t['variationIndex'],rotation=rotation))
   assert len({(c['row'],c['column']) for c in cells})==len(cells)
   levels.append(dict(name=Path(name).stem,sourceSha256=hashlib.sha256(raw).hexdigest(),rows=s['_rows'],columns=s['_columns'],position=tr['m_LocalPosition'],cellSize=s['_cellSize'],cells=cells))
  out=ROOT/'Assets/Editor/NativeTilemapMigration'; out.mkdir(parents=True,exist_ok=True)
  (out/'levels.json').write_text(json.dumps(dict(levels=levels),indent=2),encoding='utf-8')
  atlas=Image.open(io.BytesIO(archive.read('Assets/TileBrushes/woodTest1 Autotile/atlas.png'))); assert atlas.size==(256,256)
  textures=ROOT/'Assets/NativeTiles/Textures'; textures.mkdir(parents=True,exist_ok=True)
  for i in range(64):
   x,y=i%8*32,i//8*32; atlas.crop((x,y,x+32,y+32)).save(textures/f'Wood_{i:02}.png')
  reports=ROOT/'MigrationReports/reference-maps';reports.mkdir(parents=True,exist_ok=True)
  for l in levels:
   preview=Image.new('RGBA',(l['columns']*32,l['rows']*32))
   for c in l['cells']:
    i=c['tile'];x,y=i%8*32,i//8*32
    preview.paste(atlas.crop((x,y,x+32,y+32)),(c['column']*32,c['row']*32))
   preview.save(reports/(l['name']+'.png'))
  report=dict(levels=len(levels),occupiedCells=sum(len(l['cells']) for l in levels),positions=list({str(l['position']) for l in levels}))
  (ROOT/'MigrationReports/extraction.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(report)
if __name__=='__main__': export()
