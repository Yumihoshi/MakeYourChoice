---
description: 改动关卡一大厅流程、触发区、红字/视线判定或安全门交互时使用。
---

# 关卡一：大厅流程

改动本域代码前通读本文件；项目级约束见根 `AGENTS.md`，同目录其他条目按需加载。

## 流程脚本、安全门与触发区

`Assets/Scripts/Level/Hall`（`HallFlowState` 关卡开关状态、`ElevatorDoorInteract` 瞄准电梯门按交互键开门、`RedTextSightTrigger` 红字要在可视距离内、视线对准且真的出现在画面里、`DoorListenTrigger` 门前听 3 s、`ElevatorCountdown` 电梯内倒计时结局、`HallEntryTrigger` 第一次进入大厅），触发区在 `关卡/电梯/触发` 与 `关卡/大厅/触发`。

大厅被一整块 `关卡/大厅/建筑/中间墙`（12.8×3×0.3，z=1.5，实心无洞）隔开，两边各留 2.6 m 缝隙；

`安全门`（原 `Door_Mid_Safe`，场景路径 `关卡/大厅/门/安全门`）是贴在中间墙南面之外的门板（`AutoDoor` Hinge，门板 1.54×2.44×0.30 在 z=1.198，北面距墙 2 mm，挂在门洞西侧 `铰链` 下绕竖直轴转动），所以**站在墙北侧既看不到门也交互不到**——射线先打到 `Wall_Mid`。

门默认锁着（`SafeDoorInteract.Awake` 里 `AutoDoor.Locked = true`），而且**要开过电梯门（`HallFlowState.DoorOpened`，触发 2）才可交互**——否则游戏一开始就能按出「打不开」；

拿到真门禁卡才向大厅内转开（`hingeOpenAngle` 95°，门扇扫向门洞西侧、把门洞让出来），同时把 `exitOpened`（UnityEvent）抛给下一节点当切场景接口。

缝隙里响应「第一次位于 B」的两个触发区 `缝隙西侧`/`缝隙东侧`（原 `Trigger_B_West`/`Trigger_B_East`，在 `关卡/大厅/触发`）（缝隙中央 x=±7.7，触发盒 2.4×2.4×1.5，比缝隙略窄所以贴边也绕不过去）共用 `HallFlowState.EnteredRoom`，从任一侧进来只播一次（所以没复用 `DialogueZoneTrigger`，它每个实例各播一次）。

## 红字后上锁与玩家控制锁

**看过红字后电梯门上锁**：`HallFlowState.CanOpenDoor` 在风声剧情（`LV1_Listen`）播完前为 false，门既不显示交互提示也开不了，`DoorListenTrigger` 在剧情播完后才 `MarkListenFinished()` 解锁。

玩家侧还有 `PlayerControlLock`（挂在 Player 上，只锁**开场剧情**：播完前禁用 `FirstPersonController`，即开场不能走动/回头；`IsLocked` 是静态状态，开场锁定期间准心与交互也一并关闭，其他剧情播放中玩家照常移动、照常交互）
