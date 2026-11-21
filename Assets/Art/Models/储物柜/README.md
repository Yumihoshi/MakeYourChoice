# 文件柜（Filing Cabinet）

PSX 风格四抽屉铁皮文件柜，已拆成「柜体 + 4 个独立抽屉」的节点结构，方便按交互单独拉开某一格。

- 来源：Sketchfab — *Filing Cabinet* by **SketchyBot**
  <https://sketchfab.com/models/d4956ed3b7dd49059899e8df45e0111d>
- 许可：Sketchfab **Free Standard + NoAI**（不是 CC 许可；商用或再分发前请按原页面条款复核）
- 原始下载：`source/FilingCabinet.fbx`（带骨骼和一条开合动画，仅作备份）
- 可用模型：`source/FilingCabinet_Separated.fbx`（无骨骼、无动画，抽屉独立）

## 节点结构

导入 Unity 后的层级（根节点名取自文件名，拖进场景后可改成 `FilingCabinet`）：

```text
FilingCabinet_Separated      localPosition (0, 0, 0)，rotation 0，scale 1
├─ Body                      1.0 × 1.8 × 1.0 m，原点在底面中心
├─ Drawer1                   0.9 × 0.35 × 0.87 m，localPosition (0, 1.575, 0.065)
├─ Drawer2                   0.9 × 0.35 × 0.87 m，localPosition (0, 1.130, 0.065)
├─ Drawer3                   0.9 × 0.35 × 0.87 m，localPosition (0, 0.680, 0.065)
└─ Drawer4                   0.9 × 0.35 × 0.87 m，localPosition (0, 0.230, 0.065)
```

- 所有节点的 rotation 都是 identity，scale 都是 1。
- 柜体正面朝 Unity **+Z**，抽屉拉开方向就是各自的 **local +Z**。
- 抽屉 pivot 在自身包围盒中心；只做平移，不要绕 pivot 旋转。
- 建议行程 **0.35–0.45 m**。拉到 0.6 m 时抽屉重心已经越过柜口、会明显悬空，再长就不真实了。
- 抽屉是完整盒体（不是只有一块面板），拉出后能看到侧壁和底板。
- 没有烘焙 AnimationClip，这是故意的：留给代码按交互控制开合距离、速度和目标抽屉。

## 材质与贴图

FBX 内嵌两个 URP Lit 材质，导入时已自动重链到项目贴图：

| 材质 | 贴图 | 说明 |
| --- | --- | --- |
| `FilingCabinet1` | `textures/FilingCabinet1.png`（512×512） | 柜体 |
| `Drawer` | `textures/FilingCabinet1Drawer.png`（256×256） | 抽屉（四格共用） |

Smoothness 0、Metallic 0，是哑光铁皮效果；需要更亮/更旧可以在材质上改 `_BaseColor` 或换贴图。

## 接入建议

1. 拖进场景后把根节点改名为 `FilingCabinet`。
2. `Body` 加一个 `BoxCollider`（center `(0, 0.9, 0)`，size `(1, 1.8, 1)`），让玩家走不过去。
3. 每个 `Drawer` 加一个 `BoxCollider`（size `(0.9, 0.35, 0.87)`），用于交互射线命中「具体哪一格」。
4. 交互时记录抽屉的闭合 `localPosition`，打开就是：

   ```csharp
   drawer.DOLocalMove(closedLocalPosition + Vector3.forward * 0.4f, 0.35f);
   ```

   按项目约定，等补间用 `await tween.WaitForCompletionAsync(ct)`，不要用协程。
5. 想做「只开瞄准的那一格」：每个抽屉挂一个 `InteractableBase` 子类，或在柜体父级用 `hit.collider` 反查对应的 `Drawer` 再驱动。

## 预处理说明

- 拆分脚本为一次性工作脚本，已清理（结果已固化在 FBX 中）。
- 可编辑 Blender 源文件：`Tools~/FilingCabinet/FilingCabinet_Separated.blend`（贴图已 pack，Unity 不导入 `Tools~`）
- 原始 FBX 的贴图路径指向作者机器，Unity 导入时按同名贴图自动重链到了本目录 `textures/`。
- 导出参数使用 `FBX_SCALE_UNITS` + `bake_space_transform`，所以 Unity 根节点是 identity，不会出现 scale 100 / rotation 270°X。
