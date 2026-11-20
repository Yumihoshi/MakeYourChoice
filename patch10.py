p = r"D:\Projects\UnityProjects\MakeYourChoice\docs\superpowers\brainstorm\2026-11-20-ante-options.html"
t = open(p, encoding="utf-8").read()
t = t.replace("储藏室 6.6 m 面宽（西墙凸出大厅 3 m）", "储藏室 6.6 m 面宽（西墙凸出大厅 2.7 m）", 1)
open(p, "w", encoding="utf-8").write(t)
print("ok")
