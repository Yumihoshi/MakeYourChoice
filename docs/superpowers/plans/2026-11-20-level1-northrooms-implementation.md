# Level1 北侧三区调整 实现计划

> **面向 AI 代理的工作者：** 逐任务实现本计划，步骤用 `- [ ]` 勾选跟踪。场景改动一律走 Unity MCP（`execute_code` / `manage_*`）并在编辑器里保存场景，**不手写 YAML**。视觉验收交给用户手测。

**目标：** 把 Level1 大厅北侧的两间房（储藏室 9.3×5.19、打印室 9.3×5.19）改造成「储藏室 11×11 + 前室 8×9（L 形，西南实心块 5.0×4.8）+ 1 m 空腔 + 打印室 29×18」，柜子全部换成抽屉柜模型、南排可开关抽屉，触发/台词按新几何重摆。

**架构：** 不改任何系统边界——沿用现有 `AutoDoor`、`InteractableBase` 系列、`RoomEntryTrigger`、`BackRoomFlowState`、`Yarn` 剧情；只新增一个无台词的 `EmptyCabinetDrawer`。几何用 Cube + 现有材质扩展（每 2 m 一格的 `_BaseMap_ST` 写进材质资产），柜子统一实例化 `FilingCabinet_Separated` 预制体。

**技术栈：** Unity 2022.3 URP、ProBuilder 场景网格（Cube 图元即可）、Odin `[LabelText]`、DOTween、Yarn Spinner 3、Unity MCP（编辑器内执行 C# + 保存场景）。

---

## 实施状态（2026-11-20 已落地）

- [x] 任务 1 材质、任务 2 大厅北墙切段、任务 3 三区地板/墙/实心块、任务 4 储藏室门与假门、任务 5 十六个抽屉柜、任务 6 `EmptyCabinetDrawer`、任务 7 触发区与台词锚点、任务 8 灯光、任务 9 Yarn 台词。
- 场景已 `SaveScene`；Play 过一次，Console 无报错（只有已知的 EventSystem 重复提示）。
- 待办：用户手测（见规格 HTML 的验收清单与交付说明）。
- 修复（2026-11-20，用户报「储藏室门不铰链式打开 + 门口地面有缝」）：
  - `Assets/Scripts/Level/Door/HingeDoorMotion.cs` 不再用 `HingeJoint` 电机：枢轴刚体设 `isKinematic`，`FixedTick` 按剩余角度限速 `MoveRotation`。原实现转动时把门扇碰撞体整组关掉，刚体失去全部形状后 PhysX 不再模拟它、电机彻底失效（实测 `HingeJoint.angle` 恒为 0、手动给角速度也立刻被清零；红门没有这问题是因为它先动起来了）。
  - 门扇放行改成把 `Player` 层写进 `Collider.excludeLayers`，碰撞体始终启用。
  - 场景 `= 关卡/储藏室/建筑/门槛`（-10.05..-9.65 × z 10.85..12.51、y -0.3..0）补上前室地板（东缘 -9.7）与储藏室地板（西缘 -10）之间 0.3 m 的洞。
  - 实测：储藏室门 +95°（Y90→184.5）、关门回 Y90.5、放行层 `excludeLayers` 到位后清零；红门 -94.5° 同样正常。
- 修复（2026-11-20，用户报「门以把手一侧为轴、应绕另一侧打开」）：
  - 储藏室门铰链从门洞北侧改到**南侧**：`铰链` world `(-10.042, 1.22, 10.93)`（`localPosition` 0.75,1.22,0.172），`门板` `localPosition` (-0.77,0,0)、跨 z 10.93..12.47（比门洞多压北侧 4 cm），`hingeOpenAngle` 改 **-95°**；门扇扫向西南、开门后贴在储藏室东墙上，所以 `柜子_门南` 从 z 10.2 挪到 **z 8.8**。
  - 实测（Play、`runInBackground` 保证推进）：开门 Y90→355.5°（-94.5°）、`OpenProgress 0.995`、门板落在 (-10.810,1.220,10.870)；关门回 Y89.5°、`progress 0.005`、`excludeLayers` 归 0 重新挡人。场景已 SaveScene。
