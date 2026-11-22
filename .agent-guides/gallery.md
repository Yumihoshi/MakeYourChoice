---
description: 改动艺术馆场景（Gallery.unity：几何烘焙、坠落入场与雾、杯子提交流程、豆袋运镜、门）时使用。
---

# 艺术馆（Gallery）

改动本域前后通读本文件；项目级约束见根 `AGENTS.md`，同目录其他条目按需加载。

规格见 `docs/superpowers/specs/2026-11-22-gallery-design.html`，实施步骤见 `docs/superpowers/plans/2026-11-22-gallery.md`（plan 写完即冻结，改动以本文件与代码为准）。

## 场景与加载

- 场景 `Assets/Scenes/Gallery.unity`，Build index 2（`ProjectSettings/EditorBuildSettings.asset` 里 Level1 → Level2 → Gallery）。
- 入口链路：`Assets/Prefabs/Level/泳池房.prefab` 里 `建筑/竖井/触发_下坠` 的 `PoolDrainExitTrigger.onNextLevel` 持久监听同预制体根上的 `SceneLoadRequest.Load()`（`sceneName = "Gallery"`）。`LoadSceneMode.Single` 会卸载当前所有场景，这就是「同时卸载前两个场景」，不需要额外的卸载代码。
- **持久监听只能在同一个预制体内部连**：`PoolDrainExitTrigger` 在预制体资产里，所以 `SceneLoadRequest` 必须挂在 `泳池房.prefab` 根上；挂到场景里的节点上无法从预制体资产引用。
- 场景骨架（`游戏入口`/`= 玩家`/`= 相机`/`= 对话`/`+ 界面`/`背景音乐`）是从 Level1 复制的：**`= 玩家` 上没有 `PlayerControlLock`、`眼睛/玩家相机` 上没有 `ElevatorRideMotion`**，不要为了「统一」加回来。
- `GameBootstrap` 勾了 `直接进入`（`directEntry`）：跳过开始菜单与电梯到站演出。UIKit 静态重置（`ResetUIKitState`）与 ESC 暂停仍由它负责，所以这个组件不能删。
- **从 Level1 复制根节点会丢掉跨根引用**：`+ 界面/低分辨率画面` 的 `LowResDisplay.renderCamera`、`= 对话/飘字相机` 的 `Dialogue3DOverlay.mainCamera`、`= 对话/飘字实例` 的 `WorldLineView.worldCamera` 都指向另一个根节点 `= 相机/主相机`，逐个 `Instantiate` 复制后会全部变 null。`LowResDisplay` 拿不到渲染相机会打一条 Error 就禁用，`画面` 这个全屏 RawImage 永远没有贴图——UGUI 的无贴图 RawImage 是**白色**，于是直接运行 Gallery 就是满屏白。复制完骨架必须回填这三处（`GameBootstrap.floorDisplay`/`rideMotion` 是 `directEntry` 下故意留空的）。改完可以拿 Level1 做一次逐字段比对来查漏。
- `directEntry` 分支里要先 `_ = UIKit.Root;` 再 `UiNavigation.EnsureSingleEventSystem()`：UIRoot 自带一个旧版事件系统，只有它存在之后才清得掉。没有开始菜单的场景若直接去清，UIRoot 随后会再创建出第二个事件系统，指针事件被两个输入模块各处理一次。

## 几何

