---
description: 改动或新增门（AutoDoor 三种模式、铰链运动、锁定/定时门）时使用。
---

# 门（AutoDoor）

改动本域代码前通读本文件；项目级约束见根 `AGENTS.md`，同目录其他条目按需加载。

## AutoDoor 三种开门方式与铰链运动

`AutoDoor` 有三种开门方式：`Rise`（整扇向上，旧的卷帘门）、`SlideApart`（左右门扇 `Leaf_Left`/`Leaf_Right` 各向两侧滑 `slideDistance`，电梯门用这个）与 `Hinge`（门扇挂在一个带 `Rigidbody` + `HingeJoint` 的枢轴节点下，`hingeOpenAngle` 负值反向；关门角速度与刚度可单独配（`hingeCloseSpeed` / `hingeCloseGain`，0 表示与开门相同），储藏室门（前室↔储藏室）为防玩家探头后退出去，关门配 720°/s、刚度 24（约 0.22 s，开门仍是 150°/s）；

枢轴刚体在 `AutoDoor` 初始化时被设成 `isKinematic`，由 `HingeDoorMotion` 按剩余角度限速 `MoveRotation` 绕竖直轴转到开门角，关卡二两扇北门与安全门都用这个——**不要改回 `HingeJoint` 电机**：电机推不动玩家的 `CharacterController`，而门扇转动期间又不该挡玩家（放行做法是把 `Player` 层写进门扇碰撞体的 `Collider.excludeLayers`，**不能直接关碰撞体**——关掉后刚体失去全部形状、PhysX 不再模拟它，门会卡在关着的位置），这两条叠加会让电机彻底失效；

运动学刚体则与碰撞、玩家站位都无关。转动到位且不再压着玩家后恢复碰撞：门板要整块贴在墙体朝房间的那一面之外（只留 2 mm 防止与墙面共面闪），门扇才会始终在墙面以外扫掠、铰链轴可以直接压在门洞边角上把门洞盖严；若门板插在门洞厚度里，铰链必须离侧壁留出门厚一半的余量，关门时就会留下一条竖缝）；

滑动门的两扇要略宽于门洞并在中缝重叠，滑到底还要多滑几厘米藏进门洞侧壁后面，否则会露出薄片或与侧壁共面闪。

## 储物柜门（CabinetDoorInteract）

`Assets/Scripts/Interaction/CabinetDoorInteract.cs`（`Game.Level`，`InteractableBase` 子类）给没有刚体的柜门做旋转开启，**不要**为它加 `Rigidbody`/`HingeJoint` 再套 `AutoDoor`：柜子根节点带非均匀缩放，刚体在这种缩放下发数会变形。

- 挂在**门板本身**（`Door_Front` / `Door_Back`，碰撞体就在这层，`IsAimed` 的 `IsChildOf(transform)` 才能命中）；`hinge` 指对应的 `Hinge_*` 节点（必填，为空会 LogError 并禁用），`openAngle` 决定外开方向，`duration` 默认 0.45 s。
- 关门态由 `Awake` 里的 `hinge.localRotation` 缓存，开门角 = `_closedRotation * Quaternion.Euler(0, openAngle, 0)`，用 `Quaternion.SlerpUnclamped` 在 `UniTask.Yield(PlayerLoopTiming.Update)` 循环里插值，不做物理。
- 转动期间给门板碰撞体的 `excludeLayers` 加上 `Player` 层（与 `HingeDoorMotion` 同规则：放行而不是关碰撞体），结束时恢复；不要在协程里做（项目禁用协程）。
- `eye` 绑 **Main Camera** 的 Transform：`玩家_测试` 那套是测试容器，会被 `TestSandboxGuard` 销毁，绑它换场景/换玩家实例就失效。
- 关卡二长廊口的「假柜」是两段式：走廊侧先开背门（`Hinge_BackDoor`，+90°），进柜体空腔后再开前门（`Hinge_FrontDoor`，−90°）进更衣室。几何与拉伸做法见 `props.md`。