- 修复（2026-11-20，用户报「大厅和前室的门居中 + 柜子都能开、拉开一定数量才播剧情」）：
  - 红门根 x -3.75 → **-3.2**（铰链 -3.95、门板中心 -3.18、跨 -3.95..-2.41）：在实心块东面 -4.7 与前室东墙 -1.7 之间居中；`北墙_西段` 收成 -21.3..-3.95、`北墙_中段` 改 -2.45..3.0、`北墙_门楣_红` 跟到 -3.2、触发 12 移到 x -3.2。
  - 新增 `Assets/Scripts/Level/BackRooms/StorageDrawerQuest.cs`（`requiredDrawers = 3`，累计拉开 3 格不同抽屉后播 `LV2_CabinetB` 并 `MarkOpenedCabinetB()`）；`EmptyCabinetDrawer` 增加 `state`/`quest`，首次拉开报数、锁柜后只能关。
  - 16 个柜子共 64 格抽屉统一挂 `EmptyCabinetDrawer`（此前只有南排 6 个可开）；`柜子_北6` 上的 `StorageCabinetInteract` 删除，柜子B 不再特殊。
  - 实测：Edit 模式射线先命中抽屉（dist 0.360）；Play 里红门 -94.5°、`OpenProgress 0.995`，`RegisterOpened()` 三次后 `OpenedCount=3`、`OpenedCabinetB=true`。
- 修复（2026-11-20，用户报「最上层的抽屉不允许交互」）：
  - `EmptyCabinetDrawer` / `FilingCabinetDrawer` 增加 `[SerializeField, LabelText("允许交互")] bool interactable = true`，`CanInteract()` 首个条件为 `interactable`；为 false 时 `InteractableBase.IsAimed()` 直接返回 false，不登记准心、按键无效，抽屉碰撞体保留。
  - 场景 `Assets/Scenes/Level1.unity`：16 个储藏室柜与柜子A 共 17 个 `Drawer1`（local y=1.575）把 `interactable` 设为 false，其余 51 格保持 true；存盘后核对 YAML 中 `interactable: 0` 17 处、`interactable: 1` 51 处。
- 修复（2026-11-20，用户报「前室和储藏室的门关得太慢，玩家会 peek 进去后快速出来」）：
  - `AutoDoor` 增加 `hingeCloseSpeed` / `hingeCloseGain`（旋转门专用，0 表示沿用开门的 `hingeSpeed` / `hingeGain`）；`HingeDoorMotion` 在 `Open()` / `Close()` 里记 `_closing`，`FixedTick` 按方向选角速度上限与刚度。
  - 储藏室门 `= 关卡/储藏室/储藏室门` 设 `hingeCloseSpeed = 720`、`hingeCloseGain = 24`，关门约 0.22 s（原 150°/s / 4 约 1.3 s），开门保持 150°/s / 4。
  - 另注：门扇转动期间 `Player` 层被排除在碰撞之外、转到位且玩家离开门板包围盒后恢复，因此关门必须够快才能在玩家退回前把门关严。

## 现状基线（2026-11-20 场景 `Assets/Scenes/Level1.unity`）

- `= 关卡/大厅/建筑`：`北墙_左`（4.8×3×0.3）、`北墙_中`（6×3×0.3）、`北墙_右`（4.8×3×0.3）、`北墙_门楣_左/右`（1.5×0.6×0.3）、`南墙_左/右`、`南墙_门楣`、`西墙/东墙`（0.3×3×9.45）、`地板/天花板`（18.6×0.3×9.45）、`中间墙`、`安全门背板`。
- `= 关卡/大厅/门`：`红门/铰链/门板`、`蓝门/铰链/门板`、`安全门/铰链/门板`（都是 `DoorBox` + `AutoDoor`）。
- `= 关卡/储藏室`：`进入储藏室`（BoxCollider 触发）、`建筑/{地板,天花板,北墙,西墙,隔墙}`、`道具/{柜子B,空柜子}`（Quad + YBillboard）、`剧情锚点/柜子B_台词点`。
- `= 关卡/打印室`：`进入打印室`、`建筑/{地板,天花板,北墙,东墙}`、`道具/{柜子A(Model/Body+Drawer1..4), 门禁卡, 复印机(+紫光), 办公桌(9 个子物体)}`、`剧情锚点/{复印机_台词点,柜子A_台词点,进打印室_台词点}`。
- `= 关卡/剧情状态/储藏室打印室`：`BackRoomFlowState`。
- 12/13 号触发目前**不在场景里**（随旧前室一起没有），本计划新建。

