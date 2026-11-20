p = r"D:\Projects\UnityProjects\MakeYourChoice\docs\superpowers\brainstorm\2026-11-20-ante-options.html"
t = open(p, encoding="utf-8").read()
old = "  // 南墙 + 电梯门洞"
new = """  // 大厅东西墙
  s += R(-9.3, -4.95, -9.0, 4.5, 'wall');
  s += R(9.0, -4.95, 9.3, 4.5, 'wall');
  // 南墙 + 电梯门洞"""
assert old in t
t = t.replace(old, new, 1)
open(p, "w", encoding="utf-8").write(t)
print("ok")
