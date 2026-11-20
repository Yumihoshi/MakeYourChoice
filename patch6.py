p = r"D:\Projects\UnityProjects\MakeYourChoice\docs\superpowers\brainstorm\2026-11-20-ante-options.html"
t = open(p, encoding="utf-8").read()
reps = []

reps.append(("const S = 30, X0 = -12.85, Z0 = 13.5;", "const S = 30, X0 = -12.85, Z0 = 14.6;"))
reps.append(('viewBox="0 0 700 630"', 'viewBox="0 0 700 660"'))

reps.append(("  s += R(-12.0, 4.815, 9.0, 12.815, 'floor');", "  s += R(-12.0, 4.815, 9.0, 13.965, 'floor');"))
reps.append(("  s += R(-12.3, 4.5, -12.0, 13.115, 'wall');", "  s += R(-12.3, 4.5, -12.0, 14.265, 'wall');"))
reps.append(("  s += R(9.0, 4.5, 9.3, 13.115, 'wall');", "  s += R(9.0, 4.5, 9.3, 14.265, 'wall');"))
reps.append(("  s += R(-12.3, 12.815, 9.3, 13.115, 'wall');", "  s += R(-12.3, 13.965, 9.3, 14.265, 'wall');"))

reps.append(("""  s += R(-5.4, 4.8, -5.1, 8.05, 'wall');
  s += R(-5.4, 9.55, -5.1, 12.815, 'wall');
  s += R(-5.4, 8.05, -5.1, 9.55, 'door');
  s += `<line x1="${px(-5.1)}" y1="${py(8.05)}" x2="${px(-5.9)}" y2="${py(8.8)}" class="leaf"/>`;""",
"""  s += R(-5.4, 4.8, -5.1, 8.63, 'wall');
  s += R(-5.4, 10.13, -5.1, 13.965, 'wall');
  s += R(-5.4, 8.63, -5.1, 10.13, 'door');
  s += `<line x1="${px(-5.1)}" y1="${py(8.63)}" x2="${px(-5.9)}" y2="${py(9.38)}" class="leaf"/>`;"""))

reps.append(("  s += R(o.divB[0], 4.8, o.divB[1], 12.815, 'wall');", "  s += R(o.divB[0], 4.8, o.divB[1], 13.965, 'wall');"))

reps.append(("""  const row = [-11.02, -10.14, -9.26, -8.38, -7.5];
  for (const c of row) s += R(c - 0.39, 12.15, c + 0.39, 12.75, 'cab');""",
"""  const row = [-11.02, -10.14, -9.26, -8.38, -7.5];
  for (const c of row) s += R(c - 0.39, 13.3, c + 0.39, 13.9, 'cab');"""))
reps.append(("  s += R(-6.1, 6.3, -5.4, 7.4, 'cab');", "  s += R(-6.1, 6.8, -5.4, 7.9, 'cab');"))
reps.append(("  s += R(-6.1, 10.2, -5.4, 11.3, 'cab');", "  s += R(-6.1, 10.8, -5.4, 11.9, 'cab');"))
reps.append(("  s += R(-12.0, 6.3, -11.3, 7.4, 'cab');", "  s += R(-12.0, 6.8, -11.3, 7.9, 'cab');"))
reps.append(("  s += R(-12.0, 10.2, -11.3, 11.3, 'cab');", "  s += R(-12.0, 10.8, -11.3, 11.9, 'cab');"))

reps.append(("  s += R(-12.0, 8.05, -11.9, 9.55, 'fake');", "  s += R(-12.0, 8.63, -11.9, 10.13, 'fake');"))

reps.append(("  s += T(-7.2, 9.9, '储藏室');", "  s += T(-7.2, 10.5, '储藏室');"))
reps.append(("  s += T(-7.2, 9.2, '6.6 m', 'small');", "  s += T(-7.2, 9.8, '6.6 m', 'small');"))
reps.append(("  s += T(o.printX, 9.9, '打印室');", "  s += T(o.printX, 10.5, '打印室');"))
reps.append(("  s += T(o.printX, 9.2, o.printW + ' m', 'small');", "  s += T(o.printX, 9.8, o.printW + ' m', 'small');"))
reps.append(("  s += T(-11.5, 7.4, '假门', 'small');", "  s += T(-11.5, 8.0, '假门', 'small');"))
reps.append(("  s += T(-7.5, 11.75, '柜子B', 'small');", "  s += T(-7.5, 12.9, '柜子B', 'small');"))
reps.append(("  s += T(9.55, 13.15, 'N ↑', 'small', 'end');", "  s += T(9.55, 14.265, 'N ↑', 'small', 'end');"))

