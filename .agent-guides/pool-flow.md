---
description: 改动泳池房八步流程（抽水泵提示、池底数字、暗门、长池阀、退水、20 柜（原 37 柜）、主阀、地漏下坠）时使用。
---

# 泳池房八步流程

改动本域代码或接线前通读本文件；项目级约束见根 `AGENTS.md`，规格见 `docs/superpowers/specs/2026-11-21-pool-room-flow-design.html`，实施记录见 `docs/superpowers/plans/2026-11-21-pool-room-flow.md`。

## 代码边界

脚本在 `Assets/Scripts/Level/Pool/`，全部 `Game.Level` 命名空间：

- `PoolFlowState`：11 个只读 bool 与 `Mark*/Take*` 方法；只由交互点置位，不广播、不存档。
- `PoolPickupInteract`：轮 / 钥匙 / 扳手共用；按 `Kind` 组合状态门控，拾取后 `SetActive(false)`。
- `PoolValveWheelInteract`：长池阀；第一次左键装轮，之后**按住左键**累计 1080° 后长池退水。
- `PoolMainValveInteract`：主阀；无扳手只播提示（不置 `WrenchNeedLearned`），有扳手转 1.2 s 后池核退水。
- `PoolWaterDrain`：一次退水；水面本地 y 在 2 s 内降到 -1.55 后禁用，空气墙同帧禁用；长池/池核各调一次。
- `PoolNumberSightTrigger`：池底数字的一次性视线判定：观察区 + 距离 + 视口 + 水平夹角 + 可见几何遮挡（`RaycastNonAlloc` 跳过无渲染器的空气墙，地板/池壁等有渲染器的实体算遮挡）。不要改回 `ScreenVisibility`（池边空气墙会挡 `Physics.Linecast`），也不能只做视口判定——隔着池沿和地板会误触发。
- `PoolStoryTrigger`：泵平台提示、暗门内侧“要扳手”两个区域触发。
- `PoolDrainExitTrigger`：竖井底一次性触发，只置 `NextLevelReached` 并调 `On Next Level`，不加载场景、不淡出。

## 场景接线

- 泳池房是 `Assets/Prefabs/Level/泳池房.prefab` 的 connected 实例；几何、道具、碰撞改预制体，交互组件与 Yarn 引用在 `Level1` 的 `TestRoom` / `Level2` 的 `泳池房` 实例上 Add Component。
- 两个场景各自有一份 `流程`：`流程状态`、`退水控制`、`剧情`（12 个 `DialogueTrigger`）；`DialogueTrigger` 的 `runner`/`presenter` 必须指向本场景根 `= 对话/对话系统`。
- `眼` / `viewCamera` 留空时由 `InteractableBase` / `PoolNumberSightTrigger` 取 `Camera.main`；不要绑会被 `TestSandboxGuard` 销毁的测试容器。
- 长池：水面 `道具/水面_01..03`，空气墙 `建筑/空气墙_长池北叶/长池颈/长池颈_北/长池南叶`。
- 池核：水面 `道具/水面_04..07`，空气墙 `建筑/空气墙_池核/池核_北/池核_北中/池核_南/池核_东`。水渠 `水面_08/09` 不参与。

## 容易踩的坑

- `CabinetDoorInteract` 的 `FeedbackLocked` 锁着时仍要求 `CanInteract()` 为真，才能给短句；`SilentLocked` 不可被瞄准。20 柜（原 37 柜）用前者，00 暗门用后者。
- 锁门/拾取物隐藏后再解锁的目标必须用**常驻空锚点**（`剧情锚点/交互点_*`）挂 `DialogueTrigger`；拾取物会 `SetActive(false)`，不要把 3D 飘字挂在它身上。
- 号牌与文字必须挂在各自 `Door_Front` 下：00 为 `门_暗门/Hinge_FrontDoor/Door_Front/号牌_00`、`文字_00`；Level2 场景当前 26 个柜子按 01..26 连续编号（原 36 柜=新 20 柜、原 37 柜=新 21 柜），每扇 `储物柜_XX/Hinge_FrontDoor/Door_Front` 下也有 `号牌_XX`、`文字_XX`（同一套本地变换，字号 0.8，编号取柜名）；文字本地 Y 旋转 270° 才正面朝房间，90° 会左右镜像。预制体仍保留 39 个旧编号柜，Level1 不受本次重排影响。它们曾挂在 `道具` 下，开门时不会跟门板旋转，会留在原地浮空。
- `PoolPickupInteract` 的轮必须等 `WrenchNeedLearned` 才能捡；主阀无扳手时**不能**补置 `WrenchNeedLearned`，否则玩家可绕过暗门。
- 钥匙只 `Unlock()` 20 门（原 37 柜已改为原 36 柜，即新 20 柜），不自动开；00 暗门由 `PoolNumberSightTrigger` 解锁并 `Open()` 双门。
- 扳手柜从原 37 改到原 36（新 20）：`CabinetDoorInteract` 已复制到新 20 门，旧 21 门移除；钥匙的 `unlockDoors` 与 `剧情_20锁着` / `剧情_扳手` 的 `textAnchor` 都指向新 20 的 `交互点_20柜`。移动柜子后记得同步这个锚点。
- Level2 的 20 号柜使用专用 Yarn 节点 `LV2_Pool_Locked20` / `LV2_Pool_Key20`；Level1 测试房仍用 `LV2_Pool_Locked37` / `LV2_Pool_Key`（编号 37）。不要把 Level2 改回共用节点。
- 给对象补组件不要写 `GetComponent<T>() ?? AddComponent<T>()`：Unity 的伪 null 会让 `??` 判空失效；显式 `if (c == null)`。
- `InteractableBase.IsAimed()` 要求命中的 Collider 在交互组件所在物体或其子物体上；Collider 不能挂父级/兄弟。
- 拾取物/阀的 Collider 由脚本所在物体或子物体承担；改预制体后 Level1/Level2 实例的 override（`Lock Mode`、`Hinge`、`Open Angle`、`lockedDialogue`）要确认没有被 Revert。