---

## 文件结构

- 新建：`Assets/Scripts/Level/BackRooms/EmptyCabinetDrawer.cs`——南排空柜的开关抽屉，无台词。
- 新建材质：`Assets/Materials/Level/Floor_Storage_L1.mat`、`Floor_Ante_L1.mat`、`Floor_Print_L1.mat`、`Wall_Storage_11.mat`、`Wall_Storage_9.mat`、`Wall_Storage_18.mat`、`Wall_Storage_29.mat`。
- 新建预制体：`Assets/Prefabs/Props/FilingCabinet_Separated.prefab`（由 `柜子A/Model` 另存，供 16 个柜子实例化）。
- 修改：`Assets/Scenes/Level1.unity`（场景内容）。
- 修改：`Assets/Yarn/Level2.yarn`（仅 `LV2_08` 一句）。
- 修改：`docs/superpowers/specs/2026-11-20-level1-northrooms-adjustment-design.html`（§13 状态）、`AGENTS.md`（已有能力表）。
- 不新建程序集、不加框架/事件总线。

## 实施约定

- git 只做查询（`status`/`log`/`diff`），**不 commit**。
- 场景改动只通过 Unity MCP 的 `execute_code`（编辑器内 C#）或 `manage_*` 工具；每批改完 `EditorSceneManager.MarkSceneDirty` + `EditorSceneManager.SaveScene`，再 `read_console` 确认无报错。
- 视觉与手感验收由用户手测，代理不模拟输入。
- 坐标一律按规格 §5 的施工表；墙厚 0.3、高 3（中心 y=1.5），地板/天花板厚 0.3（中心 y=-0.15 / 3.15）。

---

## 任务 1：材质资产（地板与墙的平铺）

**文件：**
- 新建：`Assets/Materials/Level/Floor_Storage_L1.mat`、`Floor_Ante_L1.mat`、`Floor_Print_L1.mat`
- 新建：`Assets/Materials/Level/Wall_Storage_11.mat`、`Wall_Storage_9.mat`、`Wall_Storage_18.mat`、`Wall_Storage_29.mat`
- 参考：`Assets/Materials/Level/Floor_Office_Level2.mat`（ST 4.65×2.6）、`Wall_Divider.mat`（ST 1.37×1.5）

- [ ] **步骤 1：复制资产并写 ST**

`execute_code`（编辑器内）：

```csharp
using UnityEditor;
using UnityEngine;
using System.IO;

void MakeMat(string src, string dst, Vector2 st)
{
    if (File.Exists(dst)) AssetDatabase.DeleteAsset(dst);
    if (!AssetDatabase.CopyAsset(src, dst)) throw new System.Exception("copy failed " + src);
    var m = AssetDatabase.LoadAssetAtPath<Material>(dst);
    m.SetTextureScale("_BaseMap", st);
    EditorUtility.SetDirty(m);
}

// 地板：2 m 一格 → 储藏室 11×11、前室 8×9、打印室 29×18
MakeMat("Assets/Materials/Level/Floor_Office_Level2.mat", "Assets/Materials/Level/Floor_Storage_L1.mat", new Vector2(5.5f, 5.5f));
MakeMat("Assets/Materials/Level/Floor_Office_Level2.mat", "Assets/Materials/Level/Floor_Ante_L1.mat", new Vector2(4f, 4.5f));
MakeMat("Assets/Materials/Level/Floor_Office_Level2.mat", "Assets/Materials/Level/Floor_Print_L1.mat", new Vector2(14.5f, 9f));
// 墙：每米 0.28 列（Wall_Divider 4.9 m → 1.37），高度沿用 1.5
MakeMat("Assets/Materials/Level/Wall_Storage.mat", "Assets/Materials/Level/Wall_Storage_11.mat", new Vector2(3.08f, 1.5f));
MakeMat("Assets/Materials/Level/Wall_Storage.mat", "Assets/Materials/Level/Wall_Storage_9.mat", new Vector2(2.52f, 1.5f));
MakeMat("Assets/Materials/Level/Wall_Storage.mat", "Assets/Materials/Level/Wall_Storage_18.mat", new Vector2(5.04f, 1.5f));
MakeMat("Assets/Materials/Level/Wall_Storage.mat", "Assets/Materials/Level/Wall_Storage_29.mat", new Vector2(8.12f, 1.5f));
AssetDatabase.SaveAssets();
return "ok";
```

