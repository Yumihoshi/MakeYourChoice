import openpyxl
from openpyxl.utils import get_column_letter
p=r"D:\Projects\UnityProjects\MakeYourChoice\docs\后室游戏交互节点_1-2 (1) (1).xlsx"
wb=openpyxl.load_workbook(p)
ws=wb["艺术馆区域"]
print("dims",ws.dimensions,ws.min_row,ws.max_row,ws.min_column,ws.max_column)
for r in range(ws.min_row,ws.max_row+1):
    for c in range(ws.min_column,ws.max_column+1):
        cell=ws.cell(row=r,column=c)
        if cell.value not in (None,""):
            print(cell.coordinate,repr(cell.value))
