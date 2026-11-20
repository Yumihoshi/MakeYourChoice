import openpyxl
from openpyxl.utils import get_column_letter
p=r"D:\Projects\UnityProjects\MakeYourChoice\docs\后室游戏交互节点_1-2 (1) (1).xlsx"
wb=openpyxl.load_workbook(p)
out=[]
for name in ["办公室区域"]:
    ws=wb[name]
    widths={}
    for col,dim in ws.column_dimensions.items():
        widths[col]=dim.width
    cols=[f"{get_column_letter(c)}:{widths.get(get_column_letter(c),'-')}" for c in range(ws.min_column, ws.max_column+1)]
    out.append("WIDTHS "+" ".join(cols))
    out.append("ROWHEIGHTS "+" ".join(f"{r}:{ws.row_dimensions[r].height}" for r in range(ws.min_row,ws.max_row+1)))
    seen={}
    for r in range(ws.min_row, ws.max_row+1):
        rowstr=[]
        for c in range(ws.min_column, ws.max_column+1):
            cell=ws.cell(row=r,column=c)
            f=cell.fill
            rgb=None
            if f is not None and f.fill_type:
                sc=f.start_color
                rgb=str(sc.rgb) if sc is not None else None
            v=cell.value
            seen[rgb]=seen.get(rgb,0)+1
            key=(rgb or "")
            if v not in (None,""):
                ch="*"
            elif key.endswith("C6A700"):
                ch="#"
            elif key.endswith("E8C34A"):
                ch="o"
            elif key.endswith("FFF2A0"):
                ch="-"
            elif key.endswith("FFFFFF"):
                ch="."
            else:
                ch="?"
            rowstr.append(ch)
        out.append(f"R{r:02d} "+"".join(rowstr))
    out.append("COLORS "+repr(seen))
open("xlsx_map.txt","w",encoding="utf-8").write("\n".join(out))
print("ok")
