# 文件柜 A 抽屉与门禁卡实现计划

> **规格：** `docs/superpowers/specs/2026-11-19-filing-cabinet-drawers-design.md`

**目标：** 用 `FilingCabinet_Separated.fbx` 替换 `关卡/打印室/道具/柜子A`，保留原剧情触发，四格抽屉可独立开合，第三格内的门禁卡在满足流程后可交互拿走。

**技术栈：** Unity 2022.3.62f3c1、URP 14.0.12、DOTween Pro、Yarn Spinner 3.2.8、Odin Inspector、Unity MCP。

**项目约束：** 按 `AGENTS.md` 只做查询类 git 操作，本计划不包含 `git add/commit`；场景/材质/网格用 Unity 编辑器或 Unity MCP 修改，不手写 YAML；`Assets/Plugins` 与 `Assets/QFramework` 只读；序列化字段加 Odin `[LabelText("中文")]`；视觉与动态效果由用户手测。

---

## 文件结构

**创建：**

- `Assets/Shaders/CabinetOutline.shader` — URP inverted-hull 绿色描边。
- `Assets/Scripts/Level/BackRooms/FilingCabinetDrawer.cs` — 单格抽屉的开合交互。
- `Assets/Scripts/Level/BackRooms/FilingCabinetCard.cs` — 第三格门禁卡的交互与跟随。
- `Assets/Materials/Level/FilingCabinet_Body.mat`、`FilingCabinet_Drawer.mat` — URP/Unlit 柜体与抽屉。
- `Assets/Materials/Level/AccessCard.mat` — URP/Unlit + Alpha Clip 门禁卡。
- `Assets/Materials/Level/Cabinet_Outline.mat` — 描边材质。
- `Assets/Art/Meshes/FilingCabinet/Body_Outline.asset`、`Drawer1_Outline.asset`…`Drawer4_Outline.asset` — 平滑法线的描边网格副本。

**修改：**

- `Assets/Scripts/Level/BackRooms/GlowCabinetInteract.cs` — 从 `InteractableBase` 改为纯协调器。
- `Assets/Scenes/Level1.unity` — 替换 `柜子A` 的视觉与碰撞，实例化 FBX、加抽屉/卡片/描边。
- `Assets/Art/Models/filing-cabinet/textures/*.png.meta`、`Assets/Art/素材/门禁卡.png.meta` — 导入改 Point/Uncompressed。
- `AGENTS.md` — 更新「已有能力」表的关卡二交互条目。

---

## 任务 1：抽屉与卡片的交互脚本

- [x] **步骤 1：把 `GlowCabinetInteract` 改成协调器**

  `Assets/Scripts/Level/BackRooms/GlowCabinetInteract.cs` 不再继承 `InteractableBase`，保留 `state`、`firstDialogue`、`secondDialogue`，提供：

  - `bool IsDialogueRunning`：两段剧情是否在播；
  - `bool CanUseDrawers`：`state.EnteredPrintRoom && !IsDialogueRunning`；
  - `void OnDrawerOpened()`：第一次拉开时 `MarkCheckedCabinetA()` + `firstDialogue.StartDialogue()`；
  - `bool TryTakeRealCard()`：未满足条件只重播第一段并返回 false；满足 `CheckedCabinetA && TriedSafeDoorWithTempCard` 后 `TakeRealCard()` + `secondDialogue.StartDialogue()` 并返回 true。

- [x] **步骤 2：新增 `FilingCabinetDrawer`**

  `Awake` 缓存关闭时的 `localPosition`；`CanInteract` 要求进过打印室、柜子剧情没在播，锁柜期间只允许关闭已开抽屉；`OnInteract` 切换 `_open`，打开时通知 `cabinet.OnDrawerOpened()`，用 DOTween `DOLocalMove(closed + Vector3.forward * openDistance, duration)` 开合。

- [x] **步骤 3：新增 `FilingCabinetCard`**

  卡片不作为抽屉子物体；`LateUpdate` 用 `drawerMount.TransformPoint(localPositionInDrawer)` 与 `drawerMount.rotation * Quaternion.Euler(localEulerAnglesInDrawer)` 跟随；`CanInteract` 要求抽屉已开、未锁、未拿真卡、剧情没在播；`OnInteract` 调 `TryTakeRealCard()`，成功则 `SetActive(false)`。

- [x] **步骤 4：编译检查**

  Unity 刷新后 Console 无 error；`execute_code` 确认 `FilingCabinetDrawer`/`FilingCabinetCard` 的基类是 `InteractableBase`，`GlowCabinetInteract` 的基类是 `MonoBehaviour`。

