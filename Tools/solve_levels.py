"""Independently solve all original maps, using mirrored horizontal movement."""
import collections
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
levels=json.loads((ROOT/'Assets/Editor/NativeTilemapMigration/levels.json').read_text())['levels']
results=[]
for level in levels:
 if level['name']=='Scene':continue
 walls={(c['column'],c['row']) for c in level['cells']}
 def step(x,y,dx,dy):
  nx,ny=x+dx,y+dy
  return (x,y) if (nx,ny) in walls or not (0<=nx<level['columns'] and 0<=ny<level['rows']) else (nx,ny)
 start=(7,10,9,10); queue=collections.deque([start]); parents={start:None}; goal=None
 while queue:
  state=queue.popleft()
  if state in [(7,1,9,1),(9,1,7,1)]:goal=state;break
  for key,dx,dy in [('W',0,-1),('S',0,1),('A',-1,0),('D',1,0)]:
   nxt=step(state[0],state[1],-dx,dy)+step(state[2],state[3],dx,dy)
   if nxt not in parents:parents[nxt]=(state,key);queue.append(nxt)
 assert goal is not None,level['name']
 route=''
 while parents[goal]:goal,key=parents[goal];route=key+route
 assert not any(p in walls for p in [(7,10),(9,10)])
 results.append(dict(number=int(level['name'].split()[1]),solvable=True,steps=route,states=len(parents)))
results.sort(key=lambda r:r['number'])
assert len(results)==99
(ROOT/'Assets/Resources/Maps/solutions.json').write_text(json.dumps(dict(levels=results),indent=2)+'\n')
(ROOT/'MigrationReports/solvability.txt').write_text('PASS: 99/99 original levels have a solution; mirrored movement; original occupancy; all spawn cells clear.\n')
print('PASS: 99/99 levels solvable')
