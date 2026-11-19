# 非欧几何「空间叠加」效果（2026-11-19）

## 状态

设计已由用户确认（2026-11-19），规格待用户审阅；尚未实现。实现计划将在规格通过后写入 `docs/superpowers/plans/`。

## 目标

在 Level1 大厅的安全门处实现「空间叠加」：

- 安全门打开后，玩家站在门前面向 **+z**，能透过门洞看见 Level2 的 TestRoom 测试走廊，并走进去；
- 玩家在安全门背面（大厅北侧）面向 **-z**，只能看到墙，走不过去；
- 玩家可以原路走回大厅，落在大厅安全门处，保持朝向（面朝南，门在背后）；
- 走廊实际位于 1000 m 外的口袋空间，不和 Level1 的现有几何、灯光、碰撞冲突。

## 非目标

递归传送门、多对传送门、移动传送门、物理物体/NPC 穿门、存档记录玩家在走廊里的位置、跨门阴影/反射、音频遮挡、走廊内的玩法内容、运行时 Additive 加载 `Level2.unity`。

## 现状

- `Assets/Scenes/Level1.unity`：大厅被 `= 关卡/大厅/建筑/中间墙`（12.8×3×0.3，中心 z=1.5，z 1.35..1.65，实心 BoxCollider）隔开；`= 关卡/大厅/门/安全门`（instanceID 57706）是贴在中间墙南面的门板，`AutoDoor(mode=Rise, openHeight=3.0, duration=0.8, autoClose=false)`，`SafeDoorInteract` 在拿到真门禁卡后 `door.Open()` 并 `exitOpened.Invoke()`。`exitOpened` 目前没有持久监听。
- 门板 `门板`（instanceID 56828）世界 z 1.288..1.588、y 0..2.4、x 1.44 宽；中间墙南面 z=1.35。
- 玩家 `= 玩家`：CharacterController（radius 0.3、height 1.8、skinWidth 0.08、center (0,0.9,0)），`FirstPersonController`，眼睛在 local (0,1.6,0)；主相机 `= 相机/主相机`（CinemachineBrain，VCam `玩家相机`，fov 58.7、near 0.1、far 100、cullingMask -9）。
- `+ 界面/低分辨率画面` 的 `LowResDisplay` 把主相机渲染到 **320×180** 的 Point 过滤 RenderTexture，再由 RawImage 显示。
- `Assets/Scenes/Level2.unity` 目前只有 TestRoom 测试走廊（沿 X 的 13.3×1.03×1.73 走廊，默认 Cube 材质，无天花板/端墙）和一个 Main Camera、一个 Directional Light。
- `Assets/Scripts` 下没有任何传送门、切场景或 Additive 加载代码。

## 设计决定

1. **单向传送面 + 瞬移**：安全门处放一张朝南的传送面 P1（大厅侧），走廊入口放一张朝北的传送面 P2（走廊侧）。玩家穿过 P1 进入走廊，穿过 P2 回到大厅。
2. **同一扇门可返回**：P2 只从走廊内可见；大厅北侧仍是实体墙。返回时保持玩家朝向。
3. **走廊截面重建为 1.44×2.40**：与门洞一致，补天花板与尽头封墙。现有 TestRoom 的 1.03×1.73 截面太矮（眼高 1.6 时顶部只剩 0.13 m），不使用。
4. **TestRoom 做成 Prefab + 口袋实例**：`Assets/Prefabs/Level/TestRoom.prefab` 是唯一内容源；`Level2.unity` 保留一个 Prefab 实例作为编辑/测试入口；Level1 在 `= 关卡/口袋空间` 放一个实例，位置 (0,-1000,0)，不参与运行时场景加载。
5. **中间墙不拆成三块**：把现有 `中间墙` 网格用 ProBuilder 布尔减出 1.44×2.4 的洞，保留前表面 UV/贴图连续性；洞的北侧开口用同一网格补面（Cap）还原背面遮挡。若补面 UV 难对齐，退回一块 1.5×2.46 的 `Wall_Mid` 背板。
6. **斜近裁剪 + 屏幕空间采样**：传送相机用 `Camera.CalculateObliqueMatrix` 把目的地墙体内侧作为近裁剪面，避免看到目的地墙后的空间；传送 quad 用屏幕空间 UV 采样 320×180 RT，贴合现有低分辨率画面。

## 空间结构

### 中间墙开洞与补面

