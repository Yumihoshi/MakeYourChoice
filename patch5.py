p = r"D:\Projects\UnityProjects\MakeYourChoice\docs\superpowers\brainstorm\2026-11-20-ante-options.html"
t = open(p, encoding="utf-8").read()
reps = []

# CSS
reps.append(("""  .sightOk{font-size:12px;fill:#1f8a4c;font-weight:700;paint-order:stroke;stroke:#fff;stroke-width:3px}""",
"""  .sightOk{font-size:12px;fill:#1f8a4c;font-weight:700;paint-order:stroke;stroke:#fff;stroke-width:3px}
  .dim{stroke:#8a6d1f;stroke-width:1;stroke-dasharray:4 3;opacity:.8}
  .dimlbl{font-size:10px;fill:#5c4a16;text-anchor:middle;paint-order:stroke;stroke:#faf7ee;stroke-width:3px;stroke-linejoin:round}"""))

# 尺寸线 helper（插在 T 之后）
reps.append(("""const T = (x, z, s, cls = 'lbl', anchor = 'middle') =>
  `<text x="${px(x).toFixed(1)}" y="${py(z).toFixed(1)}" class="${cls}" text-anchor="${anchor}">${s}</text>`;""",
"""const T = (x, z, s, cls = 'lbl', anchor = 'middle') =>
  `<text x="${px(x).toFixed(1)}" y="${py(z).toFixed(1)}" class="${cls}" text-anchor="${anchor}">${s}</text>`;
const dimH = (z, x1, x2, label) => {
  const y = py(z), xa = px(x1), xb = px(x2);
  return `<line class="dim" x1="${xa.toFixed(1)}" y1="${y.toFixed(1)}" x2="${xb.toFixed(1)}" y2="${y.toFixed(1)}"/>` +
    `<line class="dim" x1="${xa.toFixed(1)}" y1="${(y - 5).toFixed(1)}" x2="${xa.toFixed(1)}" y2="${(y + 5).toFixed(1)}"/>` +
    `<line class="dim" x1="${xb.toFixed(1)}" y1="${(y - 5).toFixed(1)}" x2="${xb.toFixed(1)}" y2="${(y + 5).toFixed(1)}"/>` +
    `<text x="${((xa + xb) / 2).toFixed(1)}" y="${(y - 5).toFixed(1)}" class="dimlbl">${label}</text>`;
};
const dimV = (x, z1, z2, label) => {
  const x0 = px(x), y1 = py(z1), y2 = py(z2);
  return `<line class="dim" x1="${x0.toFixed(1)}" y1="${y1.toFixed(1)}" x2="${x0.toFixed(1)}" y2="${y2.toFixed(1)}"/>` +
    `<line class="dim" x1="${(x0 - 5).toFixed(1)}" y1="${y1.toFixed(1)}" x2="${(x0 + 5).toFixed(1)}" y2="${y1.toFixed(1)}"/>` +
    `<line class="dim" x1="${(x0 - 5).toFixed(1)}" y1="${y2.toFixed(1)}" x2="${(x0 + 5).toFixed(1)}" y2="${y2.toFixed(1)}"/>` +
    `<text x="${(x0 - 5).toFixed(1)}" y="${((y1 + y2) / 2).toFixed(1)}" class="dimlbl" transform="rotate(-90 ${(x0 - 5).toFixed(1)} ${((y1 + y2) / 2).toFixed(1)})">${label}</text>`;
};"""))

# 门标签带宽度
reps.append(("  s += T((o.red[0] + o.red[1]) / 2, 4.15, '红门', 'small');", "  s += T((o.red[0] + o.red[1]) / 2, 4.15, '红门 1.5 m', 'small');"))
reps.append(("  s += T(3.75, 4.15, '蓝门', 'small');", "  s += T(3.75, 4.15, '蓝门 1.5 m', 'small');"))
reps.append(("  s += T(0, 2.2, '安全门', 'small');\n", ""))
reps.append(("  s += T(0, -4.35, '电梯门', 'small');", "  s += T(0, -4.35, '电梯门 3.0 m', 'small');"))
reps.append(("  s += T(6.8, 3.0, '大厅', 'small');", "  s += T(7.2, 3.6, '大厅', 'small');"))

# 大厅尺寸标注
reps.append(("""  // 指北
  s += T(9.55, 13.15, 'N ↑', 'small', 'end');""",
"""  // 大厅尺寸
  s += dimH(2.9, -9.0, 9.0, '18.0 m（净）');
  s += dimV(-9.55, -4.65, 4.5, '9.15 m（净）');
  s += T(3.3, 0.62, '中间墙 12.8 m', 'dimlbl');
  s += T(-7.7, 0.62, '缝隙 2.6 m', 'dimlbl');
  s += T(7.7, 0.62, '缝隙 2.6 m', 'dimlbl');
  s += T(0, -0.62, '安全门 1.5 m', 'dimlbl');
  // 指北
  s += T(9.55, 13.15, 'N ↑', 'small', 'end');"""))

# 副标题比例与大厅尺寸
reps.append(("按统一比例绘制（1 m ≈ 40 px），尺寸以标注为准。储藏室 6.6 m 面宽（西墙凸出大厅 3 m）、净深 8.0 m；",
             "按统一比例绘制（1 m ≈ 30 px），尺寸以标注为准。储藏室 6.6 m 面宽（西墙凸出大厅 3 m）、净深 8.0 m；大厅净 18.0×9.15（含墙 18.6×9.45）；"))

for old, new in reps:
    assert old in t, old[:60]
    t = t.replace(old, new, 1)
open(p, "w", encoding="utf-8").write(t)
print("ok")
