---
description: 改动关卡二储藏室/打印室、柜子抽屉、复印机或对应台词触发时使用。
---

# 关卡二：储藏室与打印室

改动本域代码前通读本文件；项目级约束见根 `AGENTS.md`，同目录其他条目按需加载。

## 流程脚本、台词与场景节点

`Assets/Scripts/Level/BackRooms`（`BackRoomFlowState` 记录 8、10–16 号触发点、`InteractableBase` 瞄准+交互基类、`RoomEntryTrigger` 走进房间播剧情、`BlueArrowInteract` 追问蓝箭头、`CopierInteract` 拿临时门禁卡、`StorageDrawerQuest`（储藏室抽屉计数：任意柜子累计拉开 3 格不同抽屉后播一次 `LV2_CabinetB`，只播一次）、`EmptyCabinetDrawer`（储藏室每格抽屉开关；

每格第一次拉开向 `StorageDrawerQuest` 报数；拿临时卡后 `CabinetsLocked` 只能关）、`GlowCabinetInteract`（柜子A 协调器，普通 MonoBehaviour；第一次拉开任意抽屉播 14，取第三格门禁卡播 15）、`FilingCabinetDrawer`（柜子A 四格抽屉，各自开关）、`FilingCabinetCard`（柜子A 第三格真门禁卡，跟随 Drawer3；用临时卡试过安全门、且该抽屉完全关过一次后才从底板下露出）；所有柜子（16 个储藏室柜 + 柜子A）的**最上层 `Drawer1` 不可交互**（脚本的「允许交互」`interactable` 设为 false，不显示提示、按键无效，碰撞体保留）、`SafeDoorInteract` 安全门、`BreathGlow` 紫色呼吸光），台词在 `Assets/Yarn/Level2.yarn`（`LV2_*` 节点），场景节点 `关卡/储藏室` 与 `关卡/打印室`（各含 `建筑` / `道具`，`关卡/大厅/门` 下是两扇北门）。

## 房间几何、门与道具位置

北墙两扇门后面是「储藏室 11×11（x -21..-10，z 4.8..15.8）+ 前室 8×9（x -9.7..-1.7，z 4.8..13.8，西南实心块 5.0×4.8 在 -9.7..-4.7 × 4.8..9.55）+ 1 m 空腔（-1.4..-0.4）+ 打印室 33×18（x -0.1..32.9，z 4.8..22.8）」；

打印室按《Back D Room.xlsx》「办公室区域」摆了 12 组 × 4 个 `CubicleV2`（`Assets/Prefabs/Props/CubicleV2.prefab`，父节点 `道具/办公桌`，左 6 组与右 6 组之间留 3 列，整组随房间东扩向 +x 偏移 2 m，东西两侧各留约 5.3 m）；

前室东墙 -1.7..-1.4、打印室西墙 -0.4..-0.1 各自独立（空腔无地板无天花）；

储藏室门在隔墙 A 的 z 10.93..12.43（= 前室 9 m 去掉实心块 4.8 之后的中线），`AutoDoor(mode=Hinge, autoClose=false)` 铰链在门洞**南侧**（z 10.93）、门板向北伸并比门洞多压北侧 4 cm，`hingeOpenAngle=-95°` 向储藏室内开（门扇扫向西南、贴在储藏室东墙上，`柜子_门南` 因此摆在 z=8.8），克隆自红门，并挂上 `TimedDoorLock`（进入储藏室时关门并锁 60 s，红门不带）；

假门在西墙同段（`DoorBox` 1.54×2.44×0.08、无碰撞无脚本）；

大厅北墙随三区重新切段：西 -21.3..-3.95、中 -2.45..3.0、东 4.5..32.9，红门 -3.95..-2.45（在实心块东面 -4.7 与前室东墙 -1.7 之间居中）、蓝门 3.0..4.5 不动；

大厅中间墙仍是 z=1.5（不归中）；

进房触发盒随新几何：`进储藏室` 中心 (-15.5,1.2,10.3) 尺寸 10.8×2.4×10.8、`进打印室` 中心 (16.4,1.2,13.8) 尺寸 32.8×2.4×17.8；

台词锚点：`柜子B_台词点` (-10.9,1.7,14.45)、`进储藏室_台词点` (-11.5,1.65,11.68)、`进打印室_台词点` (3.75,1.65,6.1)、`复印机_台词点` (6.65,1.65,6.84)、`柜子A_台词点` (0.45,1.7,21.61)；

三区各加 `灯光` 点光（储藏室 2、前室 1、打印室 6，range 11、强度 1.45、暖白、无阴影）；