- 4 份 Mesh 在 `Assets/Art/Meshes/艺术馆/`，由 `Assets/Editor/Gallery/GalleryBuilder.cs` 的菜单 `Tools ▸ 艺术馆 ▸ 1 生成网格资产` 烘出，再用 `2 摆入当前场景` 放进场景。改地图就改脚本里的 `Map` 字符串再重跑两条菜单（`Place()` 是「同名子物体复用」，重复跑不会堆节点）。
- `Map` 符号：`#` 墙 / `.` 地面 / `R` 中心红毯 / `D` 暗橙标记 / `Y` 杯子 / `O` 门 / `B` 豆袋；一格 **2 m**（`CellSize = 2f`），行按 z 递增。
- **墙体固定 1 m 厚**（`WallThickness = 1f`）并**居中在 2 m 的墙格里**，两边各留 0.5 m 露边，所以墙面之间净宽约 3 m。墙格按四邻定朝向：左右有墙→沿 x 走、厚度压 z；上下有墙→沿 z 走、厚度压 x；两边都有（拐角/丁字）→整格 2×2 填实；孤立墙格→1×1。**不要改成「墙占满整格」**，那是 1 m 格距时代的样子，会让走廊窄回 1 格。
- **列要镜像**：表格第 c 列落在世界 `x ∈ [(51 − c) × 2, (52 − c) × 2]`，`CellCenter` 已含镜像（`(MapWidth − cellX − 0.5f) × CellSize`）；改 `Map` 时照表格抄字符串即可，不要自己再翻一次。
- **地面与天花板按整片投影铺**，不逐格铺：`FootprintPieces` = `(0,0,104,34)` + `(0,34,92,58)`（z ≥ 34 的 x < 6 列是场外空地，避开它），天花板再扣掉 4×4 m 洞口（`HoleRect()` 由 `Hole` 两列两行的格心算出）。逐格铺会在墙脚、墙顶留下 0.5 m 的缝。
- `AddBox` 只写 5 个面（底面不出），**每盒 10 个三角形**；墙网格 = 781 盒 = 7780 三角（776 墙格 + 2 个门楣），顶点 15560。地面 2 个矩形（4 三角）、天花板 5 个矩形（10 三角）、中心毯 1 个 Quad（2 三角）。
- UV 按世界坐标 1 m 一平铺（朝 X 的面用 worldZ、朝 Z 的面用 worldX、竖直方向 worldY；地面/天花用 worldX/worldZ），所以材质 `_BaseMap_ST` 保持 (1,1)、贴图 `wrapMode = Repeat`。
- 中心地毯是一整张 16×16 m Quad（顶点 4，中心 (46,0.01,46)），UV 0–1 且 `中心地毯.png` 是 `Clamp`，**不要给它加平铺或 Repeat**，否则用户要求的「一张图铺满红区」会变成九宫格。
- **中心地毯必须带 MeshCollider**：地面网格虽然铺到了红毯下面，但红毯抬到 y=0.012 会盖住视野，没有碰撞体时玩家会踩到低 1.2 cm 的地面并出现穿地错觉；`2 摆入当前场景` 已给 `中心地毯` 加 `MeshCollider`，不要当成纯装饰删掉。
- 门格上方的门楣（2.44–4.35）烘在墙网格里，场景里没有单独的「门楣」物体。

## 入场坠落与雾

- `GalleryEntryFall`（挂在 `= 关卡/艺术馆/流程/入场坠落`）：出生点在红毯正上方 **(46, 7.6, 46)**，天花板 4×4 m 洞口在格 (x=22,23)×(z=22,23)，即世界 x ∈ [44,48] × z ∈ [44,48]。
- `Awake` 里把 `RenderSettings.fog` 设成黑色线性雾 start 0.2 / end 3.5（当作黑屏用），并 `firstPerson.enabled = false`；`Update` 用 `controller.Move` 自己下落（重力 −22、上限 25 m/s）。
- 落地判定 `controller.isGrounded || position.y <= 0.05`，另加 3 s 超时兜底；落地后 2 s 把雾拉回 4 / 30，再等 1.73 s 交还控制并播 `Gallery_落地`。
- 天花板没有 MeshCollider（只渲染），坠落不会撞到它；玩家胶囊底部贴地时 transform.y ≈ 0，所以 `y <= 0.05` 是可靠的地面线。

## 杯子与提交