- 目标门洞：x ±0.72（宽 1.44），y 0..2.4（高 2.4），贯穿墙厚 z 1.35..1.65。
- 在 `中间墙` 网格上布尔减一个 1.44×2.4×0.3 的盒子；原前表面 UV 保持不变，只有门洞内侧（门套）是新面，UV 允许轻微拉伸。
- 门洞北侧开口用同一网格补一个面，UV 对齐墙北面原贴图区域；补面与墙北面共面（z=1.65），从背面看是连续的墙。
- 碰撞：优先给开洞后的墙网格用静态 `MeshCollider`（非 Convex），洞与补面都参与碰撞；若 CharacterController 在洞口边缘出现卡顿，退化为左右/门楣三个不可见 `BoxCollider` + 补面 `BoxCollider`。
- 中间墙的材质仍是 `Wall_Mid`；门洞侧壁可以按需单独给材质，属美术调整。

### 安全门与 P1

- 安全门、门板、门框、`SafeDoorInteract`、对话、开门动画全部不动。门板仍在 z 1.288..1.588，升起 3.0 m 后藏进墙/天花板。
- **P1 传送 quad**：1.44×2.4，位置 (0,1.2,1.40)，rotation y=0（可见面法线 -z，朝南）。它在门板体积内：关门时被门板挡住，门升起后逐渐露出。
- 玩家胶囊半径 0.3 + skin 0.08，从南侧最近被补面挡在 z≈1.27（补面 z=1.65 减 0.38）。传送触发距离 **0.18 m**（玩家中心到 P1 平面），即 z≈1.22 触发；0.18 > 主相机 near clip 0.1，传送面不会被裁掉。
- 触发时玩家偏到门洞一侧（x 最大 ±0.42），门洞边缘到传送面的夹角 atan(0.30/0.18)≈59° > 水平半 FOV 44°，门洞能铺满屏幕，瞬移不露出墙。

### 口袋空间与 TestRoom Prefab

`Assets/Prefabs/Level/TestRoom.prefab` 的局部坐标：入口在 z=0，走廊沿 +z 延伸到 z=13.3；地板顶面 y=0。

| 部件 | 尺寸 (x × y × z) | 位置/范围 |
| --- | --- | --- |
| 地板 | 1.70 × 0.13 × 13.3 | y -0.13..0，z 0..13.3，x ±0.85 |
| 左墙 | 0.13 × 2.4 × 13.3 | 内表面 x=+0.72，外表面 x=+0.85，y 0..2.4 |
| 右墙 | 0.13 × 2.4 × 13.3 | 内表面 x=-0.72，外表面 x=-0.85 |
| 天花板 | 1.70 × 0.13 × 13.3 | y 2.4..2.53 |
| 尽头封墙 | 1.70 × 2.4 × 0.13 | z 13.3..13.43 |
| 入口侧壁（两根） | 0.13 × 2.4 × 0.13 | x ±0.785，z -0.13..0；中间留 1.44×2.4 开口 |
| 局部点光 | — | 约 (0,2.2,2.5)、(0,2.2,7.0)、(0,2.2,11.5)，range 5–7，强度在编辑器里调 |

- 走廊净空 1.44×2.4，长 13.3 m；地板、墙、天花板、尽头封墙都带碰撞。
- 走廊材质先沿用 TestRoom 现有材质（默认 Cube 材质）；若要贴合大厅，可换成 `Assets/Materials/Level` 下的墙面材质，属美术调整，不影响传送逻辑。
- 房间自带局部点光，不使用全局 Directional Light；大厅光照不受影响。
- **P2 传送 quad**：1.44×2.4，位置 (0,1.2,0)，rotation y=180（可见面法线 +z，朝走廊内）。
- **P2 传送相机**与 **P2 裁剪面**在 Prefab 内；P2 的 quad/相机默认禁用，由 Level1 的 `PortalPair` 在开门后启用。
- **P1/P2 裁剪面标记**：都是空物体，Inspector 里设内置圆点图标。P1 的标记在 `= 关卡/大厅/空间叠加` 下，P2 的在 Prefab 内。
  - P1 的裁剪面在 (0,1.2,1.35)，forward 指向大厅内侧（-z）；它对应中间墙的大厅侧面，供「走廊 → 大厅」的传送相机使用。
  - P2 的裁剪面在 (0,1.2,0)，forward 指向走廊内侧（+z）；它对应走廊入口的室内面，供「大厅 → 走廊」的传送相机使用。

### 场景层级

