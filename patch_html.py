# -*- coding: utf-8 -*-
p = r"D:\Projects\UnityProjects\MakeYourChoice\docs\superpowers\brainstorm\2026-11-20-ante-options.html"
t = open(p, encoding="utf-8").read()

reps = []

reps.append(("const S = 40, X0 = -9.85, Z0 = 13.5;", "const S = 34, X0 = -9.85, Z0 = 13.5;"))

reps.append(('<svg viewBox="0 0 830 575" role="img">', '<svg viewBox="0 0 700 700" role="img">'))

reps.append(("s += R(-9.0, 1.65, 9.0, 4.5, 'hallfloor');", "s += R(-9.0, -4.65, 9.0, 4.5, 'hallfloor');"))

reps.append(("  .elev{fill:#d7dbe2;stroke:#9aa5b5}", "  .elev{fill:#d7dbe2;stroke:#9aa5b5}\n  .steel{fill:#b9bec7}\n  .box{fill:#f5c518;stroke:#c9a400;stroke-width:1}"))

old_mid = """  // 中间墙 + 安全门
  s += R(-6.4, 1.35, -0.75, 1.65, 'wall');
  s += R(0.75, 1.35, 6.4, 1.65, 'wall');
  s += R(-0.75, 1.35, 0.75, 1.65, 'door');
  s += `<line x1="${px(-0.75)}" y1="${py(1.65)}" x2="${px(-0.75)}" y2="${py(3.19)}" class="leaf"/>`;
  // 电梯方向（大厅南端，示意）
  s += `<line x1="${px(0)}" y1="${py(1.2)}" x2="${px(0)}" y2="${py(0.75)}" stroke="#6b7280" stroke-width="2" marker-end="url(#arrowGray)"/>`;"""

new_mid = """  // 中间墙 + 安全门（画在大厅正中）
  s += R(-6.4, -0.07, -0.75, 0.23, 'wall');
  s += R(0.75, -0.07, 6.4, 0.23, 'wall');
  s += R(-0.75, -0.07, 0.75, 0.23, 'door');
  s += `<line x1="${px(-0.75)}" y1="${py(0.23)}" x2="${px(-0.75)}" y2="${py(1.77)}" class="leaf"/>`;
  // 南墙 + 电梯门洞
  s += R(-9.3, -4.95, -1.5, -4.65, 'wall');
  s += R(1.5, -4.95, 9.3, -4.65, 'wall');
  s += R(-1.5, -4.95, 1.5, -4.65, 'door');
  s += R(-1.5, -4.62, -0.02, -4.5, 'steel');
  s += R(0.02, -4.62, 1.5, -4.5, 'steel');
  // 电梯（向下裁切，示意）
  s += R(-2.55, -6.6, 2.55, -4.95, 'hallfloor');
  s += R(-2.7, -6.6, -2.55, -4.95, 'wall');
  s += R(2.55, -6.6, 2.7, -4.95, 'wall');
  s += `<line x1="${px(-2.7)}" y1="${py(-6.6)}" x2="${px(2.7)}" y2="${py(-6.6)}" stroke="#8a6d1f" stroke-width="2" stroke-dasharray="5 4"/>`;
  s += R(1.19, -5.81, 1.79, -5.21, 'box');"""

reps.append((old_mid, new_mid))

reps.append(("  s += T(0, 2.55, '安全门', 'small');\n  s += T(0, 0.35, '电梯（大厅南端）', 'small');",
             "  s += T(0, 2.2, '安全门', 'small');\n  s += T(0, -4.35, '电梯门', 'small');\n  s += T(0, -5.5, '电梯', 'small');\n  s += T(1.49, -6.05, '黄箱子', 'small');"))

for old, new in reps:
    assert old in t, old[:60]
    t = t.replace(old, new, 1)

open(p, "w", encoding="utf-8").write(t)
print("ok")