- [ ] **步骤 2：验证**
`read_console` 无报错；在 Project 里选中 7 个新材质，确认 `Surface Inputs → Base Map` 的 Tiling 是上面写的值。

---

## 任务 2：大厅北墙重切（西段 16.8 / 中段 6.0 / 东段 24.4）

**文件：** 场景 `Assets/Scenes/Level1.unity`，节点 `= 关卡/大厅/建筑`

- [ ] **步骤 1：先取原北墙材质**

`execute_code` 读取 `北墙_左` 的 `MeshRenderer.sharedMaterial` 并打印名字（后面新段沿用同一材质，避免色差）。

- [ ] **步骤 2：删旧段、建新段**

```csharp
var arch = GameObject.Find("= 关卡/大厅/建筑").transform;
Material wall = GameObject.Find("= 关卡/大厅/建筑/北墙_左").GetComponent<MeshRenderer>().sharedMaterial;
foreach (var n in new[]{"北墙_左","北墙_中","北墙_右","北墙_门楣_左","北墙_门楣_右"})
    Object.DestroyImmediate(arch.Find(n).gameObject);

GameObject Box(string name, float x1, float x2, float z1, float z2, float y1, float y2, Material m)
{
    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
    go.name = name; go.transform.SetParent(arch, false);
    go.transform.position = new Vector3((x1+x2)/2, (y1+y2)/2, (z1+z2)/2);
    go.transform.localScale = new Vector3(x2-x1, y2-y1, z2-z1);
    go.GetComponent<MeshRenderer>().sharedMaterial = m;
    return go;
}
// 三段北墙（门洞红 -4.5..-3.0、蓝 3.0..4.5）
Box("北墙_西段", -21.3f, -4.5f, 4.5f, 4.8f, 0f, 3f, wall);
Box("北墙_中段", -3.0f, 3.0f, 4.5f, 4.8f, 0f, 3f, wall);
Box("北墙_东段", 4.5f, 28.9f, 4.5f, 4.8f, 0f, 3f, wall);
// 门楣（门洞上方 2.44..3.0）
Box("北墙_门楣_红", -4.5f, -3.0f, 4.5f, 4.8f, 2.44f, 3f, wall);
Box("北墙_门楣_蓝", 3.0f, 4.5f, 4.5f, 4.8f, 2.44f, 3f, wall);
```

- [ ] **步骤 3：验证**
红门/蓝门/安全门/电梯门的位置没动；大厅地板 18.6×9.45 仍盖住北墙内侧 4.5；`read_console` 无报错。

---

## 任务 3：拆旧房、建三区地板/天花与空腔

**文件：** 场景根 `= 关卡/储藏室/建筑`、`= 关卡/打印室/建筑`、新建 `= 关卡/前室`（含 `建筑`）

- [ ] **步骤 1：删旧建筑**

`execute_code`：`DestroyImmediate` 掉 `储藏室/建筑` 下全部子物体、`打印室/建筑` 下全部子物体（保留 `建筑` 空节点本身）；新建 `= 关卡/前室/建筑`。

- [ ] **步骤 2：铺地板/天花板**

