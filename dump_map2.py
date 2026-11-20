import openpyxl
from openpyxl.utils import get_column_letter
p=r"D:\Projects\UnityProjects\MakeYourChoice\docs\后室游戏交互节点_1-2 (1) (1).xlsx"
wb=openpyxl.load_workbook(p)
ws=wb["办公室区域"]
def sym(cell):
    f=cell.fill
    v=cell.value
    if f is None or not f.fill_type:
        return "*" if v not in (None,"") else "."
    sc=f.start_color
    if sc.type=="rgb":
        return "D" if str(sc.rgb)=="FFFFC000" else "?"
    th=getattr(sc,"theme",None); tint=getattr(sc,"tint",None) or 0
    if th==6 and tint<0: return "#"
    if th==6 and tint>0.2: return "o"
    if th==6: return "D"
    if th==0 and tint<-0.25: return "G"
    if th==0: return "g"
    if th==2: return "*" if v not in (None,"") else "="
    return "?"
lines=[]
hdr="     "+"".join(get_column_letter(c)[-1] for c in range(18,99))
lines.append(hdr)
for r in range(8,38):
    lines.append(f"R{r:02d}  "+"".join(sym(ws.cell(row=r,column=c)) for c in range(18,99)))
lines.append("")
# list non-empty cells with absolute coords in R..CT
for r in range(8,38):
    for c in range(18,99):
        cell=ws.cell(row=r,column=c)
        if cell.value not in (None,""):
            lines.append(f"{cell.coordinate}\t{cell.value!r}\tfill={sym(cell)}")
open("xlsx_map2.txt","w",encoding="utf-8").write("\n".join(lines))
print("ok")
