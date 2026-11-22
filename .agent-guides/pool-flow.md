---
description: 改动泳池房八步流程（抽水泵提示、池底数字、暗门、长池阀、退水、20 柜（原 36 柜，钥匙开）、主阀、地漏下坠、进入泳池房后柜门开放与 20 柜钥匙门控）时使用。
---

# 泳池房八步流程

改动本域代码或接线前通读本文件；项目级约束见根 `AGENTS.md`，规格见 `docs/superpowers/specs/2026-11-21-pool-room-flow-design.html`，实施记录见 `docs/superpowers/plans/2026-11-21-pool-room-flow.md`。

## 代码边界

脚本在 `Assets/Scripts/Level/Pool/`，全部 `Game.Level` 命名空间：

- `PoolFlowState`：12 个只读 bool 与 `Mark*/Take*` 方法；只由交互点/进入触发器置位，不广播、不存档。
- `PoolPickupInteract`：轮 / 钥匙 / 扳手共用；按 `Kind` 组合状态门控，拾取后 `SetActive(false)`。
- `PoolValveWheelInteract`：长池阀；第一次左键装轮，之后**按住左键**累计 1080° 后长池退水。
- `PoolMainValveInteract`：主阀；无扳手只播提示（不置 `WrenchNeedLearned`），有扳手转 1.2 s 后池核退水。
- `PoolWaterDrain`：一次退水；水面本地 y 在 2 s 内降到 -1.55 后禁用，浮物（长池/池核气垫）保持起始相对水位的高度随水面下沉，落到池底顶面 `floatRestY=-1.5` 后停住；空气墙同帧禁用；长池/池核各调一次。
- `PoolNumberSightTrigger`：池底数字的一次性视线判定：观察区 + 距离 + 视口 + 水平夹角 + 可见几何遮挡（`RaycastNonAlloc` 跳过无渲染器的空气墙，地板/池壁等有渲染器的实体算遮挡）。不要改回 `ScreenVisibility`（池边空气墙会挡 `Physics.Linecast`），也不能只做视口判定——隔着池沿和地板会误触发。
- `PoolStoryTrigger`：泵平台提示、暗门内侧“要扳手”两个区域触发。
- `PoolRoomEntryTrigger`：玩家进入长池/池核所在泳池房的一次性区域触发；`OnTriggerEnter/Stay` 检查 `Player` 标签与 `GameCursorState.IsPlaying`，置 `EnteredPoolRoom`。
- `PoolDrainExitTrigger`：竖井底一次性触发，只置 `NextLevelReached` 并调 `On Next Level`，不加载场景、不淡出。

## 场景接线

- 泳池房是 `Assets/Prefabs/Level/泳池房.prefab` 的 connected 实例；几何、道具、碰撞改预制体，交互组件与 Yarn 引用在 `Level1` 的 `TestRoom` / `Level2` 的 `泳池房` 实例上 Add Component。
- 两个场景各自有一份 `流程`：`流程状态`、`退水控制`、`剧情`（12 个 `DialogueTrigger`）；`DialogueTrigger` 的 `runner`/`presenter` 必须指向本场景根 `= 对话/对话系统`。
- Level2 在 `流程` 下另有 `触发_进入泳池房`（世界位置 `(-10, 1.7, 32)`，BoxCollider trigger，size `(26, 3.4, 24)`），挂 `PoolRoomEntryTrigger` 并指向 `流程状态`；更衣室 01..25 号 `Door_Front` 与 00 暗门的 `CabinetDoorInteract.poolFlowState` 都指向同一个 `流程状态`；**20 号柜前门不绑定**（保留原有 `FeedbackLocked` + 钥匙 `Unlock()` 逻辑），**26 号入口假柜的前后双门也不绑定**（见下条坑）。
- `眼` / `viewCamera` 留空时由 `InteractableBase` / `PoolNumberSightTrigger` 取 `Camera.main`；不要绑会被 `TestSandboxGuard` 销毁的测试容器。
- 长池：水面 `道具/水面_01..03`，空气墙 `建筑/空气墙_长池北叶/长池颈/长池颈_北/长池南叶`。
- 池核：水面 `道具/水面_04..07`，空气墙 `建筑/空气墙_池核/池核_北/池核_北中/池核_南/池核_东`。水渠 `水面_08/09` 不参与。

## 容易踩的坑

