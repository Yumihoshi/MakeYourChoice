import openpyxl
from openpyxl.utils import get_column_letter
p=r"D:\Projects\UnityProjects\MakeYourChoice\docs\后室游戏交互节点_1-2 (1) (1).xlsx"
wb=openpyxl.load_workbook(p)
ws=wb["办公室区域"]
from collections import Counter
cnt=Counter()
samples={}
for r in range(8,38):
    for c in range(18,99):
        cell=ws.cell(row=r,column=c)
        f=cell.fill
        if f is None or not f.fill_type:
            key=("none",None,None)
        else:
            sc=f.start_color
            key=(f.fill_type, sc.type, getattr(sc,"theme",None), getattr(sc,"indexed",None), getattr(sc,"tint",None), sc.type=="rgb" and sc.rgb or None)
        cnt[key]+=1
        samples.setdefault(key,(cell.coordinate,cell.value))
for k,v in cnt.items(): print(k,v,samples[k])