复印机仍是 **Quad + `YBillboard`** 贴 `Copier` 材质（放在打印室西南组 cubicle 的东侧桌面上，(6.75,1.11,7.55)，桌面顶 y=0.80）；

柜子B 已和其它柜子统一成抽屉柜模型、不再特殊（最东的北排柜子）；

## 柜子 A、其余柜子与北门/安全门

柜子A 已换成 `FilingCabinet_Separated.fbx` 实体模型（`Body` + `Drawer1..4`，根在 `(0.45,0,22.25)`（打印室西北角）、Y180），Body/抽屉用 URP/Unlit `FilingCabinet_Body`/`FilingCabinet_Drawer`，绿色描边用 `CabinetOutline.shader` + `Cabinet_Outline.mat` 和平滑法线网格 `Assets/Art/Meshes/FilingCabinet/*_Outline.asset`（原网格硬边分裂法线，直接外扩会裂），**Body/抽屉材质必须 `_Cull=0` 双面**（FBX 是单面壳体、抽屉是开口朝上的盒子且法线全朝外，单面时拉开抽屉后视线穿过内表面直达 inverted-hull 的内侧背面，整个抽屉口会变成一片描边绿），卡片用 `AccessCard.mat`（Unlit + Alpha Clip）。

柜子A 根只挂协调器；

每格抽屉只给正面板薄 `BoxCollider`（`(0,0,0.42)`/`(0.9,0.35,0.06)`），根 `BoxCollider` 前表面后退 0.1 m，射线才先打到抽屉；

卡片放在根下、由 `FilingCabinetCard` 跟随 Drawer3，抽屉拉开后才进入可瞄准区域，其余 15 个柜子实例化 `Assets/Prefabs/Props/FilingCabinet_Separated.prefab`（16 个柜子的 64 格抽屉全部可开，每格 `EmptyCabinetDrawer`；累计拉开 3 格不同抽屉后由 `= 关卡/储藏室/抽屉剧情` 的 `StorageDrawerQuest` 播一次 `LV2_CabinetB`，之后不再触发），两间房的墙用 `Wall_Storage_11`/`_9`/`_18`/`_29`/`_33`（按 11/9/18/29/33 m 长分别另存，`_BaseMap_ST` 按 0.28 列/米 换算）、地板用 `Floor_Storage_L1`/`Floor_Ante_L1`/`Floor_Print_L1`（`Floor_Office_Level2` 派生，平铺 5.5×5.5 / 4×4.5 / 16.5×9）。

两扇北门是 `AutoDoor(mode=Hinge, autoClose=false)`（门扇 `门板` 挂在子节点 `铰链` 下，`铰链` 上带 `Rigidbody` + `HingeJoint`；门板 1.54×2.44×0.08 贴在墙北面 z 4.802..4.882（比 1.5 m 门洞宽 4 cm，压住对侧门框不留缝）；门板必须留在铰链节点下，`AutoDoor.Awake` 会在铰链下没有门扇时报错并禁用（否则交互、音效都正常但门板纹丝不动）；两扇都铰在门洞西侧（玩家面对门时的左侧）门框角上，向房间内旋转，`hingeOpenAngle` 都是 -95°），且**必须对准门板按交互键才开**（`DoorInteract`，靠 `AutoDoor.IsOpen`/`Locked` 判断，根节点的 `isTrigger` 盒已删掉，所以靠近不会自动开）；

`安全门`（原 `Door_Mid_Safe`）也是同样结构：门板 1.54×2.44×0.30 贴在中间墙南面之外（北面 z=1.348，与墙面留 2 mm），`铰链` 在门洞西侧 `(-0.77, 1.22, 1.198)`，`hingeOpenAngle` 95° 向大厅内转开，转开后门洞露出 `空间叠加` 的传送面；

三扇门都由子物体 `门板` 用 `DoorBox` 网格做实心门板。**`AutoDoor.Locked` 同时挡住 `OnTriggerEnter` 与 `Open()`**，所以安全门靠不近也打不开，只有 16 号拿到真卡才解锁升起并把 `SafeDoorInteract.exitOpened`（UnityEvent）抛出去当切场景接口；

储藏室门（前室↔储藏室）则在玩家进储藏室时由 `TimedDoorLock.LockForAWhile()` 关门 + 锁 60 s 再自动开（12 号"锁定柜子无法交互"必须在 13 号解开，否则 15 号永远触发不了）。

交互判定沿用 `InteractFocus`+`InteractPromptView`，要求 `Cursor.lockState == Locked`