```text
= 关卡
  ├─ 大厅
  │   ├─ 建筑
  │   │   ├─ 中间墙                 (ProBuilder 网格，带洞+补面)
  │   │   └─ ...
  │   └─ 空间叠加                   (PortalPair)
  │       ├─ 入口传送面             (P1 quad，1.44×2.4，朝南)
  │       ├─ 入口传送相机           (P1 camera，depth -10)
  │       └─ 入口裁剪面             (空物体，z=1.35，forward -z)
  └─ 口袋空间                       (TestRoom.prefab 实例，y=-1000)
      ├─ 地板 / 左墙 / 右墙 / 天花板 / 尽头封墙
      ├─ 点光 × 3
      ├─ 返回传送面                 (P2 quad，朝北，默认禁用)
      ├─ 返回传送相机               (P2 camera，depth -10，默认禁用)
      └─ 返回裁剪面                 (空物体，z=0，forward +z)
```

`Level2.unity` 保留 TestRoom Prefab 的一个实例，替换原来的 Ground/LeftWall/RightWall 三个 Cube；该实例的 P2 quad/相机保持禁用，Level2 场景本身不参与 Level1 运行时。

## 组件与代码

### PortalPair

`Assets/Scripts/Level/Portal/PortalPair.cs`，namespace `Game.Level`，`[DefaultExecutionOrder(1000)]`（排在 CinemachineBrain 之后）。

字段（全部加 Odin `[LabelText("中文")]`）：

- `Transform player`、`CharacterController controller`
- `Camera mainCamera`、`CinemachineBrain brain`
- `AutoDoor safeDoor`
- `PortalSide hallSide`、`PortalSide roomSide`
- `float triggerDistance = 0.18f`、`float landingOffset = 0.25f`
- `Vector2Int resolution = (320, 180)`
- `bool showGuideGizmos = true`（Scene 视图辅助线开关）

职责：

- `Activate()`：由 `SafeDoorInteract.exitOpened` 调用；创建两张 RT、绑定材质、启用 P1/P2 的 quad 与相机；重复调用直接返回。
- `LateUpdate()`：更新两个传送相机的 position/rotation/斜投影；按主相机视锥决定每个传送相机是否渲染；检测触发并执行瞬移。
- `OnDestroy()`：释放两张 RT，清理材质引用。
- `Awake()`/`OnEnable()` 校验引用；缺引用 `Debug.LogError` 并 `enabled = false`。
- `OnDrawGizmos()`：Scene 视图里为每一侧画三张面——**白**是传送面本体，**橙**是触发面（沿该侧可见面法线偏移 `triggerDistance`），**绿**是落点面（同样偏移 `landingOffset`），并在右侧标注两者数值。橙色面就是「玩家跨过后瞬移」的位置，绿色面就是从对侧过来后玩家出现的位置；只用于调参，不参与运行时逻辑，`showGuideGizmos` 关闭时不画。

### PortalSide

`Assets/Scripts/Level/Portal/PortalSide.cs`，`[Serializable]` 数据类：

- `Transform quad`、`Camera camera`、`Transform clipPlane`、`Material material`
- `[NonSerialized] RenderTexture renderTexture`
- `Vector3 FrontNormal => quad != null ? -quad.forward : Vector3.zero`（Unity Quad 的可见面法线是 `-forward`；判空避免未配置时抛异常）。
- `clipPlane` 是该传送面**自己空间一侧**的墙体内表面；它作为**目的地**时，传送相机用它做斜近裁剪。

### PortalSurface.shader / 材质

- `Assets/Shaders/PortalSurface.shader`：URP Unlit；用 `ComputeScreenPos` 得到主相机屏幕 UV，采样 `_MainTex`；`Cull Back`、`ZWrite On`、`ZTest LEqual`；不参与光照/雾。
- `Assets/Materials/Level/PortalSurface_Hall.mat`、`PortalSurface_Room.mat`：两个 quad 各一份材质资产，`_MainTex` 在运行时指向各自的 RT。
- Quad 的 `MeshRenderer`：`shadowCastingMode = Off`、`receiveShadows = false`；不挂 Collider。

### AutoDoor.OpenProgress / IsFullyOpen

在 `Assets/Scripts/Level/Door/AutoDoor.cs` 增加只读属性：

```csharp
public float OpenProgress => _motion != null ? _motion.Progress : 0f;
public bool IsFullyOpen(float epsilon = 0.99f) => OpenProgress >= epsilon;
```

`IDoorMotion` 增加 `float Progress { get; }`（0..1，接近 1 为完全打开；旋转门受电机死区可能略小于 1，消费侧用 `AutoDoor.IsFullyOpen` 判断）；`RiseDoorMotion`、`SlideApartDoorMotion`、`HingeDoorMotion` 各自实现。门的视觉/碰撞/开关逻辑不变。