```csharp
GameObject Make(string parentPath, string name, float x1,float x2,float z1,float z2,float y1,float y2, Material m)
{
    var p = GameObject.Find(parentPath).transform;
    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
    go.name = name; go.transform.SetParent(p, false);
    go.transform.position = new Vector3((x1+x2)/2,(y1+y2)/2,(z1+z2)/2);
    go.transform.localScale = new Vector3(x2-x1, y2-y1, z2-z1);
    go.GetComponent<MeshRenderer>().sharedMaterial = m;
    return go;
}
var floorS = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level/Floor_Storage_L1.mat");
var floorA = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level/Floor_Ante_L1.mat");
var floorP = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level/Floor_Print_L1.mat");
var wallS = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level/Wall_Storage_11.mat");

// 地板（厚 0.3，中心 y=-0.15）
Make("= 关卡/储藏室/建筑","地板",-21f,-10f,4.8f,15.8f,-0.3f,0f,floorS);
Make("= 关卡/前室/建筑","地板",-9.7f,-1.7f,4.8f,13.8f,-0.3f,0f,floorA);
Make("= 关卡/打印室/建筑","地板",-0.1f,28.9f,4.8f,22.8f,-0.3f,0f,floorP);
// 天花板（中心 y=3.15）
Make("= 关卡/储藏室/建筑","天花板",-21f,-10f,4.8f,15.8f,3f,3.3f,wallS); // 用墙材质或现有天花材质
Make("= 关卡/前室/建筑","天花板",-9.7f,-1.7f,4.8f,13.8f,3f,3.3f,wallS);
Make("= 关卡/打印室/建筑","天花板",-0.1f,28.9f,4.8f,22.8f,3f,3.3f,wallS);
```

> 天花板材质取旧 `天花板` 的 sharedMaterial（删之前先缓存），不要用墙材质顶替；空腔 -1.4..-0.4 两样都不铺。

- [ ] **步骤 3：建墙与实心块**

```csharp
var w11 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level/Wall_Storage_11.mat");
var w9  = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level/Wall_Storage_9.mat");
var w18 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level/Wall_Storage_18.mat");
var w29 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level/Wall_Storage_29.mat");
// 储藏室
Make("= 关卡/储藏室/建筑","西墙",-21.3f,-21f,4.8f,16.1f,0f,3f,w11);
Make("= 关卡/储藏室/建筑","北墙",-21.3f,-9.7f,15.8f,16.1f,0f,3f,w11);
// 隔墙 A：门洞 z 10.93..12.43
Make("= 关卡/储藏室/建筑","隔墙",-10f,-9.7f,4.8f,10.93f,0f,3f,w9);
Make("= 关卡/储藏室/建筑","隔墙",-10f,-9.7f,12.43f,15.8f,0f,3f,w9);
// 前室
Make("= 关卡/前室/建筑","北墙",-9.7f,-1.4f,13.8f,14.1f,0f,3f,w9);
Make("= 关卡/前室/建筑","东墙",-1.7f,-1.4f,4.8f,13.8f,0f,3f,w9);
Make("= 关卡/前室/建筑","实心块",-9.7f,-4.7f,4.8f,9.55f,0f,3f,w9);
// 打印室
Make("= 关卡/打印室/建筑","西墙",-0.4f,-0.1f,4.8f,22.8f,0f,3f,w18);
Make("= 关卡/打印室/建筑","北墙",-0.4f,29.2f,22.8f,23.1f,0f,3f,w29);
Make("= 关卡/打印室/建筑","东墙",28.9f,29.2f,4.8f,23.1f,0f,3f,w18);
```

- [ ] **步骤 4：验证**
三块地板互不重叠、空腔 1 m 处没有地板；墙与地板相接处无 1 cm 以上的缝；`read_console` 无报错；保存场景。

---

## 任务 4：储藏室门与假门

**文件：** 场景 `= 关卡/大厅/门/红门`（克隆源）、新建 `= 关卡/储藏室/门`、`= 关卡/储藏室/建筑/假门`

- [ ] **步骤 1：克隆红门**

`execute_code`：`Instantiate(红门, parent)` → 改名 `储藏室门`，放到 `= 关卡/储藏室` 下；`铰链` 位置 `(-9.85, 1.22, 12.43)`（门洞北侧，隔墙 A 西面外 2 mm 侧），`铰链` 绕 Y 旋转 90°（使门板贴隔墙 A 的储藏室侧，沿东西向），`门板` 尺寸 1.5×2.44×0.08、local z 0.02；`AutoDoor` 的 `hingeOpenAngle` 先填 95，方向不对改 -95；`autoClose=false`、`Locked=false`。

