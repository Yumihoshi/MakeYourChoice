# 文件柜 A：可开抽屉与门禁卡（2026-11-19）

## 背景

关卡二打印室的 `柜子A` 原本是一张 Quad + `YBillboard` 的平面道具，只承担“看一眼、播两段台词”的剧情触发。现在替换为 `Assets/Art/Models/filing-cabinet/source/FilingCabinet_Separated.fbx` 的实体文件柜（`Body` + `Drawer1..4`），要求：

- 保留原来的两个 `DialogueTrigger`（`LV1_CabinetA_First` / `LV2_CabinetA_Second`）和 `GlowCabinetInteract` 的剧情职责；
- 四格抽屉各自可交互开合；
- 从上往下第三格抽屉里放 `Assets/Art/素材/门禁卡.png`，满足流程后可以交互拿走。

## 设计决定

### 1. 根节点改成纯协调器，抽屉各自是交互物

`柜子A` 的根不能继续继承 `InteractableBase`：子抽屉的 collider 也是根的子物体，根脚本的 `IsAimed()` 用 `hit.collider.transform.IsChildOf(transform)` 判定，会同时命中根和抽屉，一次点击触发两个交互。

因此：

- 根节点只保留 `GlowCabinetInteract`（普通 `MonoBehaviour`），负责“是否允许开抽屉”“第一次拉开播 14”“拿卡播 15”；
- 四格抽屉各挂 `FilingCabinetDrawer : InteractableBase`，只处理自己的开合；
- 门禁卡挂 `FilingCabinetCard : InteractableBase`，只处理拿卡。

### 2. collider 分层：抽屉正面可打到，卡片在抽屉拉开前不可打到

- `Body` 的碰撞由根上的 `BoxCollider` 承担：`center=(0,0.9,-0.05)`、`size=(1,1.8,0.9)`。根旋转 180° 后，这个盒子的前表面在柜体正面之后约 0.1 m，射向抽屉正面的射线会先命中抽屉自己的 collider。
- 每格抽屉只给**正面板**加薄 `BoxCollider`：`center=(0,0,0.42)`、`size=(0.9,0.35,0.06)`。不用 0.9×0.35×0.87 的实心盒，否则第三格里的小卡片会被抽屉自己的 collider 完全包住，射线永远打不到。
- 卡片 `BoxCollider` 是 `size=(1,1,0.12)` 的薄盒，随卡片缩放后约 0.14×0.14×0.017 m。抽屉关闭时卡片位于根 collider 内部（不可交互是预期）；拉开 0.4 m 后卡片移到根 collider 前方，玩家低头即可瞄准。

### 3. 卡片不是抽屉的子物体

卡片如果挂在 `Drawer3` 下，抽屉的 `IsAimed()` 会把卡片也当成自己（`IsChildOf`），交互会串。卡片父级放在柜子根下，`FilingCabinetCard` 每帧用序列化的 `drawerMount`（指向 `Drawer3`）和 `localPositionInDrawer` / `localEulerAnglesInDrawer` 跟随抽屉的世界位姿。这样抽屉的补间和卡片的跟随互不干扰。

### 4. 剧情映射

沿用 `Assets/Yarn/Level2.yarn` 里已有的两个节点，不改台词：

- **第一次拉开任意一格抽屉**：`GlowCabinetInteract.OnDrawerOpened()` 在 `!state.CheckedCabinetA` 时 `MarkCheckedCabinetA()` 并播 `LV1_CabinetA_First`（触发 14“只是反光”）。之后不再重复。
- **交互第三格里的门禁卡**：
  - 还没满足 `CheckedCabinetA` 或 `TriedSafeDoorWithTempCard` 时，只重播 `LV1_CabinetA_First`，卡片留在抽屉里；
  - 两个条件都满足后，`state.TakeRealCard()` + 播 `LV2_CabinetA_Second`（触发 15），卡片 `SetActive(false)` 消失。