### 事件接线

`= 关卡/大厅/门/安全门` 的 `SafeDoorInteract.exitOpened` 持久监听 `= 关卡/大厅/空间叠加` 的 `PortalPair.Activate()`。`SafeDoorInteract` 本身不改。

## 传送数学

### 坐标与配对

- P1（大厅侧）：位置 A=(0,1.2,1.40)，可见面法线 n_A=(0,0,-1)，rotation 0。
- P2（走廊侧）：位置 B=(0,1.2,0)（口袋空间内），可见面法线 n_B=(0,0,+1)，rotation y=180。
- 标准旋转：

```text
rot = dest.quad.rotation * Quaternion.Euler(0,180,0) * Quaternion.Inverse(src.quad.rotation)
```

本配对 `rot = identity`。

### 传送相机

对每个传送面 `visible`（玩家看到的 quad）与另一侧 `other`（目的地）：

```text
cam = visible.camera
cam.position = other.quad.position + rot * (mainCamera.position - visible.quad.position)
cam.rotation = rot * mainCamera.rotation
```

- 本配对即纯平移：P1 相机在走廊入口南侧（z≈-0.18）看向 +z；P2 相机在中间墙北侧（z≈1.58）看向 -z。
- 传送相机在目的地背后，玩家越靠近源面，它越贴近目的地入口；玩家穿过时视角连续。

### 斜近裁剪

- 使用**目的地侧**的 `clipPlane`（`other.clipPlane`）：
  - P1 相机（目的地走廊）：裁剪面在走廊入口室内面 z=0，法线朝南（朝相机）。
  - P2 相机（目的地大厅）：裁剪面在中间墙大厅侧面 z=1.35，法线朝北（朝相机）。
- `clipPlane.forward` 指向该侧空间内侧；相机用 `-clipPlane.forward` 作为朝向相机的法线，构造相机空间平面，再 `Camera.CalculateObliqueMatrix` 替换投影矩阵。
- 传送相机投影复制主相机（fov 58.7、16:9）；RT 320×180、Point、无 mip/AA；`UniversalAdditionalCameraData` 复制主相机的 renderer/shadows/AA，**关闭后处理**（后处理由主相机对最终画面统一做）。
- 传送相机不挂 AudioListener、不挂 Cinemachine；`depth = -10`，主相机 `-1`。

### 玩家瞬移

触发条件（全部满足）：

- 玩家与源传送面处于同一空间：`|player.position.y - src.quad.position.y| <= spaceHeightTolerance`（默认 3 m）。两个空间在水平面上完全重叠、只隔 1000 m 竖直距离，单看平面距离两侧都会命中，必须先用竖直距离排除对侧；
- 玩家根节点在源面前方：`dot(player.position - src.quad.position, src.FrontNormal) > 0`；
- 玩家位置增量朝门：`dot(delta, src.FrontNormal) < 0`；
- 到源面距离 ≤ `triggerDistance`（0.18）；
- `safeDoor.IsFullyOpen()`（0.99 容差，覆盖旋转门电机死区）；
- 玩家在门洞侧向范围内（|localX| ≤ 0.72；由墙体碰撞保证，仍做一次显式检查）。

目标位置：

```text
lateral = player.position - src.quad.position - dot(player.position - src.quad.position, src.FrontNormal) * src.FrontNormal
target = other.quad.position + rot * lateral + landingOffset * other.FrontNormal
```

- 本配对 x/y 保留，z 落到目的地平面 + 0.25 m：
  - 进走廊：z=0.25，面朝北；
  - 回大厅：z=1.15（安全门南侧），面朝南，门在背后。
- 朝向：`rot * player.rotation`（本配对不变），不写 `FirstPersonController` 的 `_yaw/_pitch`。

执行顺序：

1. `controller.enabled = false`；
2. `player.position = target`；
3. `controller.enabled = true`；
4. `CinemachineCore.OnTargetObjectWarped(player, delta)`，delta = target - oldPosition。

防乒乓：

- 瞬移后把「上一帧位置」重置为 target；
- 触发要求玩家朝门移动；
- 落点 0.25 m 在目的地面前方，大于 0.18 m 的触发距离，不会立刻反向触发；
- 触发前先用 `spaceHeightTolerance` 确认玩家在源侧空间。两个空间的走廊 z 范围（0..13.3）与大厅门洞 z≈1.4 重叠，缺这一步时玩家在走廊里走到 z≈1.22..1.4 会误触发大厅侧传送，落点 y 每次再叠 -1000 m（实测 -1000 → -2000 → -3000）。