- [ ] **步骤 2：假门**

在 `= 关卡/储藏室/建筑` 下建空物体 `假门`，位置 `(-20.99, 1.22, 11.68)`、Y 旋转 90°；子物体 `门板`：`MeshFilter` 用 `Assets/Art/Meshes/DoorBox.asset`、材质 `Door_Office`、scale (1.5, 2.44, 0.08)；**不加碰撞体、不加脚本**。

- [ ] **步骤 3：验证**
储藏室门能开（按交互键）、开向储藏室内；假门看不见碰撞、瞄准不出现提示；`read_console` 无报错。

---

## 任务 5：柜子（抽屉柜模型 ×16 + 柜子B + 柜子A）

**文件：** `Assets/Prefabs/Props/FilingCabinet_Separated.prefab`（新建）、场景 `= 关卡/储藏室/道具`、`= 关卡/打印室/道具`

- [ ] **步骤 1：另存预制体**

`execute_code`：`PrefabUtility.SaveAsPrefabAsset(柜子A/Model.gameObject, "Assets/Prefabs/Props/FilingCabinet_Separated.prefab")`（`Model` 的 local 变换归零后另存；不要保存根上的脚本）。

- [ ] **步骤 2：删旧贴图柜**

`DestroyImmediate` `储藏室/道具/柜子B`、`储藏室/道具/空柜子`（都是 Quad + YBillboard）。

- [ ] **步骤 3：实例化 16 个**

```csharp
var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/FilingCabinet_Separated.prefab");
var root = new GameObject("柜子").transform; root.SetParent(GameObject.Find("= 关卡/储藏室").transform, false);
// 北排 6：中心 x -19.9 -18.1 -16.3 -14.5 -12.7 -10.9，z 15.2..15.8，正面朝南（Y 180）
float[] xs = {-19.9f,-18.1f,-16.3f,-14.5f,-12.7f,-10.9f};
for (int i = 0; i < 6; i++) {
    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
    go.name = i == 5 ? "柜子B" : "柜子_" + i;
    go.transform.position = new Vector3(xs[i], 0, 15.5f);   // 模型原点在柜底中心
    go.transform.rotation = Quaternion.Euler(0, 180, 0);
}
// 南排 6：z 4.8..5.4，正面朝北（Y 0），每格挂 EmptyCabinetDrawer
for (int i = 0; i < 6; i++) {
    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
    go.name = "空柜_" + i;
    go.transform.position = new Vector3(xs[i], 0, 5.1f);
    go.transform.rotation = Quaternion.identity;
}
// 两侧 4：内门两侧（-10.6..-10.0）+ 假门两侧（-21..-20.4），z 9.83..10.93 / 12.43..13.53
```

> 模型正面朝向与原点以 `柜子A` 为准：`柜子A` 根在 (2.2,0,9.19)、Y180、正面朝南；实例化后按同款正面朝向（北排 Y180 朝南、南排 Y0 朝北），位置用「柜背面贴墙、正面朝房间」微调，以目视为准。

- [ ] **步骤 4：柜子B**

第 6 个（-10.9）改名 `柜子B`，根上加 `StorageCabinetInteract`（引用 `剧情状态/储藏室打印室` 与台词锚点，照抄旧组件字段）；`Body` 子物体补 `BoxCollider`（尺寸取 Body 的 Renderer bounds，略小 2 mm）——射线要能打到 Body。

- [ ] **步骤 5：南排抽屉脚本**

每格 `Drawer1..4` 的**正面板**补 `BoxCollider`（0.8×0.35×0.05，贴正面外侧），挂 `EmptyCabinetDrawer`，`openDistance=0.28`、`duration=0.25`；互动提示由 `InteractableBase` 统一处理，不加台词。

- [ ] **步骤 6：柜子A 与复印机**

