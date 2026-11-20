p = r"D:\Projects\UnityProjects\MakeYourChoice\docs\superpowers\brainstorm\2026-11-20-ante-options.html"
t = open(p, encoding="utf-8").read()
reps = []

reps.append(('viewBox="0 0 700 660"', 'viewBox="0 0 760 660"'))
reps.append(("  s += R(-12.0, 4.815, 9.0, 13.965, 'floor');", "  s += R(-12.0, 4.815, 11.3, 13.965, 'floor');"))
reps.append(("  s += R(9.0, 4.5, 9.3, 14.265, 'wall');", "  s += R(11.3, 4.5, 11.6, 14.265, 'wall');"))
reps.append(("  s += R(-12.3, 13.965, 9.3, 14.265, 'wall');", "  s += R(-12.3, 13.965, 11.6, 14.265, 'wall');"))
reps.append(("  s += R(4.5, 4.5, 9.3, 4.8, 'wall');", "  s += R(4.5, 4.5, 11.6, 4.8, 'wall');"))

# CSS：桌子
reps.append(("""  .dim{stroke:#8a6d1f;stroke-width:1;stroke-dasharray:4 3;opacity:.8}""",
"""  .desk{fill:#e2cf9a;stroke:#b99b52;stroke-width:.8}
  .dim{stroke:#8a6d1f;stroke-width:1;stroke-dasharray:4 3;opacity:.8}"""))

# desks() 函数 + 调用
reps.append(("""function base(o) {
  let s = '';""",
"""function desks() {
  let s = '';
  const cols = [1.7, 4.1, 6.5, 8.9], rows = [7.2, 9.4, 11.6];
  for (const z of rows) for (const x of cols) {
    s += R(x - 0.7, z - 0.25, x + 0.7, z + 0.25, 'desk');
    s += R(x - 0.25, z - 0.7, x + 0.25, z + 0.7, 'desk');
  }
  return s;
}

function base(o) {
  let s = '';"""))

reps.append(("  // 储藏室家具\n  s += cabinets();", "  // 储藏室家具\n  s += cabinets();\n  // 打印室办公桌（4×3）\n  s += desks();"))

# 打印室文字
reps.append(("  s += T(o.printX, 10.5, '打印室');", "  s += T(o.printX, 6.3, '打印室');"))
reps.append(("  s += T(o.printX, 9.8, o.printW + ' m', 'small');", "  s += T(o.printX, 5.75, o.printW + ' m', 'small');"))
reps.append(("  s += T(-7.5, 12.9, '柜子B', 'small');", "  s += T(-7.5, 12.9, '柜子B', 'small');\n  s += T(5.3, 13.4, '12 办公桌（4×3）', 'small');"))

# 指北针
reps.append(("  s += T(9.55, 14.265, 'N ↑', 'small', 'end');", "  s += T(11.85, 14.265, 'N ↑', 'small', 'end');"))

# 三案打印室宽度/中心
reps.append(("  printX: 4.2, printW: '9.7',", "  printX: 5.3, printW: '12.0',"))
reps.append(("  printX: 4.0, printW: '10.05',", "  printX: 5.3, printW: '12.0',"))
reps.append(("  printX: 4.0, printW: '10.05',", "  printX: 5.3, printW: '12.0',"))

# 副标题
reps.append(("大厅净 18.0×9.15（含墙 18.6×9.45）；",
             "打印室 12.0 m 面宽（放 12 张办公桌）；大厅净 18.0×9.15（含墙 18.6×9.45）；"))

for old, new in reps:
    assert old in t, old[:60]
    t = t.replace(old, new, 1)
open(p, "w", encoding="utf-8").write(t)
print("ok")