- `CabinetDoorInteract` 的 `FeedbackLocked` 锁着时仍要求 `CanInteract()` 为真，才能给短句；`SilentLocked` 不可被瞄准。未绑定 `poolFlowState` 时 20 柜（原 36 柜）用前者、00 暗门用后者；Level2 的 01..25 与 00 暗门绑定后，进入泳池房前仍保持这两种表现，进入后 `EnteredPoolRoom` 为真，这些柜门忽略锁可开。**20 号柜刻意不绑定**，保持 `FeedbackLocked` + 钥匙 `Unlock()`：进入泳池房也不解锁。不要把 00 暗门或普通柜改回永久 `SilentLocked`。
- `PoolRoomEntryTrigger` 必须覆盖长池与池核两片区域，且只置位、不重置；`EnteredPoolRoom` 是一次性进度。Level2 的 01..25 号 `Door_Front` 与 00 暗门都要挂 `CabinetDoorInteract` 并绑同一个 `流程状态`，漏掉的门会退回旧锁逻辑（20 号柜就是刻意保留旧锁逻辑的例外）。**20 号柜前门不绑定**：它必须是 `FeedbackLocked`，没拿钥匙点只播“锁着”，钥匙 `Unlock()` 后才可开。**26 号是长廊→更衣室的唯一入口假柜，它的 `Door_Front` / `Door_Back` 都不能绑 `poolFlowState`**：一旦绑定，进入泳池房前两扇门都无准心，玩家根本进不了更衣室（2026-11-22 修复的 bug）。
- 气垫是 `道具` 下的独立预制体实例，不是水面的子物体，退水时不会自动跟随。改/换气垫后必须把它填进 `退水控制` 对应池的 `longPoolFloats` / `coreFloats`；漏填时退水后气垫悬在原位（2026-11-22 修复的 bug）。
- 锁门/拾取物隐藏后再解锁的目标必须用**常驻空锚点**（`剧情锚点/交互点_*`）挂 `DialogueTrigger`；拾取物会 `SetActive(false)`，不要把 3D 飘字挂在它身上。
- 号牌与文字必须挂在各自 `Door_Front` 下：00 为 `门_暗门/Hinge_FrontDoor/Door_Front/号牌_00`、`文字_00`；Level2 场景当前 26 个柜子按 01..26 连续编号（原 36 柜=新 20 柜、原 37 柜=新 21 柜），每扇 `储物柜_XX/Hinge_FrontDoor/Door_Front` 下也有 `号牌_XX`、`文字_XX`（同一套本地变换，字号 0.8，编号取柜名）；文字本地 Y 旋转 270° 才正面朝房间，90° 会左右镜像。预制体仍保留 39 个旧编号柜，Level1 不受本次重排影响。它们曾挂在 `道具` 下，开门时不会跟门板旋转，会留在原地浮空。
- `PoolPickupInteract` 的轮必须等 `WrenchNeedLearned` 才能捡；主阀无扳手时**不能**补置 `WrenchNeedLearned`，否则玩家可绕过暗门。
- 钥匙只 `Unlock()` 20 门（原 37 柜已改为原 36 柜，即新 20 柜），不自动开；即使玩家已进入泳池房，没钥匙也开不了 20。00 暗门由 `PoolNumberSightTrigger` 解锁并 `Open()` 双门。
- 扳手柜从原 37 改到原 36（新 20）：`CabinetDoorInteract` 已复制到新 20 门，旧 21 门移除；钥匙的 `unlockDoors` 与 `剧情_20锁着` / `剧情_扳手` 的 `textAnchor` 都指向新 20 的 `交互点_20柜`。移动柜子后记得同步这个锚点。
- Level2 与 Level1 测试房现在共用预制体里的同一套 `剧情`，20 号柜的专用 Yarn 节点是 `LV2_Pool_Locked20` / `LV2_Pool_Key20`。旧版 Level1 里另抄的一份 `剧情_37锁着` / `LV2_Pool_Key`（编号 37）已删除，不要再按 37 号柜接回。
- 改柜子编号/位置后要连同实例上的 Transform override 一起查：Level1 的 `道具/扳手` 曾被实例 override 在旧 37 柜位置 `(-11.05, 0.85, 14.45)`（旧 `交互点_37柜` 也在这），表现为扳手悬空在 20 柜外；2026-11-22 已 Revert 回预制体值 `(-8.5852, 0.0647, 14.5178)`，落在 20 柜底部。查法：`PrefabUtility.GetPropertyModifications(TestRoom)` 里看目标 Transform 的 `m_LocalPosition.*`，或逐个对比实例与预制体源的 local 位置/旋转/缩放。
- Level1 的 `TestRoom` 曾经在预制体实例上又手抄了一整套 `流程`（旧的 37 柜版）、8 个重复 `交互点_*`，以及 7 个重复组件（`阀_长池` / `轮` / `主阀` / `扳手` / `钥匙` / `触发_下坠` 上的组件副本，引用全空）——2026-11-22 已全部删掉，仅保留预制体自带的那套。若再看到 `GetAddedComponents(TestRoom)` 非空，说明又有人在实例上手抄接线，先确认是不是重复件再决定去留。
- 给对象补组件不要写 `GetComponent<T>() ?? AddComponent<T>()`：Unity 的伪 null 会让 `??` 判空失效；显式 `if (c == null)`。
- `InteractableBase.IsAimed()` 要求命中的 Collider 在交互组件所在物体或其子物体上；Collider 不能挂父级/兄弟。
- 拾取物/阀的 Collider 由脚本所在物体或子物体承担；改预制体后 Level1/Level2 实例的 override（`Lock Mode`、`Hinge`、`Open Angle`、`lockedDialogue`）要确认没有被 Revert。
- 长池阀 `道具/管道_轮阀接口/锚点_轮阀安装位/阀_长池` 必须是锚点的**子物体**（local 位置/旋转归零、scale 1），`轮_已安装` 是 `阀_长池` 的子物体（local 归零、scale 0.486，旋转轴动画才会带着轮一起转）。管道/法兰在 6080fd7 里被整体下移到 y=1.553，而 `阀_长池` 当时是 `道具` 下的固定坐标 2.18，结果装轮后轮悬在法兰上方 0.627 m（2026-11-22 修回预制体：阀挂到锚点下、轮 scale 0.486、`交互点_阀_长池` 同步到 1.553）。以后调管道高度只动 `管道_轮阀接口`，阀和锚点会跟着走；不要再把阀挂到 `道具` 下写死坐标。
