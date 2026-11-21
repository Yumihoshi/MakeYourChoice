---
description: 改动第一人称移动/视角、输入动作与键位或低分辨率显示时使用。
---

# 输入与第一人称

改动本域代码前通读本文件；项目级约束见根 `AGENTS.md`，同目录其他条目按需加载。

## 输入（键位 / 手柄）

`Assets/Scripts/Player/PlayerInputRouter.cs`：代码里建一张 `InputActionMap("Player")`，动作 Move / Look / Interact / Sprint / Menu / Back，键位按飞书 3C 表（WASD + 左摇杆、鼠标 + 右摇杆、鼠标左键 + 手柄 A、Shift + 扳机、Esc + Start、手柄 B 单独作 Back 只给菜单用）。

`CurrentDevice` 会在键鼠/手柄之间自动切换（`DeviceChanged` 事件），新增输入一律从 `PlayerInputRouter.Instance` 取动作，不要直接读 `Keyboard.current`。

注意本版本 Input System 的 `AddAction` 没有 `expectedControlType` 参数

## 第一人称视角与低分辨率显示

ProBuilder（5.2.4）搭 3D 几何 + Cinemachine 第一人称；

运行时组件在 `Assets/Scripts/Level` 与 `Assets/Scripts/Player`（`LowResDisplay` 低分辨率显示、`FirstPersonController`、`AutoDoor`、`TriggerVolume`；`FirstPersonController` 的 yaw 写在玩家根节点，pitch 写在序列化的 `lookPivot`（场景与预制体中指向子物体「眼睛」），上限 ±85°——**不要把 pitch 写进 `玩家相机` 这个 VCam 的局部旋转**，`ElevatorRideMotion` 正用它做电梯晃动与到站回弹，两边会互相覆盖）。

## 玩家预制体与控制权锁

玩家预制体 `Assets/Prefabs/= 玩家.prefab`（根名 `= 玩家`）自带 `CharacterController` + `FirstPersonController` + `PlayerControlLock`，子级 `眼睛`（`lookPivot`）、`输入`（`PlayerInputRouter`）、`玩家相机`（CinemachineVirtualCamera + `ElevatorRideMotion`）。

**`FirstPersonController` 在预制体里必须是 `enabled = true`**：锁的职责是"开场剧情期间临时关掉它"，不是"平时它就该是关的"。曾经因为预制体里把它存成关闭态、而 Level2 的实例又把 `PlayerControlLock` 整个移除（`m_RemovedComponents` 里有 `fileID: 145274049532506897`），两道保险同时失效，进 Play 后玩家原地不能动、Console 也只剩一条 MCP 提示——排查这类"能动不了"先看这两处。

Level1 的实例保留 `PlayerControlLock` 并覆盖 `gate`（指向开场独白）；Level2 没有开场剧情，实例里把锁移除。`PlayerControlLock` 现在对 `gate == null` 的情况单独处理：打印警告、把控制器**启用**、`IsLocked = false` 并关掉自己，这样"场景没配开场剧情"不会变成"玩家被永久冻住"。
