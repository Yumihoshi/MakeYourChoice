---
description: 新增中文字体或世界空间 TMP 文字时使用。
---

# 中文字体与世界空间文字

改动本域代码前通读本文件；项目级约束见根 `AGENTS.md`，同目录其他条目按需加载。

## 中文字体资产

`Assets/Fonts/SourceHanSansCN-Medium SDF.asset`（思源黑体 Medium，OFL 可随游戏分发；Dynamic + 4096 图集，已预烘 `Assets/Fonts/常用字.txt` 的 3654 字，生僻字运行时会自动补字形）。

UI 中文一律用它，不要运行时自建字体资产。世界空间 TMP 文字的朝向：正面法线 = `(sinθ, 0, cosθ)`（θ=eulerAngles.y），θ=0 时正面朝 -Z、相机在 -Z 侧才看得到正面；

字形「→」指向 `(cosθ, 0, -sinθ)`。想同时"字朝玩家"又"箭头指向某方向"要先算这个组合，不要靠补 180° 兜底（会看到背面镜像）
