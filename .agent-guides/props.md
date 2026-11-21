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

## 长廊口假柜（常规柜 + 墙体补洞）

关卡二长廊口 1.44 宽 × 3.00 高的南墙开口用**常规尺寸** `储物柜_54`（0.45 × 1.80 × 0.49，根节点 (0,0,14.53)、rot 270°）堵住，门洞左右与柜顶用补墙封住（三块墙的坐标见 `level2-backrooms.md`）。`Door_Front` −90° 朝更衣室、`Door_Back` +90° 朝长廊，交互脚本见 `doors.md`。

2026-11-21 二修：`OldMetalWardrobe_Body` 的 −X 背板是 8 个单面三角形（x=−0.206），单面碰撞导致玩家从更衣室侧走不回来、回看只看到柜背。已复制网格为 `Assets/Art/Models/储物柜2/processed/Meshes/OldMetalWardrobe_Body_OpenBack.asset`（删掉这 8 个三角形，174→166），目前给 `储物柜_54/Body`（长廊口）与 `门_暗门/Body`（更衣室北墙）的 MeshFilter 与 MeshCollider 用；其它储物柜背后靠墙，继续用原 `OldMetalWardrobe_Body`（有背板）。要再做可穿人的假柜时照此复制网格、删背面三角形，别直接改原网格。

## 更衣室北墙暗门（常规柜 + 墙体补洞）

`泳池房/道具/门_暗门` 与长廊口同一套做法：1.0 m 墙洞（x −21..−20、z 19.45）里放**常规尺寸** `OldMetalWardrobe`（根节点 (-20.5,0,19.45)、rot 90°、scale 1，柜体 0.45 × 1.80 × 0.49），洞左右各补 0.275 × 1.80 × 0.30 墙（`建筑/墙_更衣室北_暗门洞补_西/东`，材质 `Tile_Pool_Wall_0.275`，`_BaseMap_ST=(0.1375,0.9)`），`门楣_暗门` 下移到 y=3.075 对齐柜顶。`Body` 用开背网格，两扇门挂 `CabinetDoorInteract`（外门 −90° 朝更衣室、背门 +90° 朝泵房），玩家可从更衣室穿到泵房。历史：初版把柜体沿世界 X 拉伸 2.04 倍堵满 1.0 m 洞，2026-11-21 修复为常规柜 + 补墙。

玩家胶囊 2026-11-21 缩到 height 1.55 / radius 0.15 / skinWidth 0.02（center (0,0.775,0)；柜内净宽 0.412、开口净高 1.749 可过）。

历史：曾把 `Body` 与两扇门非均匀拉伸到 1.44 × 3.00 撑满门洞。拉伸**不能写在根节点**上：根节点一旦非均匀缩放，铰链转出去的门扇会被父级缩放压窄（实测 root scale z=3.2 时门宽从 1.43 m 掉到 0.448 m——门宽沿局部 Z，转 90° 后落到不受 Z 缩放的轴上）。该方案已废弃（柜体与门板比例失真、玩家余量太小），但这条缩放坑对其它要拉伸的柜体仍然成立。

量尺寸时不要信 `Renderer.bounds`（会读到过期缓存）：用 `mesh.vertices × localToWorldMatrix` 算顶点级 AABB。

## 外部 FBX 的缩放坑（重要）

Sketchfab 的 game-ready FBX 常把 100 倍缩放烘在节点上（本模型根节点 localScale=100、rotation=(270,0,0)）。Unity 的 `ModelImporter.globalScale`/fileScale 0.01 与之互补，**无法靠导入设置抵消**：`useFileScale=false` 会把模型放大 100 倍；`bakeAxisConversion=true` 只能清掉 90° 旋转，100 缩放仍在。

已验证的做法：保留 Blender 导出设置（`apply_unit_scale=True`、`apply_scale_options='FBX_SCALE_NONE'`、`global_scale=1.0`、`axis_forward='-Z'`、`axis_up='Y'`），导入后在 Unity 里**把顶点烘焙到世界空间**：

- 位置：`t.localToWorldMatrix.MultiplyPoint3x4(v)`；门要再减铰链世界坐标当 pivot
- 法线：`t.rotation * n`（局部网格已由 bakeAxisConversion 预旋转 −90°，乘上 +90 才是世界法线）
- 之后 `RecalculateBounds()` + `RecalculateTangents()`，UV/三角形照搬

脚本：`AITemp/process_wardrobe.py`（Blender 侧拆分/缩放/导出）+ Unity 侧烘焙（execute_code）。注意 `execute_code` 的 safety_checks 会拦 `AssetDatabase.DeleteAsset`，复用 Mesh 资产覆写数据即可。