残差：传送相机在目的地背后 0.18 m，玩家落在目的地前方 0.25 m，视角差约 0.43 m；320×180 下预计不明显。若手测可见 pop，再加 0.06–0.08 s 极短淡入作为兜底，不默认开启。

## 渲染与生命周期

- 两张 RT 在 `Activate()` 时创建：`new RenderTexture(320, 180, 24, RenderTextureFormat.ARGB32) { filterMode = Point, antiAliasing = 1, useMipMap = false }`；`Create()` 失败、`IsCreated() == false` 时 `Debug.LogError` 并停止。
- RT 分别赋给两个传送相机的 `targetTexture` 与两个材质的 `_MainTex`。
- 传送相机只在对应 quad 位于主相机视锥内时启用（`GeometryUtility.CalculateFrustumPlanes` + `TestPlanesAABB`）；不可见时关掉相机，避免无谓渲染。
- `OnDestroy()` 释放 RT；材质引用清空。
- 主相机继续用 `LowResDisplay` 的 320×180 RT；两个传送相机各自渲染到独立的 320×180 RT；三者像素密度一致，传送画面不会比主画面更清晰或更糊。
- 未 `Activate()` 时 P1/P2 quad 与相机禁用；门板关着，玩家看不到传送面，也没有 RT 开销。

## 错误处理

- 缺引用（玩家、CharacterController、主相机、CinemachineBrain、安全门、两个 PortalSide 的 quad/camera/clipPlane/material）→ `Debug.LogError` 指明字段名并 `enabled = false`，不静默用默认值。
- RT 创建失败、shader/材质缺失、投影异常 → 报错并停止，不退化成「能跑但画面错」。
- P2 的 quad/相机在 `Level2.unity` 实例里保持禁用是预期状态，不报错。

## 验收

1. **开门前**：安全门关着；从大厅北侧看中间墙只有墙，走不过去；看不到走廊。
2. **拿真卡开门**：门升起，走廊透过门洞逐渐露出；左右移动时有正确视差；中间墙前表面贴图连续，没有三块拼接的接缝。
3. **走进门**：在离传送面约 0.18 m 处无缝进入走廊；相机不飞、无黑帧；走廊 1.44×2.4、有尽头封墙与局部照明。
4. **在走廊里转身**：入口显示的是大厅南侧（不是墙）；走回去落在大厅安全门南侧约 0.25 m，面朝南，门在背后；不会立刻被弹回去。
5. **调参辅助**：选中 `空间叠加`，Scene 视图里能看到白色传送面、橙色触发面与绿色落点面；调整 `triggerDistance` / `landingOffset` 时三张面实时移动，数值标签同步更新。
6. **斜看传送面**：画面正确、边缘与门框对齐、无黑缝；大厅光照不变；走廊灯光不漏进大厅。
7. **开门后再从北侧看**：仍然是墙，走不过去；Console 无报错；传送相机只在可见时渲染。

## 风险与待手测

- ProBuilder 布尔 + 补面的 UV 是否真的连续（前表面与北面）；若补面 UV 难对齐，退回 1.5×2.46 的 `Wall_Mid` 背板。
- 斜投影的裁剪面法线方向（朝相机）是否算对；如果画面出现「目的地墙后」或「黑屏」，优先检查 `clipPlane.forward` 与 `CalculateObliqueMatrix` 的符号。
- 传送相机的执行顺序是否真的在 CinemachineBrain 之后；若移动时有拖影，改用 `Camera.onPreCull` 或调整 `DefaultExecutionOrder`。
- 0.43 m 的视角残差在 320×180 下是否可见；若可见，加 0.06–0.08 s 淡入。
- 玩家胶囊在补面处停在 z≈1.27，触发在 z≈1.22；若手感上「还没走到门就进去了」，把 `triggerDistance` 降到 0.14–0.16（仍 > near clip 0.1）。
- 走廊局部点光的亮度/颜色是否和大厅协调；属美术调整。

## 参考

- 项目约定：`AGENTS.md`
- 第一人称俯仰：`docs/superpowers/specs/2026-11-19-first-person-pitch-design.md`
- 低分辨率显示：`Assets/Scripts/Level/Visual/LowResDisplay.cs`
- 安全门交互：`Assets/Scripts/Level/BackRooms/SafeDoorInteract.cs`
- 门的运动：`Assets/Scripts/Level/Door/AutoDoor.cs`
- 玩家控制：`Assets/Scripts/Player/FirstPersonController.cs`
- 场景：`Assets/Scenes/Level1.unity`、`Assets/Scenes/Level2.unity`
