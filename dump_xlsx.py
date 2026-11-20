import openpyxl, json
p=r"D:\Projects\UnityProjects\MakeYourChoice\docs\后室游戏交互节点_1-2 (1) (1).xlsx"
wb=openpyxl.load_workbook(p, data_only=True)
out=[]
out.append("SHEETS: "+json.dumps(wb.sheetnames, ensure_ascii=False))
for ws in wb.worksheets:
    out.append(f"=== {ws.title} dims={ws.dimensions} rows={ws.max_row} cols={ws.max_column} images={len(ws._images)}")
    out.append("merged: "+json.dumps([str(r) for r in ws.merged_cells.ranges], ensure_ascii=False))
    for row in ws.iter_rows():
        for c in row:
            if c.value not in (None, ""):
                out.append(f"{c.coordinate}\t{repr(c.value)}")
open("xlsx_dump.txt","w",encoding="utf-8").write("\n".join(out))
print("ok", len(out))
