import openpyxl
p=r"D:\Projects\UnityProjects\MakeYourChoice\docs\后室游戏交互节点_1-2 (1) (1).xlsx"
wb=openpyxl.load_workbook(p)
ws=wb["办公室区域"]
def sym(cell):
    f=cell.fill; v=cell.value
    if f is None or not f.fill_type: return "." if v in (None,"") else "T"
    sc=f.start_color
    if sc.type=="rgb": return "D" if str(sc.rgb)=="FFFFC000" else "?"
    th=getattr(sc,"theme",None); tint=getattr(sc,"tint",None) or 0
    if th==6 and tint<0: return "#"
    if th==6 and tint>0.2: return "o"
    if th==6: return "D"
    if th==0 and tint<-0.25: return "G"
    if th==0: return "g"
    if th==2: return "T"
    return "?"
for r in range(8,34):
    runs=[]; cur=None; start=None
    for c in range(18,72):
        s=sym(ws.cell(row=r,column=c))
        if s!=cur:
            if cur not in (None,"."): runs.append(f"{cur}{start}-{c-1}")
            cur=s; start=c
    if cur not in (None,"."): runs.append(f"{cur}{start}-71")
    if runs: print(f"R{r}: "+" ".join(runs))
