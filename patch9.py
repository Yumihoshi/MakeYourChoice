p = r"D:\Projects\UnityProjects\MakeYourChoice\docs\superpowers\brainstorm\2026-11-20-ante-options.html"
t = open(p, encoding="utf-8").read()
reps = [
("三区改为 <b>6.6 / 4.1 / 9.7</b>。",
 "三区为 <b>6.6 / 4.1 / 13.0</b>；打印室与前室之间留 <b>1.3 m 空腔</b>（两道墙脱开，不共用墙）。"),
("<td>西南实心块 2.0×3.2，腿在东 2.1 m</td><td>是</td><td>大厅北墙 3 块 + 红门整套（铰链/门板/锁/触发/台词点）</td>",
 "<td>西南实心块 2.0×3.8，腿在东 2.1 m</td><td>是</td><td>大厅北墙 3 块 + 红门整套 + 打印室西墙独立（1.3 m 空腔）</td>"),
("<td>东南实心块 1.65×4.2，腿在西 2.1 m</td><td>否（斜看得到）</td><td>只改前室</td>",
 "<td>东南实心块 1.65×4.2，腿在西 2.1 m</td><td>否（斜看得到）</td><td>只改前室 + 打印室西墙独立</td>"),
("<td>西南块 1.6×2.85，挡在红门与储藏室门之间</td><td>是</td><td>只改前室</td>",
 "<td>西南块 1.6×2.85，挡在红门与储藏室门之间</td><td>是</td><td>只改前室 + 打印室西墙独立</td>"),
]
for old, new in reps:
    assert old in t, old[:50]
    t = t.replace(old, new, 1)
open(p, "w", encoding="utf-8").write(t)
print("ok")
