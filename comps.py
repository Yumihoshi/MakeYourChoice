import openpyxl
from openpyxl.utils import get_column_letter
p=r"D:\Projects\UnityProjects\MakeYourChoice\docs\后室游戏交互节点_1-2 (1) (1).xlsx"
wb=openpyxl.load_workbook(p)
ws=wb["办公室区域"]
def sym(cell):
    f=cell.fill
    v=cell.value
    if f is None or not f.fill_type:
        return None
    sc=f.start_color
    if sc.type=="rgb":
        return "D" if str(sc.rgb)=="FFFFC000" else None
    th=getattr(sc,"theme",None); tint=getattr(sc,"tint",None) or 0
    if th==6 and tint<0: return "wall"
    if th==6 and tint>0.2: return "furn"
    if th==6: return "door"
    return None
cells={}
for r in range(8,38):
    for c in range(18,99):
        s=sym(ws.cell(row=r,column=c))
        if s: cells[(r,c)]=s
# connected components per class
def comps(kind):
    seen=set(); out=[]
    for k,v in cells.items():
        if v!=kind or k in seen: continue
        stack=[k]; seen.add(k); pts=[]
        while stack:
            r,c=stack.pop(); pts.append((r,c))
            for dr,dc in ((1,0),(-1,0),(0,1),(0,-1)):
                n=(r+dr,c+dc)
                if n in cells and cells[n]==kind and n not in seen:
                    seen.add(n); stack.append(n)
        rs=[q[0] for q in pts]; cs=[q[1] for q in pts]
        out.append((min(rs),max(rs),min(cs),max(cs),len(pts)))
    return sorted(out)
for kind in ("wall","furn","door"):
    print("=== ",kind)
    for (r0,r1,c0,c1,n) in comps(kind):
        print(f"rows {r0}-{r1} cols {c0}-{c1} ({get_column_letter(c0)}..{get_column_letter(c1)}) cells={n} size={r1-r0+1}x{c1-c0+1}")
