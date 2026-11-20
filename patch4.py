p = r"D:\Projects\UnityProjects\MakeYourChoice\docs\superpowers\brainstorm\2026-11-20-ante-options.html"
t = open(p, encoding="utf-8").read()
reps = []

# 1) 储藏室门移到右墙中间
reps.append(("""  s += R(-5.4, 4.8, -5.1, 10.3, 'wall');
  s += R(-5.4, 11.8, -5.1, 12.815, 'wall');
  s += R(-5.4, 10.3, -5.1, 11.8, 'door');
  s += `<line x1="${px(-5.1)}" y1="${py(11.8)}" x2="${px(-5.9)}" y2="${py(11.0)}" class="leaf"/>`;""",
"""  s += R(-5.4, 4.8, -5.1, 8.05, 'wall');
  s += R(-5.4, 9.55, -5.1, 12.815, 'wall');
  s += R(-5.4, 8.05, -5.1, 9.55, 'door');
  s += `<line x1="${px(-5.1)}" y1="${py(8.05)}" x2="${px(-5.9)}" y2="${py(8.8)}" class="leaf"/>`;"""))

# 2) 假门也居中
reps.append(("  s += R(-12.0, 10.3, -11.9, 11.8, 'fake');", "  s += R(-12.0, 8.05, -11.9, 9.55, 'fake');"))

# 3) 门两侧柜子
reps.append(("""  s += R(-6.1, 9.2, -5.4, 10.1, 'cab');
  s += R(-6.1, 11.9, -5.4, 12.8, 'cab');
  // 假门两侧（贴西墙）
  s += R(-12.0, 9.2, -11.3, 10.1, 'cab');
  s += R(-12.0, 11.9, -11.3, 12.8, 'cab');""",
"""  s += R(-6.1, 6.3, -5.4, 7.4, 'cab');
  s += R(-6.1, 10.2, -5.4, 11.3, 'cab');
  // 假门两侧（贴西墙）
  s += R(-12.0, 6.3, -11.3, 7.4, 'cab');
  s += R(-12.0, 10.2, -11.3, 11.3, 'cab');"""))

reps.append(("  s += T(-11.5, 9.85, '假门', 'small');", "  s += T(-11.5, 7.4, '假门', 'small');"))

# 4) 三案参数：实心块让到门洞以南，路线/视线对准居中的门
reps.append(("""const cfgA = {
  red: [-3.1, -1.6],
  divB: [-1.0, -0.7],
  mass: [-5.1, 4.815, -3.1, 9.0],
  path: [[-2.35, 4.95], [-2.35, 10.55], [-4.95, 10.55], [-5.35, 10.9]],
  sight: [[-2.35, 4.95], [-5.4, 11.05]],
  blocked: true,
  anteLabel: [-2.05, 9.7], anteW: '4.1', legLabel: [-2.05, 5.6], massLabel: [-4.1, 6.6],
  printX: 4.2, printW: '9.7',
  sightNote: [-3.9, 8.35]
};""",
"""const cfgA = {
  red: [-3.1, -1.6],
  divB: [-1.0, -0.7],
  mass: [-5.1, 4.815, -3.1, 8.05],
  path: [[-2.35, 4.95], [-2.35, 8.8], [-5.4, 8.8]],
  sight: [[-2.35, 4.95], [-5.4, 8.8]],
  blocked: true,
  anteLabel: [-2.05, 10.6], anteW: '4.1', legLabel: [-2.05, 6.2], massLabel: [-4.1, 6.4],
  printX: 4.2, printW: '9.7',
  sightNote: [-2.55, 7.55]
};"""))

reps.append(("""const cfgB = {
  red: [-4.5, -3.0],
  divB: [-1.35, -1.05],
  mass: [-3.0, 4.815, -1.35, 9.0],
  path: [[-3.75, 4.95], [-3.75, 10.55], [-5.2, 10.55]],
  sight: [[-3.75, 4.95], [-5.4, 11.05]],
  blocked: false,
  anteLabel: [-4.1, 9.7], anteW: '3.75', legLabel: [-4.1, 5.6], massLabel: [-2.15, 6.6],
  printX: 4.0, printW: '10.05',
  sightNote: [-5.15, 8.2]
};""",
"""const cfgB = {
  red: [-4.5, -3.0],
  divB: [-1.35, -1.05],
  mass: [-3.0, 4.815, -1.35, 9.0],
  path: [[-3.75, 4.95], [-3.75, 8.8], [-5.4, 8.8]],
  sight: [[-3.75, 4.95], [-5.4, 8.8]],
  blocked: false,
  anteLabel: [-4.1, 10.6], anteW: '3.75', legLabel: [-4.1, 6.2], massLabel: [-2.15, 6.4],
  printX: 4.0, printW: '10.05',
  sightNote: [-4.6, 7.55]
};"""))

reps.append(("""const cfgC = {
  red: [-4.5, -3.0],
  divB: [-1.35, -1.05],
  mass: [-5.1, 6.6, -3.5, 9.8],
  path: [[-3.75, 4.95], [-3.75, 6.15], [-2.4, 6.15], [-2.4, 10.2], [-4.7, 10.2], [-5.3, 10.85]],
  sight: [[-3.75, 4.95], [-5.4, 11.05]],
  blocked: true,
  anteLabel: [-3.0, 5.75], anteW: '3.75', legLabel: [-2.45, 8.1], massLabel: [-4.3, 8.25],
  printX: 4.0, printW: '10.05',
  sightNote: [-4.45, 9.15]
};""",
"""const cfgC = {
  red: [-4.5, -3.0],
  divB: [-1.35, -1.05],
  mass: [-5.1, 5.2, -3.5, 8.05],
  path: [[-3.75, 4.95], [-3.75, 5.05], [-2.4, 5.05], [-2.4, 8.8], [-5.4, 8.8]],
  sight: [[-3.75, 4.95], [-5.4, 8.8]],
  blocked: true,
  anteLabel: [-2.6, 10.6], anteW: '3.75', legLabel: [-2.45, 7.2], massLabel: [-4.3, 6.6],
  printX: 4.0, printW: '10.05',
  sightNote: [-2.55, 7.55]
};"""))

# 5) 文案
reps.append(("<p>实心块从 z=6.6 开始，门前留 1.8 m 门厅。玩家先直走、再右转绕块、再左转看到储藏室门；",
             "<p>实心块从 z=5.2 开始，正挡在红门与储藏室门之间。玩家进门后先直走一小段、再右转绕块、再左转看到储藏室门；"))
reps.append(("<h2>C · 门不动，实心块让开门前转身区 <span class=\"tag warn\">多一次转弯</span></h2>",
             "<h2>C · 门不动，实心块挡在正前方 <span class=\"tag warn\">多一次转弯</span></h2>"))
reps.append(("<td>西南实心块 2.0×4.2，腿在东 2.1 m</td>", "<td>西南实心块 2.0×3.2，腿在东 2.1 m</td>"))
reps.append(("<td>西南块 1.6×3.2，门前留 1.8 m 门厅</td>", "<td>西南块 1.6×2.85，挡在红门与储藏室门之间</td>"))
reps.append(("<td>直走 1.8 m → 右转 → 左转</td>", "<td>直走一小段 → 右转 → 左转</td>"))
reps.append(("<td>直走约 4 m，左转见门</td>", "<td>直走约 3.8 m，左转见门</td>"))

for old, new in reps:
    assert old in t, old[:60]
    t = t.replace(old, new, 1)
open(p, "w", encoding="utf-8").write(t)
print("ok")