- 拾取：`CupPickup` → `GalleryCupStation.ReportPickup()`（返回累计数）→ 用该杯子挂的 `DialogueTrigger` 播 `Gallery_杯子_{n}`（Yarn + 3D 飘字，锚点是 `剧情锚点/锚点_杯子_A|B|C`，与杯子同位置），再 `SetActive(false)`。三条文案按累计数量选，所以杯子随便哪个先捡都对。
- 提交：`SubmitDeskInteract` → `GalleryCupStation.TrySubmit`：不足或已提交时直连 `ShowLine` 飘「还差 N 个杯子。」/「已经交过了。」；够 3 个时走 `submitDialogue.StartDialogue()` 播 Yarn 节点 `Gallery_提交`（「三个杯子都放上去了。」，复用 DialogueTrigger + 3D 飘字/对话框回退），同时 `paintingDoor.Open()`。
- 计数组件挂在 `= 关卡/艺术馆/流程/流程状态`；锚点分工：`锚点_提交区`(提交台正上方 (102.1,1.2,22)，提交台词与动态提示都落在这里)、`锚点_豆袋`(台词飘字)、`锚点_豆袋视线`(躺下看的房间中心)、`锚点_落地`(落地台词)。**豆袋的视线锚点要独立**，否则锚点贴在豆袋上、玩家贴脸时朝向退化。
- 提交台是**运行期可见的立方体台子**（`道具/提交台`，0.8 × 0.9 × 1.2，材质 `Assets/Materials/Art/提交台.mat`，位置 (101, 0.45, 22)），不是道具预制体。**不要改回 `Assets/Prefabs/Level/MetalOfficeDesk.prefab`**：它是坏资产，子节点 `MeshFilter` 的网格与材质引用全是 null，实例化出来完全隐形（Level1 里那些「办公桌组」是 4 个 `CubicleV2.prefab` 拼的，与它无关）。
- `WorldLineView.ShowLine` 同一时刻只允许一条 active，重复调用会 `LogWarning` 并丢弃；`GalleryCupStation.ShowLine` 已先 `DismissActive()`，新加飘字也要照此顺序。
- 杯子预制体 `Assets/Prefabs/Props/杯子.prefab`：根 scale **1**，子节点 `柱体` localScale 12.5、localPosition (0,0.125,0)、localRotation (-90,0,0)（fbx 里 mesh 只有 0.02 单位、高度轴是模型 +Z 且居中，×100 转正再抬半个身高才 0.25 m 高且底部贴地），根上 BoxCollider center (0,0.125,0) size (0.4,0.25,0.4)。**不要把缩放压到根上**：根 scale 0.125 时碰撞盒世界尺寸只剩 0.05 m，准心打不到（踩过）。
- `杯子.fbx` 的导入设置已关掉 `importCameras` / `importLights` / `importAnimation`，别再打开——fbx 自带一台相机和一盏灯，实例化前必须删掉 `Camera`/`Light` 子节点。
- 杯子预制体的 `m_Icon` 用了 `Assets/Gizmos/杯子.png`（Hierarchy/Inspector 图标）；不要再写 `[DrawGizmo]` 场景视图图标，那会在场景里叠一层白方块（内置图标名 `sv_icon_dot3_purple` 在当前管线里取不到）。

## 门

- 两扇门都是从 Level1 `= 关卡/大厅/门/蓝门` 复制后改的（`Object.Instantiate` + `MoveGameObjectToScene`），结构仍是 `AutoDoor(mode=Hinge)` + `铰链`(Rigidbody + HingeJoint) + `铰链/门板`(DoorBox + `Door_Office`)。
- **`AutoDoor.hinge` 的类型是 `HingeJoint` 而不是 `铰链` 的 Transform**（`Assets/Scripts/Level/Door/AutoDoor.cs:29`）。用脚本往里赋 Transform 会被 Unity 静默置 null，必须 `铰链.GetComponent<HingeJoint>()`；漏了这步门照样「打开」，但门板不转（`Awake` 的校验只在 `doorPanel` 不挂在铰链下时报错）。
- `门_白鱼的画`：根 (97,0,13.5)，`铰链` local (1.0,1.22,0.004)、`门板` local (−1.03,0,0) 缩放 (2.06,2.44,0.08)；铰链在 x=98 一侧、绕远离把手的一侧向提交区方向扫 **+95°**（`HingeDoorMotion` 运行期会关掉铰链限位与电机，只有 `hingeOpenAngle` 的符号决定开门方向）。门板比 2 m 门洞宽出的 6 cm 是故意留的，用来盖住框缝。
- `门_提交区`：根 (91.5,0,24) 旋转 (0,270,0)、铰链在 z=24 一侧，**向房间里开 −95°**（`hingeOpenAngle = -95`、`HingeJoint.limits = -95..0`）。用 +95° 朝走廊开会把门板扫进相邻墙垛里，看上去就是「门卡在墙里」。
- **壁厚 1 m 时开门方向要朝空的一侧**：门板铰在门洞的某个边角、转动 95° 后若那一侧是相邻墙垛就会插进墙里。验证办法：把 `铰链.localRotation` 设成 `AngleAxis(hingeOpenAngle, Vector3.up)`，取门板四角世界 XZ → 列 `51 − floor(x / 2)`、行 `floor(z / 2)` → 查 `Map` 是否 `#`，结果必须不含墙格。别用 `Physics.ComputePenetration`（非凸 MeshCollider 恒返回 0），也别把 `TransformPoint(±scale.x * 0.5)` 当门板端点用（单位立方体网格的局部坐标只有 ±0.5，乘缩放会再放大一倍）。
- 门禁不用 `AutoDoor.Locked`（锁住时 `DoorInteract.CanInteract()` 直接 false，玩家连准心都没有），改用 `GalleryDoorInteract : DoorInteract` 重写 `TryOpenDoor()`：未提交时飘「先把杯子交到提交区。」并 `return false`。
- 门板要整块贴墙面外侧、铰链轴压在门洞边角；不要改成把门板塞进门洞厚度里，否则关不严（细节见 `doors.md`）。