---

## 任务 2：材质、贴图与描边网格

- [x] **步骤 1：贴图导入**

  `FilingCabinet1.png`、`FilingCabinet1Drawer.png`、`门禁卡.png` 改 Default、Point、Uncompressed（带 mipmap；卡片开 `alphaIsTransparency`）。

- [x] **步骤 2：材质资产**

  `FilingCabinet_Body.mat` / `FilingCabinet_Drawer.mat` 用 URP/Unlit，分别绑两张柜体贴图；`AccessCard.mat` 用 URP/Unlit，`_AlphaClip=1`、`_Cutoff=0.5`、`renderQueue=2450`、`_Cull=0`；`Cabinet_Outline.mat` 用 `Game/CabinetOutline`，绿色、宽度 0.012。

- [x] **步骤 3：描边网格**

  原 FBX 网格是硬边分裂法线，直接 inverted-hull 会在棱角裂缝。用 `execute_code` 按位置合并法线生成 `Body_Outline.asset`、`Drawer1_Outline.asset`…`Drawer4_Outline.asset`，赋给各 `Outline` 子物体的 `MeshFilter`；复核 `splitPos=0`。

---

## 任务 3：场景替换

- [x] **步骤 1：根节点**

  `Assets/Scenes/Level1.unity` 的 `柜子A` 根 Transform 设为 `(2.2,0,9.19)` / `(0,180,0)` / 1；删除旧 `MeshFilter`、`MeshRenderer`、`YBillboard`；保留两个 `DialogueTrigger` 与 `GlowCabinetInteract`，根 `BoxCollider` 改 `center=(0,0.9,-0.05)`、`size=(1,1.8,0.9)`。

- [x] **步骤 2：实例化 FBX**

  `FilingCabinet_Separated.fbx` 作为根的子物体 `Model`；`Body` 用 Body 材质，`Drawer1..4` 用 Drawer 材质；每个部件下加 `Outline` 子物体（平滑网格 + 描边材质，关阴影）。

- [x] **步骤 3：抽屉交互**

  每格加 `BoxCollider(center=(0,0,0.42), size=(0.9,0.35,0.06))` 与 `FilingCabinetDrawer`，设 `cabinet`、`state`、`eye`、`maxDistance=3`、`openDistance=0.4`、`duration=0.3`。

- [x] **步骤 4：门禁卡**

  根下新建 Quad `门禁卡`，`localScale=(0.14,0.14,1)`，删 `MeshCollider`，用 `AccessCard`，加 `BoxCollider(size=(1,1,0.12))` 与 `FilingCabinetCard`；`drawer`/`drawerMount` 指向 `Drawer3`，`localPositionInDrawer=(0,-0.155,0.3)`，`localEulerAnglesInDrawer=(90,180,0)`。

- [x] **步骤 5：保存与验证**

  保存场景；检查根组件、`Model` 子物体、抽屉 collider、卡片字段、Console error=0。卡片关闭时世界位置约 `(2.20,0.52,8.83)`，Drawer3 世界位置约 `(2.20,0.68,9.12)`。

---

## 任务 4：文档

- [x] **步骤 1：设计规格**

  新增 `docs/superpowers/specs/2026-11-19-filing-cabinet-drawers-design.md`，记录协调器/collider 分层/卡片跟随/剧情映射/Unlit+描边等决定。

- [x] **步骤 2：实现计划**

  新增本文件，记录文件结构与已完成步骤。

- [x] **步骤 3：更新 `AGENTS.md`**

  在「已有能力」表的关卡二交互条目中补充文件柜 A 的协调器、抽屉、卡片、描边与材质说明。

---

## 任务 5：用户手测（待做）

- [ ] **步骤 1：进入打印室**

  Play 关卡二 → 进入打印室 → 对准任意一格抽屉按交互键：抽屉滑出约 0.4 m，第一次拉开播“我说了，只是反光。”；再按一次推回；四格可独立开合。

- [ ] **步骤 2：锁柜阶段**

  拿到临时门禁卡后，未试安全门前不能拉开新抽屉；用临时卡试过安全门后锁解除。

- [ ] **步骤 3：第三格门禁卡**

  拉开从上往下第三格，低头看到白底蓝条小卡片；条件不足时交互只重播第一段，卡片不消失；满足条件后交互播第二段、卡片消失、`GotRealCard` 为 true，随后安全门可用真卡打开。

- [ ] **步骤 4：视觉**

  柜体/抽屉亮度与复印机、办公桌接近；绿色描边在棱角处连续，没有明显裂缝。
