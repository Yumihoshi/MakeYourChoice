p = r"D:\Projects\UnityProjects\MakeYourChoice\docs\superpowers\brainstorm\2026-11-20-ante-options.html"
t = open(p, encoding="utf-8").read()
reps = []

reps.append(('viewBox="0 0 760 660"', 'viewBox="0 0 860 660"'))

# 地面：储藏室+前室 / 空腔 / 打印室
reps.append(("  s += R(-12.0, 4.815, 11.3, 13.965, 'floor');",
"""  s += R(-12.0, 4.815, -0.7, 13.965, 'floor');
  s += R(0.6, 4.815, 13.9, 13.965, 'floor');"""))

reps.append(("  s += R(11.3, 4.5, 11.6, 14.265, 'wall');", "  s += R(13.9, 4.5, 14.2, 14.265, 'wall');"))
reps.append(("  s += R(-12.3, 13.965, 11.6, 14.265, 'wall');", "  s += R(-12.3, 13.965, 14.2, 14.265, 'wall');"))
reps.append(("  s += R(4.5, 4.5, 11.6, 4.8, 'wall');", "  s += R(4.5, 4.5, 14.2, 4.8, 'wall');"))

# 前室东墙（保持 divB）+ 打印室西墙（独立，中间留空腔）
reps.append(("  s += R(o.divB[0], 4.8, o.divB[1], 13.965, 'wall');",
"""  s += R(o.divB[0], 4.8, o.divB[1], 13.965, 'wall');
  s += R(0.6, 4.8, 0.9, 13.965, 'wall');"""))

# 桌子：全部挪到蓝门右侧
reps.append(("  const cols = [1.7, 4.1, 6.5, 8.9], rows = [7.2, 9.4, 11.6];",
             "  const cols = [5.8, 8.0, 10.2, 12.4], rows = [7.2, 9.4, 11.6];"))

# 打印室文字与空腔标签
reps.append(("  s += T(o.printX, 6.3, '打印室');", "  s += T(o.printX, 6.3, '打印室');\n  s += T(-0.05, 8.5, '空腔', 'small');"))
reps.append(("  printX: 5.3, printW: '12.0',", "  printX: 7.4, printW: '13.0',"))
reps.append(("  printX: 5.3, printW: '12.0',", "  printX: 7.4, printW: '13.0',"))
reps.append(("  printX: 5.3, printW: '12.0',", "  printX: 7.4, printW: '13.0',"))
reps.append(("  s += T(5.3, 13.4, '12 办公桌（4×3）', 'small');", "  s += T(7.4, 13.4, '12 办公桌（4×3）', 'small');"))
reps.append(("  s += T(11.85, 14.265, 'N ↑', 'small', 'end');", "  s += T(14.45, 14.265, 'N ↑', 'small', 'end');"))

# 前室标签居中（A）
reps.append(("  anteLabel: [-2.05, 11.8], anteW: '4.1',", "  anteLabel: [-3.05, 11.8], anteW: '4.1',"))

# 副标题
reps.append(("打印室 12.0 m 面宽（放 12 张办公桌）；",
             "打印室 13.0 m 面宽，与前室之间留 1.3 m 空腔（不共用墙），蓝门在打印室左下、12 张桌全在蓝门右侧；"))

for old, new in reps:
    assert old in t, old[:60]
    t = t.replace(old, new, 1)
open(p, "w", encoding="utf-8").write(t)
print("ok")