`柜子A` 根移到 (3.5, 0, 12.4)、Y180；`门禁卡`/`GlowCabinetInteract`/`FilingCabinetDrawer`/`FilingCabinetCard` 引用保持原样（都挂在根下，跟着走）；复印机不动 (8.5, 1.61, 8.4)；**删除 `打印室/道具/办公桌`**（用户确认暂不放桌）。

- [ ] **步骤 7：验证**
16 个柜子不穿墙、不互相重叠、正面朝向房间；柜子B 能交互、抽屉打不开；南排 6 个柜子的抽屉都能开合、无台词；`read_console` 无报错。

---

## 任务 6：`EmptyCabinetDrawer` 脚本

**文件：**
- 新建：`Assets/Scripts/Level/BackRooms/EmptyCabinetDrawer.cs`

- [ ] **步骤 1：写脚本**

```csharp
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 空的抽屉柜：对准抽屉正面按交互键沿柜体正面滑出/滑回，不播台词、不触发剧情。
    /// </summary>
    public sealed class EmptyCabinetDrawer : InteractableBase
    {
        [SerializeField, LabelText("拉出行程（米）")]
        float openDistance = 0.28f;

        [SerializeField, LabelText("开合时长（秒）")]
        float duration = 0.25f;

        [SerializeField, LabelText("沿局部 -Z 拉出")]
        bool useNegativeZ = true;

        Vector3 _closedLocalPosition;
        bool _open;

        void Awake()
        {
            _closedLocalPosition = transform.localPosition;
        }

        protected override void OnInteract()
        {
            _open = !_open;
            transform.DOKill();
            transform.DOLocalMove(TargetLocalPosition(), duration).SetEase(Ease.OutCubic);
        }

        Vector3 TargetLocalPosition()
        {
            if (!_open) return _closedLocalPosition;
            var dir = useNegativeZ ? Vector3.back : Vector3.forward;
            return _closedLocalPosition + dir * openDistance;
        }
    }
}
```

- [ ] **步骤 2：编译检查**
等 Unity 编译结束（`refresh_unity` 或等 2 s），`read_console` 无 CS 错误；Console 里不应出现 `InteractableBase` 未找到（命名空间 `Game.Level` 已有）。

- [ ] **步骤 3：验证**
在编辑器里选中一个南排抽屉，确认 Inspector 出现 `拉出行程` / `开合时长` / `沿局部 -Z 拉出` 三个中文项；抽屉的滑出方向与柜面垂直（方向反了勾/去勾 `useNegativeZ`）。

---

## 任务 7：触发区、台词点与流程

**文件：** `Assets/Scenes/Level1.unity`、`Assets/Yarn/Level2.yarn`

- [ ] **步骤 1：改 8 号**

`= 关卡/打印室/进入打印室` 的 BoxCollider：中心 (14.4, 1.2, 13.8)、尺寸 (28.8, 2.4, 17.8)（比房间各边窄 10 cm）；确认 `RoomEntryTrigger` 仍引用 `剧情状态/储藏室打印室` 与 LV2_08。

- [ ] **步骤 2：改 10 号**

`= 关卡/储藏室/进入储藏室`：中心 (-15.5, 1.2, 10.3)、尺寸 (10.8, 2.4, 10.8)。

- [ ] **步骤 3：新建 12、13 号触发**

```csharp
var trig = GameObject.Find("= 关卡/大厅/触发").transform;
GameObject TriggerBox(string name, Vector3 center, Vector3 size)
{
    var go = new GameObject(name); go.transform.SetParent(trig, false);
    go.transform.position = center;
    var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = size;
    // 图标
    EditorGUIUtility.SetIconForObject(go, EditorGUIUtility.IconContent("sv_icon_dot11_pix16_gizmo").image as Texture2D);
    return go;
}
TriggerBox("进前室_关门", new Vector3(-3.75f, 1.2f, 5.3f), new Vector3(1.6f, 2.4f, 0.9f)); // 12
TriggerBox("进前室_台词", new Vector3(-2.4f, 1.2f, 5.4f), new Vector3(3.2f, 2.4f, 1.2f));  // 13
```

门关/锁 60 s 用 `TimedDoorLock`（照抄旧用法：红门 AutoDoor 上已有或新加），进前室触发 12 时 `LockForAWhile()`；13 挂 `DialogueZoneTrigger`（`DialogueTrigger` 用 LV2_13），照旧写法。