- **真卡出现时机**：用临时卡试过安全门（`TriedSafeDoorWithTempCard`）之后真卡才允许露出。若试卡时第三格抽屉正开着，卡片继续藏在抽屉底板下，必须把抽屉完全推回关闭位一次、再重新拉开，才抬到抽屉内显示；若试卡时抽屉已经关着，之后拉开即可看到。未露出时卡片不响应交互。
- 锁柜期间（`state.CabinetsLocked`，即刚拿到临时卡、还没试过安全门）只能把已经拉开的抽屉推回去，不能开新抽屉，也不能拿卡；这对应原流程里“柜子暂时锁住”的阶段。

### 5. 视觉：Unlit + 绿色描边

打印室没有独立灯光，整间房靠 `RenderSettings` 的 Flat ambient（约 0.212）和贴图本身的烘焙明暗。新模型如果直接用 FBX 自带的 URP/Lit 材质会明显偏暗，因此：

- `Body` / 四个 `Drawer` 换成 URP/Unlit 材质，贴图仍用 `FilingCabinet1.png` 与 `FilingCabinet1Drawer.png`，和同房间的复印机、办公桌保持一致的可见度；
- 绿色高光用自定义 `Assets/Shaders/CabinetOutline.shader`（URP inverted-hull）实现：`Cull Front`、`ZWrite On`、`ZTest LEqual`，沿法线外扩 `_OutlineWidth=0.012`，颜色 `(0.35,1,0.55,1)`；
- 每个 `Body` / `Drawer` 下加一个 `Outline` 子物体，使用同一网格的**平滑法线副本**（`Assets/Art/Meshes/FilingCabinet/*_Outline.asset`）。原 FBX 网格在硬边处是分裂法线，直接外扩会在棱角出现绿色裂缝；平滑副本按位置合并法线，只用于描边壳，不改原模型网格；
- `FilingCabinet_Body.mat` / `FilingCabinet_Drawer.mat` 必须**双面**（URP/Unlit `_Cull = 0`）。FBX 是单面壳体：抽屉是开口朝上的盒子（底面、前后面、左右面共 10 张三角，法线全部朝外），柜体也没有内壁面。inverted-hull 只画背面，单面时拉开抽屉后视线穿过朝外的内表面，直接落到描边壳的内侧背面，整个抽屉口就被填成一片绿色。改双面后内壁正常渲染并写深度（比描边壳近 `_OutlineWidth` 0.012 m），描边只在轮廓露出。同一机位网格射线核对：漏色从 17.5% 降到 0.6%，剩下的 0.6% 是轮廓本身（左右斜视角不变，说明只去掉了内部漏色）。

### 6. 场景摆放

- 根 Transform：`localPosition=(2.2,0,9.19)`、`localRotation=(0,180,0)`、`localScale=1`；
- 旋转 180° 后模型正面（本地 +Z）朝房间 -Z，抽屉向房间方向拉出；柜体背面贴北墙内表面（z≈9.7）；
- 原 `柜子A_台词点` 保持在 `(2.2,1.7,8.95)`，两段剧情仍从柜子前方飘字。

## 验收

1. Play 关卡二，进入打印室，对准任意一格抽屉按交互键：抽屉沿正面滑出约 0.4 m，第一次拉开时播“我说了，只是反光。”。
2. 再按一次同一格：抽屉推回；四格可以各自独立开合，也可以同时拉开多格。
3. 拿到临时门禁卡后（柜子锁住期间）不能拉开新抽屉；用临时卡试过安全门后锁解除。
4. 用临时卡试过安全门后，拉开从上往下第三格，低头能看到一张白底蓝条的小门禁卡；条件未满足时交互只重播第一段台词，卡片不消失。若试卡时第三格正开着，卡片在推回关闭位之前不显示，重新拉开后才出现。
5. 满足条件后交互卡片：播第二段台词，卡片消失，`BackRoomFlowState.GotRealCard` 为 true，随后安全门可用真卡打开。
6. 柜体与抽屉有绿色描边，棱角处没有明显断裂；模型在打印室的 ambient 下亮度与复印机/办公桌接近。
7. 拉开任意一格抽屉俯视内部：抽屉内壁是抽屉自己的贴图，不会整片变成绿色描边色；柜体开口内侧也不会露绿。
