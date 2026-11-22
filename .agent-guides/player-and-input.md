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

## 楼梯与 CharacterController 的 stepOffset

泳池房的台阶（长池南叶 3 级、池核西侧 3 级）每级抬升 **0.375**（顶面 -0.375 / -0.75 / -1.125），
而玩家预制体的 `CharacterController.stepOffset` 曾经是 0.3，低于这个抬升，导致站在池底拾级而上时被
每一步的立面挡住，表现为「上下楼梯走不动」。解决办法是让 stepOffset 大于台阶抬升（现为 **0.45**，
留出 skinWidth 余量），**不要**为此加跳跃或自己写台阶抬升逻辑。改台阶高度时记得同步核对这个值；
它属于玩家预制体（`Assets/Prefabs/= 玩家.prefab`）的组件配置，不要手改 YAML，用 Unity 编辑器或 MCP。

## 窄道与 PlayerCapsuleZone（临时缩小胶囊半径与台阶高度）

泳池房有两处「侧身柜」：长廊尽头 `储物柜_54`（`0,0,14.53`，rotY 270，柜内空腔净宽
**0.412**、净高 1.75）与更衣室北墙 `门_暗门`（`-20.5,0,19.45`，净宽 **0.44**）。常驻胶囊半径
0.15（含 skinWidth 后 0.17）时中线余量只有 ±4.5 cm，玩家会被门框/柜体挡住，
表现为「长廊走不到更衣室」。实测两扇柜门都打开时：r=0.150 只能过 x ±4.5 cm，
r=0.125 可过 ±7 cm。

**卡死的真正原因不是侧向宽度，而是 `stepOffset`（2026-11-22 排查结论）**：CharacterController
撞到障碍时会**先向上抬 stepOffset 再试探前进**。长廊口门洞只有 1.8 m 高（`墙_更衣室南_门洞补_上`
占 y 1.8..3.0），抬 0.45 后胶囊顶部到 2.0 m，直接顶到门楣，抬升判定失败 → 整次移动被当成撞墙，
`collisionFlags = Sides`，玩家停在 z≈13.86（胶囊前缘 13.98 = 门洞补前立面 14.00 − skin 0.02）。
探针实测（胶囊 1.55 高、中线 x=0，从 z=13.5 前进 4 m）：`step=0.45` 卡在 13.86；
`step=0.30 / 0.20 / 0.15 / 0.10 / 0.08` 全部通过（半径 0.10 / 0.12 / 0.15 都一样）。
几何查询（`OverlapCapsule` / `CapsuleCast`）在这个位置**查不到任何阻挡**，别被它误导。

**解决办法不是改玩家常驻体积**（调小 height/radius 会让整局都变瘦，用户实测会引发
Level1 交互异常），而是在关卡的窄道处套一个「胶囊区域」：

- 组件 `Assets/Scripts/Level/Trigger/PlayerCapsuleZone.cs`（`Game.Level`，需要同物体上的
  `BoxCollider` 且勾 `Is Trigger`）。它逐帧用 `box.bounds.Contains(玩家位置)` 判断，玩家在框内时
  把 `CharacterController.radius` 换成 `区域内胶囊半径`（默认 0.12）、`stepOffset` 换成
  `区域内台阶高度`（默认 0.2），出框恢复原值；不依赖 `OnTrigger*`，所以传送瞬移也不会漏掉恢复。
  **两个值必须一起换**：只缩半径、不动 stepOffset 照样卡死；只在窄道缩 stepOffset 则池边 0.375 的
  台阶会爬不上去（那是区域外的常规值 0.45 负责）。
- 泳池房里的实例：`Assets/Prefabs/Level/泳池房.prefab/触发/区域_狭道体型`，
  box center `(-10, 1.95, 10.1)` size `(26, 4.9, 20.2)`（覆盖整条长廊 + 更衣室 + 北墙暗门外
  0.6 m，世界包围盒 x -23..3、y -0.5..4.4、z 0..20.2）。
- 新增窄道时照抄一份：框要**完整罩住空腔两端**，否则玩家在空腔正中跨出框会立刻恢复半径被卡住。
- 诊断这类「Geometry 查不到阻挡但就是走不过去」的问题：**不要在玩家原地生成 CharacterController
  探针**（探针会生成在玩家胶囊内部，测出「只有 r=0.15 能过」的假结论），先 `player.enabled = false`
  或把玩家挪开 30 m，且 `CapsuleCast` 的 mask 要排除玩家层；然后逐个禁用候选碰撞体二分。

## 玩家预制体与控制权锁

玩家预制体 `Assets/Prefabs/= 玩家.prefab`（根名 `= 玩家`）自带 `CharacterController` + `FirstPersonController` + `PlayerControlLock`，子级 `眼睛`（`lookPivot`）、`输入`（`PlayerInputRouter`）、`玩家相机`（CinemachineVirtualCamera + `ElevatorRideMotion`）。

**`FirstPersonController` 在预制体里必须是 `enabled = true`**：锁的职责是"开场剧情期间临时关掉它"，不是"平时它就该是关的"。曾经因为预制体里把它存成关闭态、而 Level2 的实例又把 `PlayerControlLock` 整个移除（`m_RemovedComponents` 里有 `fileID: 145274049532506897`），两道保险同时失效，进 Play 后玩家原地不能动、Console 也只剩一条 MCP 提示——排查这类"能动不了"先看这两处。

Level1 的实例保留 `PlayerControlLock` 并覆盖 `gate`（指向开场独白）；Level2 没有开场剧情，实例里把锁移除。`PlayerControlLock` 现在对 `gate == null` 的情况单独处理：打印警告、把控制器**启用**、`IsLocked = false` 并关掉自己，这样"场景没配开场剧情"不会变成"玩家被永久冻住"。
