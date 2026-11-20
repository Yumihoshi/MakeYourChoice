p = r"D:\Projects\UnityProjects\MakeYourChoice\docs\superpowers\brainstorm\2026-11-20-ante-options.html"
t = open(p, encoding="utf-8").read()
reps = []

reps.append(("const S = 34, X0 = -9.85, Z0 = 13.5;", "const S = 30, X0 = -12.85, Z0 = 13.5;"))
reps.append(('<svg viewBox="0 0 700 700" role="img">', '<svg viewBox="0 0 700 630" role="img">'))

reps.append(("""  const row = [-8.3, -7.5, -6.7];
  for (const c of row) s += R(c - 0.39, 12.15, c + 0.39, 12.75, 'cab');
  for (const c of row) s += R(c - 0.39, 4.95, c + 0.39, 5.55, 'cab');
  // 内门两侧（储藏室内，贴隔墙 A）
  s += R(-6.1, 9.2, -5.4, 10.1, 'cab');
  s += R(-6.1, 11.9, -5.4, 12.8, 'cab');
  // 假门两侧（贴西墙）
  s += R(-9.0, 9.2, -8.3, 10.1, 'cab');
  s += R(-9.0, 11.9, -8.3, 12.8, 'cab');""",
"""  const row = [-11.02, -10.14, -9.26, -8.38, -7.5];
  for (const c of row) s += R(c - 0.39, 12.15, c + 0.39, 12.75, 'cab');
  for (const c of row) s += R(c - 0.39, 4.95, c + 0.39, 5.55, 'cab');
  // 内门两侧（储藏室内，贴隔墙 A）
  s += R(-6.1, 9.2, -5.4, 10.1, 'cab');
  s += R(-6.1, 11.9, -5.4, 12.8, 'cab');
  // 假门两侧（贴西墙）
  s += R(-12.0, 9.2, -11.3, 10.1, 'cab');
  s += R(-12.0, 11.9, -11.3, 12.8, 'cab');"""))

reps.append(("  s += R(-9.0, 4.815, 9.0, 12.815, 'floor');", "  s += R(-12.0, 4.815, 9.0, 12.815, 'floor');"))
reps.append(("  s += R(-9.3, 4.5, -9.0, 13.115, 'wall');", "  s += R(-12.3, 4.5, -12.0, 13.115, 'wall');"))
reps.append(("  s += R(-9.3, 12.815, 9.3, 13.115, 'wall');", "  s += R(-12.3, 12.815, 9.3, 13.115, 'wall');"))
reps.append(("  s += R(-9.3, 4.5, r0, 4.8, 'wall');", "  s += R(-12.3, 4.5, r0, 4.8, 'wall');"))
reps.append(("  s += R(-9.0, 10.3, -8.9, 11.8, 'fake');", "  s += R(-12.0, 10.3, -11.9, 11.8, 'fake');"))

reps.append(("  s += T(-7.2, 9.2, '3.6 m', 'small');", "  s += T(-7.2, 9.2, '6.6 m', 'small');"))
reps.append(("  s += T(-8.45, 9.85, '假门', 'small');", "  s += T(-11.5, 9.85, '假门', 'small');"))
reps.append(("  s += T(-7.5, 6.25, '空柜 ×3', 'small');", "  s += T(-8.7, 6.25, '空柜 ×5', 'small');"))

reps.append(("储藏室 3.6 m 面宽、净深 8.0 m；", "储藏室 6.6 m 面宽（西墙凸出大厅 3 m）、净深 8.0 m；"))
reps.append(("三区改为 <b>3.6 / 4.1 / 9.7</b>。", "三区改为 <b>6.6 / 4.1 / 9.7</b>。"))

for old, new in reps:
    assert old in t, old[:50]
    t = t.replace(old, new, 1)
open(p, "w", encoding="utf-8").write(t)
print("ok")