reps.append(("""  mass: [-5.1, 4.815, -3.1, 8.05],
  path: [[-2.35, 4.95], [-2.35, 8.8], [-5.4, 8.8]],
  sight: [[-2.35, 4.95], [-5.4, 8.8]],
  blocked: true,
  anteLabel: [-2.05, 10.6], anteW: '4.1', legLabel: [-2.05, 6.2], massLabel: [-4.1, 6.4],
  printX: 4.2, printW: '9.7',
  sightNote: [-2.55, 7.55]""",
"""  mass: [-5.1, 4.815, -3.1, 8.63],
  path: [[-2.35, 4.95], [-2.35, 9.38], [-5.4, 9.38]],
  sight: [[-2.35, 4.95], [-5.4, 9.38]],
  blocked: true,
  anteLabel: [-2.05, 11.8], anteW: '4.1', legLabel: [-2.05, 6.2], massLabel: [-4.1, 6.4],
  printX: 4.2, printW: '9.7',
  sightNote: [-2.4, 8.1]"""))

reps.append(("""  mass: [-3.0, 4.815, -1.35, 9.0],
  path: [[-3.75, 4.95], [-3.75, 8.8], [-5.4, 8.8]],
  sight: [[-3.75, 4.95], [-5.4, 8.8]],
  blocked: false,
  anteLabel: [-4.1, 10.6], anteW: '3.75', legLabel: [-4.1, 6.2], massLabel: [-2.15, 6.4],
  printX: 4.0, printW: '10.05',
  sightNote: [-4.6, 7.55]""",
"""  mass: [-3.0, 4.815, -1.35, 9.0],
  path: [[-3.75, 4.95], [-3.75, 9.38], [-5.4, 9.38]],
  sight: [[-3.75, 4.95], [-5.4, 9.38]],
  blocked: false,
  anteLabel: [-4.1, 11.8], anteW: '3.75', legLabel: [-4.1, 6.2], massLabel: [-2.15, 6.4],
  printX: 4.0, printW: '10.05',
  sightNote: [-4.4, 8.0]"""))

reps.append(("""  mass: [-5.1, 5.2, -3.5, 8.05],
  path: [[-3.75, 4.95], [-3.75, 5.05], [-2.4, 5.05], [-2.4, 8.8], [-5.4, 8.8]],
  sight: [[-3.75, 4.95], [-5.4, 8.8]],
  blocked: true,
  anteLabel: [-2.6, 10.6], anteW: '3.75', legLabel: [-2.45, 7.2], massLabel: [-4.3, 6.6],
  printX: 4.0, printW: '10.05',
  sightNote: [-2.55, 7.55]""",
"""  mass: [-5.1, 5.2, -3.5, 8.63],
  path: [[-3.75, 4.95], [-3.75, 5.05], [-2.4, 5.05], [-2.4, 9.38], [-5.4, 9.38]],
  sight: [[-3.75, 4.95], [-5.4, 9.38]],
  blocked: true,
  anteLabel: [-2.6, 11.8], anteW: '3.75', legLabel: [-2.45, 7.2], massLabel: [-4.3, 6.6],
  printX: 4.0, printW: '10.05',
  sightNote: [-2.4, 8.1]"""))

reps.append(("净深 8.0 m；大厅净", "净深 9.15 m；大厅净"))
reps.append(("玩家进门后沿腿直走约 4 m，左转才看到储藏室门", "玩家进门后沿腿直走约 4.4 m，左转才看到储藏室门"))
reps.append(("<td>直走约 3.8 m，左转见门</td>", "<td>直走约 4.4 m，左转见门</td>"))

for old, new in reps:
    assert old in t, old[:60]
    t = t.replace(old, new, 1)
open(p, "w", encoding="utf-8").write(t)
print("ok")
