# Bütün spritləri bir atlasa yığır və Aseprite üçün açma (unpack) Lua skriptini yaradır.
import json, re
from PIL import Image
import build as B

A_DIR = "C:/Users/Lenovo/Desktop/everything/aseprite/Mehelle_Macerasi/v2/"
U_DIR = "C:/Users/Lenovo/My project/Assets/MehelleMacerasi/Art/"

import os
flat = B.flat()
SUB = os.environ.get('SUBSET')
TAG = os.environ.get('TAG', '')
if SUB:
    keep = set(getattr(B, SUB))
    flat = {k: v for k, v in flat.items() if k in keep}
items = sorted(flat.items(), key=lambda kv: (-kv[1][1].h, kv[0]))

# rəf (shelf) yerləşdirmə
AW = 512
x = y = rowh = 0
place = {}
for name, (folder, im) in items:
    if x + im.w > AW:
        x = 0
        y += rowh + 1
        rowh = 0
    place[name] = (folder, x, y, im.w, im.h)
    x += im.w + 1
    rowh = max(rowh, im.h)
AH = y + rowh
atlas = Image.new('RGBA', (AW, AH), (0, 0, 0, 0))
for name, (folder, im) in items:
    f, px, py, w, h = place[name]
    atlas.alpha_composite(im.pil(), (px, py))
atlas.save(f'out/atlas{TAG}.png')

# Aseprite mənbə qrupları: animasiyalar bir faylda, kadrlar və etiketlərlə
groups = {}
for name, (folder, _, _, w, h) in place.items():
    m = re.match(r'^(.*)_(down|up|side)_(\d)$', name)
    if m and folder == 'Characters':
        key, tag, idx = m.group(1), m.group(2), int(m.group(3))
        order = {'down': 0, 'up': 1, 'side': 2}[tag] * 3 + idx
    else:
        m2 = re.match(r'^(.*)_(\d)$', name)
        if m2 and folder != 'Tiles':
            key, tag, idx = m2.group(1), None, int(m2.group(2))
            order = idx
        else:
            key, tag, order = name, None, 0
    groups.setdefault((folder, key), []).append((order, name, tag))

def lua_str(s):
    return '"' + s.replace('\\', '/').replace('"', '\\"') + '"'

M = ",".join("{%s,%s,%d,%d,%d,%d}" % (lua_str(f), lua_str(n), px, py, w, h) for n, (f, px, py, w, h) in place.items())
G = []
for (folder, key), fr in sorted(groups.items()):
    fr.sort()
    names = ",".join(lua_str(n) for _, n, _ in fr)
    tags = []
    cur = None
    for i, (_, n, t) in enumerate(fr):
        if t and (cur is None or cur[0] != t):
            cur = [t, i + 1, i + 1]
            tags.append(cur)
        elif t:
            cur[2] = i + 1
    tl = ",".join("{%s,%d,%d}" % (lua_str(t), a, b) for t, a, b in tags)
    G.append("{%s,%s,{%s},{%s}}" % (lua_str(folder), lua_str(key), names, tl))

lua = f'''local A={lua_str(A_DIR)}
local U={lua_str(U_DIR)}
local atlas=Image{{fromFile=A.."atlas{TAG}.png"}}
local M={{{M}}}
local G={{{",".join(G)}}}
local imgs={{}}
local n=0
for _,m in ipairs(M) do
  local im=Image(m[5],m[6],ColorMode.RGB)
  im:drawImage(atlas, Point(-m[3],-m[4]))
  app.fs.makeAllDirectories(U..m[1])
  im:saveAs(U..m[1].."/"..m[2]..".png")
  imgs[m[2]]=im
  n=n+1
end
local s=0
for _,g in ipairs(G) do
  local w,h=0,0
  for _,nm in ipairs(g[3]) do local im=imgs[nm]; if im.width>w then w=im.width end; if im.height>h then h=im.height end end
  local spr=Sprite(w,h,ColorMode.RGB)
  spr.layers[1].name="Art"
  for i,nm in ipairs(g[3]) do
    if i>1 then spr:newEmptyFrame(i) end
    spr:newCel(spr.layers[1], i, imgs[nm], Point(0,0))
  end
  for _,t in ipairs(g[4]) do local tg=spr:newTag(t[2],t[3]); tg.name=t[1] end
  app.fs.makeAllDirectories(A..g[1])
  spr:saveAs(A..g[1].."/"..g[2]..".aseprite")
  spr:close()
  s=s+1
end
return {{ok=true,pngs=n,sources=s,atlas_w=atlas.width,atlas_h=atlas.height}}
'''
open(f'out/unpack{TAG}.lua', 'w', encoding='utf-8').write(lua)
print('atlas', AW, AH, 'sprites', len(place), 'groups', len(groups), 'lua bytes', len(lua))
