---
description: 新增/修改道具模型（Assets/Prefabs/Props）、用 Blender 预处理外部 FBX 并烘焙到 Unity 时使用。
---

# 道具模型与外部模型预处理

## 目录约定

- 原始模型：`Assets/Art/Models/<模型名>/source/`（FBX 与贴图原样放入，不再改）
- 处理产物：`Assets/Art/Models/<模型名>/processed/`（`*.fbx` 与 `Meshes/*.asset`）
- 材质：`Assets/Materials/Props/`
- 预制体：`Assets/Prefabs/Props/`

## OldMetalWardrobe（旧铁皮衣柜）

单门铁皮柜，正面朝 **+X**（锁在左、铰链在右）。整体 0.49 × 1.80 × 0.45 m。

```
OldMetalWardrobe（根，identity）
├─ Body            MeshCollider（非凸）
├─ Hinge_FrontDoor 空物体，pivot=(0.225, 1.121, 0.206)
│  └─ Door_Front   MeshCollider（convex）
└─ Hinge_BackDoor  空物体，pivot=(-0.225, 0.900, 0.225)
   └─ Door_Back    MeshCollider（convex）
```

- **只转 Hinge 的 local Y**：`Hinge_FrontDoor` 用 **负角** 外开（−105° 门中心到 (0.421, 0.900, 0.271)），`Hinge_BackDoor` 用 **正角** 外开（+105° 门中心到 (−0.444, 0.900, 0.273)）；符号用反会转进柜内。
- 背板不是单独一片，是从 Body 网格里按 −X 面法线分离出来的，`Door_Back` 厚 0.02 m、比柜体窄 2 mm 留缝。
- Body 网格的隔板已全部拆除（原模型有 4 层，按 y≈0.20–0.27 / 0.40–0.47 / 1.37–1.44 三段直接删三角形，各面为互不连通的薄板，删完只剩顶板 y=1.75–1.80 与底板 y=0.03–0.05），柜内现在是空腔，背板暗门打开即可穿人。需要恢复隔板时从 `source/` 的原始 FBX 重新烘焙。
- 材质 `OldMetalWardrobe_Body.mat` / `_Door.mat` 均为 URP/Lit，`_MetallicGlossMap` 用打包贴图：R=metallic、A=255−roughness（`AITemp/pack_masks.py` 生成）。重新生成时注意 importer：normal 要 NormalMap，albedo sRGB，mask/AO 走线性。

## 外部 FBX 的缩放坑（重要）

Sketchfab 的 game-ready FBX 常把 100 倍缩放烘在节点上（本模型根节点 localScale=100、rotation=(270,0,0)）。Unity 的 `ModelImporter.globalScale`/fileScale 0.01 与之互补，**无法靠导入设置抵消**：`useFileScale=false` 会把模型放大 100 倍；`bakeAxisConversion=true` 只能清掉 90° 旋转，100 缩放仍在。

已验证的做法：保留 Blender 导出设置（`apply_unit_scale=True`、`apply_scale_options='FBX_SCALE_NONE'`、`global_scale=1.0`、`axis_forward='-Z'`、`axis_up='Y'`），导入后在 Unity 里**把顶点烘焙到世界空间**：

- 位置：`t.localToWorldMatrix.MultiplyPoint3x4(v)`；门要再减铰链世界坐标当 pivot
- 法线：`t.rotation * n`（局部网格已由 bakeAxisConversion 预旋转 −90°，乘上 +90 才是世界法线）
- 之后 `RecalculateBounds()` + `RecalculateTangents()`，UV/三角形照搬

脚本：`AITemp/process_wardrobe.py`（Blender 侧拆分/缩放/导出）+ Unity 侧烘焙（execute_code）。注意 `execute_code` 的 safety_checks 会拦 `AssetDatabase.DeleteAsset`，复用 Mesh 资产覆写数据即可。
