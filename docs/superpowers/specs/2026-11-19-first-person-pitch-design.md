# 第一人称俯仰（2026-11-19）

## 背景

`2026-11-14-doom-style-level-editor-design.md` 最初把 pitch 固定为 0，只允许左右转。现在需要玩家能抬头 / 低头观察场景，因此解锁上下看。本文记录这次改动的实现位置与理由。

## 设计决定

- **yaw 在玩家根节点，pitch 在「眼睛」节点。** `= 玩家` 根节点继续由 `FirstPersonController` 写入 `Quaternion.Euler(0, yaw, 0)`；pitch 写入子物体 `= 玩家/眼睛` 的 `localRotation.x`。
  - 不能把 pitch 直接写到 `玩家相机`（CinemachineVirtualCamera）上：`ElevatorRideMotion` 已经用它的 `localPosition` / `localRotation` 做开场电梯晃动和到站回弹，两边会互相覆盖。
  - 「眼睛」是 `玩家相机` 的父级，绕它旋转等于绕相机位置旋转，画面不会平移。
- **`FirstPersonController.lookPivot` 是序列化引用，场景与预制体中都指向「眼睛」。** 引用为空时 `Debug.LogError` 并禁用控制器，不做隐式查找或默认值。
- **鼠标与手柄共用一套符号约定。** 鼠标取 `Look` 的位移量乘 `lookSensitivity`；手柄取右摇杆推量乘 `游戏手柄视角速度`（180°/s）再乘 `Time.deltaTime`。两者 y 轴向上为正，而 Unity 的 Euler X 正角是低头，所以代码里是 `_pitch -= y`。
- **俯仰上限 ±85°**，避免翻转到相机背面或产生万向锁观感；上限由 Inspector 的「俯仰角度上限」配置。
- **交互射线随眼睛朝向变化。** `InteractableBase` 等从眼睛沿 forward 发射线，抬头 / 低头会改变可交互目标，这是预期行为。

## 验收

- Play 后鼠标上下移动、右摇杆上下推，视角可俯仰，到 ±85° 停止。
- 左右转身与 WASD 移动不受影响。
- 开场电梯晃动、到站下沉回弹仍正常，退出 Play 后眼睛的局部旋转不会残留到下一次 Play（`_pitch` 为实例字段，场景重载会重置）。
