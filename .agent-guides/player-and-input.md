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