> 2026-11-20 修正：12 / 13 两个前室触发区最终没放；关门 + 锁 60 s 改由 `进入储藏室`（触发 10）的 `RoomEntryTrigger` 执行，`TimedDoorLock` 挂在**储藏室门（前室↔储藏室）**上，红门（前室↔大厅）上的定时锁已移除。

- [ ] **步骤 4：11、14 号微调**

`BlueArrowInteract` 的交互盒随蓝门 → (3.9, 1.5, 6.0)；`柜子A` 的 `GlowCabinetInteract` 触发 14 的剧情触发区 → 中心 (3.5, 1.2, 11.6)、尺寸 (2.0, 2.4, 1.4)。

- [ ] **步骤 5：台词**

`Assets/Yarn/Level2.yarn` 的 `LV2_08`：把「蓝箭头通向隔间区」改成「打印机房」→ 按规格写「打印室」；其余节点不动。

- [ ] **步骤 6：验证**
`read_console` 无报错；Play 后由用户走流程（见任务 9）。

---

## 任务 8：灯光

**文件：** 场景 `= 关卡/储藏室/建筑`、`= 关卡/前室/建筑`、`= 关卡/打印室/建筑`（新建 `灯光` 节点）

- [ ] **步骤 1：加点光**

```csharp
Light Point(string parent, string name, Vector3 pos)
{
    var go = new GameObject(name); go.transform.SetParent(GameObject.Find(parent).transform, false);
    go.transform.position = pos;
    var l = go.AddComponent<Light>(); l.type = LightType.Point;
    l.range = 12f; l.intensity = 1.6f; l.color = new Color(1f, 0.93f, 0.8f);
    l.shadows = LightShadows.None;
    return l;
}
Point("= 关卡/储藏室/建筑","灯光", new Vector3(-15.5f, 2.9f, 10.3f));
Point("= 关卡/前室/建筑","灯光", new Vector3(-5.7f, 2.9f, 9.3f));
Point("= 关卡/打印室/建筑","灯光_1", new Vector3(6f, 2.9f, 8f));
Point("= 关卡/打印室/建筑","灯光_2", new Vector3(14f, 2.9f, 16f));
Point("= 关卡/打印室/建筑","灯光_3", new Vector3(24f, 2.9f, 10f));
```

- [ ] **步骤 2：验证**
编辑器 Scene 视图里三区不黑、打印室北端也亮；强度/颜色沿用现有房间（若墙上光斑太重，range 收到 9–10）。

---

## 任务 9：收尾验证与文档

- [ ] **步骤 1：清理**

删除 `打印室/道具/办公桌`（若任务 5 未删）；确认场景里没有残留的旧 `隔墙`/旧 `北墙`/`空柜子` Quad。

- [ ] **步骤 2：Console**

`read_console`（类型 error）为空；Play 一次（编辑器前台），由用户按验收清单手测。

- [ ] **步骤 3：文档**

- `docs/superpowers/specs/2026-11-20-level1-northrooms-adjustment-design.html`：§13 加一行「已实施（2026-11-20）」并把状态改成已落地；§11 勾掉。
- `AGENTS.md` 的「已有能力」行补充：`EmptyCabinetDrawer`（南排空柜开关抽屉、无台词）、三区新几何（11×11 / 8×9 / 29×18）、`FilingCabinet_Separated` 预制体路径。

- [ ] **步骤 4：用户手测清单**

1. 大厅开红门 → 进前室：门口看不到储藏室门；沿东侧腿走到门边（z≈11.7）左转看到门。
2. 储藏室：北排 6 个柜子，最东的柜子B 能交互；假门不可交互、无碰撞；南排 6 个抽屉都能开合、不出台词。
3. 打印室：蓝门进入播 8 号台词（说「打印室」）；复印机拿卡、柜子A 抽屉/卡片流程正常。
4. 12 号：进前室后红门自动关上并锁 60 s，60 s 后自动开。
5. 三区之间看不到缝；墙/地板无闪烁；帧率不塌。