## 豆袋运镜

- `BeanbagInteract` 先 `firstPerson.enabled = false`，再对 `= 玩家/眼睛` 做 DOTween：1.2 s 降到 (0, 0.55, 0.25)、仰角 55°、侧倾 8°、`OutBack` 回弹做「陷进去」；播 `Gallery_豆袋`（「我软脚了。」），等 `DialogueEndedEvent` 后再停 0.6 s、0.8 s 起身回原位，`finally` 恢复 `firstPerson.enabled`。
- 起身的 yaw 保持当前朝向，pitch 取 `FirstPersonController.Pitch`（`Assets/Scripts/Player/FirstPersonController.cs` 新增的只读属性），这样交还控制后视角不乱跳。
- **运镜期间不能有其他脚本写 `眼睛` 的 localPosition/localRotation**：`FirstPersonController.Update` 每帧写 `lookPivot.localRotation`，所以必须先禁用组件再动 `眼睛`。
- 豆袋是 Quad + `豆袋.mat` + `YBillboard`（留空 `targetCamera` 用 `Camera.main`），Quad 可见面朝 −Z，靠 billboard 每帧重写旋转所以初始旋转无所谓；「白鱼的画」是静态挂在 z=1.5 北墙前 (97, 2.2, 1.53) 的 Quad，必须 `rot (0,180,0)` 才朝房间。
- 豆袋摆在 **(99.6, 0.85, 9)**：表格格心是 x=101（镜像前 1.5），但 1.7 m 宽的图贴上去会把半边塞进 x=102 的墙体，所以往房间外让 0.75 m。它的碰撞盒给 1.2 × 1.7 × 0.6 的**有厚度**体积——Quad 旋转 90° 后若厚度取 0.1，世界厚度会被压成 0，准心射线时有时无。

## 台词

- `Assets/Yarn/Gallery.yarn`：`Gallery_落地`、`Gallery_豆袋`、`Gallery_杯子_1|2|3`、`Gallery_提交` 六个节点，`TestDialogue.yarnproject` 的 `sourceFiles: **/*.yarn` 自动收录，不用改工程文件。
- 台词换行必须写 `<br>`（项目里 Yarn 与 3D 飘字都按 TMP 富文本走），不要写空行或 `\n`。
- 拾取计数与提交成功句走 Yarn（`Gallery_杯子_1|2|3` 按累计数量挑节点；提交成功固定播 `Gallery_提交`，锚点是 `锚点_提交区`）；**只有「还差 N 个」「已经交过了」这类动态提示直连 `WorldLineView.ShowLine(text, 999, pos)`**，因为数量写不进静态节点。

## 调试工具（传送）

- `Assets/Editor/Gallery/GalleryTeleportTool.cs`：菜单 `Tools/艺术馆/传送/杯子 A|杯子 B|杯子 C|提交区门口`，把「= 玩家」摆到对应目标旁边并转身看向它，方便逐个验证拾取与提交流程。
- **只在播放模式下可用**：四个菜单项各配一个 `[MenuItem(path, true)]` 校验函数（`Application.isPlaying`），编辑模式下菜单是灰的；方法内还有兜底 `Debug.LogWarning`。挪动只发生在本次运行里，停止播放即复原，不会改到场景里的出生点。
- 落点不是写死坐标：按「离目标 1.6 / 2.2 / 2.8 m × 绕 8 个方向」逐个试，要求脚下有水平地面（`hit.normal.y >= 0.9`）且视线射线第一下打到目标本身，所以几何改了也不会传送进墙里。杯子从红毯中心（`CarpetCenter = (46,0,46)`）那侧靠近，提交区门口从走廊（−x）侧靠近。
- 传送前会调 `EndEntryFall`：若 `流程/入场坠落` 的 `GalleryEntryFall` 还开着（入场坠落每帧 `controller.Move` 会把玩家往下拉），先关掉它、把雾恢复 4/30、交还 `FirstPersonController`，否则传送立刻被覆盖。
- **转向必须走 `FirstPersonController.SnapLook(yaw, pitch)`**（`Assets/Scripts/Player/FirstPersonController.cs`）：水平转身每帧由内部 `_yaw` 写回 `transform.rotation`，直接改 `transform.rotation` 会在下一帧被打回原朝向。任何瞬移/改朝向的工具都要用这个方法。
