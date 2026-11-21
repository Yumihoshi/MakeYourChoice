# 泳池房搭建 实现计划

> **面向 AI 代理的工作者：** 逐任务实现本计划，步骤用 `- [x]` 勾选跟踪。所有场景/预制体改动一律走 Unity MCP 的 `execute_code`（编辑器内 C#）并在编辑器里保存，**不手写 YAML**。视觉验收交给用户手测。
> 本计划的几何数值全部来自 `docs/superpowers/specs/2026-11-21-pool-room-design.html`（下称"规格"），两者冲突时以规格为准并回来改本文件。

**目标：** 把 `Assets/Prefabs/Level/TestRoom.prefab` 重命名为 `泳池房.prefab`，保留 13.3 m 长廊并延长到 14.3 m，在其 +z 侧按图纸 1:1 接出室内净 25 × 32 m 的泳池层（更衣室 / 泵房 / 泳池房（池区）/ L 形走道 / 长池 / 池核），装上 62 个储物柜、两道门、7 块水面、6 个空气墙与 22 盏灯。

**架构：** 不改任何系统边界——`PortalPair` / `PortalSide` / `AutoDoor` / 交互体系全部沿用，不新增脚本（只新增一个水面 shader）。几何全部是 Cube 图元 + 一张 `瓷砖.png` 按尺寸分份的材质（ST 写进材质资产）。长方体几何用数据表驱动：附录 A 的 C# 数组 + 附录 B 的 4 个 helper，每个任务只负责一类构件。

**技术栈：** Unity 2022.3 URP（3D Renderer）、`PrefabUtility.LoadPrefabContents` 预制体内编辑、URP/Unlit 材质、内置 Cube 图元、Unity MCP（编辑器内 C# + 保存资产）。

---

## 实施约定

- **不 commit。** 本项目 git 只做查询（`status` / `log` / `diff`）。每个任务以「保存预制体 + `read_console` 无报错」为完成标志。
- 预制体编辑一律：`PrefabUtility.LoadPrefabContents(PREFAB)` → 改 → `PrefabUtility.SaveAsPrefabAsset` → `PrefabUtility.UnloadPrefabContents`。**不要直接改场景里的实例**，两处实例（Level1 口袋空间、Level2）会自动跟随预制体。
- 所有几何都在**预制体局部坐标**：长廊入口 z=0，沿 +z 到更衣室 z=14.3，泳池层 z 14.0..46.6；x 以长廊中心为 0（-22.8..2.8）。
- 墙统一高 4.35（中心 y=2.175），地板厚 0.3（中心 y=-0.15，顶面 0），天花板厚 0.3（中心 y=3.15 或 4.5）。
- 幂等：每个 builder 先按名字删掉同名旧子物体再新建，重复执行不会叠出两份。
- 视觉与手感验收由用户手测，代理不模拟输入（不用鼠标键盘跑 Play）。

## 文件结构

- 修改：`Assets/Prefabs/Level/泳池房.prefab`（原 `TestRoom.prefab`，重命名后内容全部在此）
- 新建材质：`Assets/Materials/Level/Tile_Pool_*.mat`（62 份，见任务 2 的命名规则）
- 新建：`Assets/Shaders/PoolWater.shader`（水面）
- 新建：`Assets/Materials/Level/Water_Pool.mat`、`Placeholder_Red.mat`
- 新建预制体：`Assets/Prefabs/Props/Valve.prefab`、`PumpLP.prefab`、`Wrench.prefab`
- 新建材质：`Assets/Materials/Props/Valve*.mat`、`PumpLP*.mat`、`Wrench*.mat`
- 不新建脚本、不新建程序集、不引入框架。

## 现有基线（开工前实测）

- `Assets/Prefabs/Level/TestRoom.prefab`：子物体 `地板`（1.70×0.13×13.3，中心 0,-0.07,6.65）、`左墙` / `右墙`（0.13×4.35×13.3，x=±0.79）、`天花板`（0.13 厚，中心 y=4.42）、`尽头封墙`（z 13.3..13.43）、`入口侧壁_左/右`、`入口门楣`、`返回传送面` / `返回传送相机` / `返回裁剪面`、`点光_1..3`。
- 玩家：`CharacterController` radius 0.3、height 1.8 → 通行净宽需 ≥ 0.62。
- 已有可复用：`OldMetalWardrobe.prefab`（0.49×1.80×0.45，正面 +X，双门可转）、`AutoDoor.prefab`、`DoorBox` 网格、`Door.mat`、`Wall_Lintel` 材质。
- 贴图：`Assets/Art/泳池/瓷砖.png`（64×64，8×8 块砖，缝 1 px）。

---

## 任务 1：预制体重命名与长廊延长

**文件：** `Assets/Prefabs/Level/TestRoom.prefab` → `Assets/Prefabs/Level/泳池房.prefab`

- [x] **步骤 1：重命名资产（GUID 不变，两处实例自动跟随）**

```csharp
AssetDatabase.RenameAsset("Assets/Prefabs/Level/TestRoom.prefab", "泳池房");
AssetDatabase.SaveAssets();
return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Level/泳池房.prefab") != null;
```

- [x] **步骤 2：删「尽头封墙」，把地板/两侧墙/天花板延到 z=14.3**

```csharp
var PREFAB = "Assets/Prefabs/Level/泳池房.prefab";
var root = PrefabUtility.LoadPrefabContents(PREFAB);

var end = root.transform.Find("长廊/尽头封墙");
if (end != null) Object.DestroyImmediate(end.gameObject);

// 地板：13.3 → 14.3（中心 6.65 → 7.15）
var f = root.transform.Find("长廊/地板");
f.localScale = new Vector3(1.70f, 0.13f, 14.30f);
f.localPosition = new Vector3(0f, -0.07f, 7.15f);

// 两侧墙与天花板同步延长
foreach (var n in new[] { "长廊/左墙", "长廊/右墙" }) {
    var t = root.transform.Find(n);
    t.localScale = new Vector3(0.13f, 4.35f, 14.30f);
    t.localPosition = new Vector3(t.localPosition.x, 2.175f, 7.15f);
}
var c = root.transform.Find("长廊/天花板");
c.localScale = new Vector3(1.70f, 0.13f, 14.30f);
c.localPosition = new Vector3(0f, 4.42f, 7.15f);

PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return "ok";
```

- [x] **步骤 3：验证**
`read_console` 无报错；再次 `LoadPrefabContents` 打印 `长廊` 子物体数量与 `地板.localScale.z`，预期 14.30 且没有 `尽头封墙`。
用户可见检查：Level1 里从大厅传送进长廊，走到尽头应直接进入更衣室（此时还没有更衣室，会看到空的）。

---

## 任务 2：瓷砖材质（50 份 `Tile_Pool_*`，尺寸分份 + ST）

**文件：** 新建 `Assets/Materials/Level/Tile_Pool_*.mat`

命名与平铺（`_BaseMap` = `瓷砖.png`，一张贴图 = 2 m，砖 0.25 m）：

| 用途 | 命名 | ST |
| --- | --- | --- |
| 地板 | `Tile_Pool_Floor_<宽>x<深>` | 宽/2 × 深/2 |
| 天花板 | `Tile_Pool_Ceil_<宽>x<深>` | 宽/2 × 深/2 |
| 墙 | `Tile_Pool_Wall_<长>` | 长/2 × 2.175 |
| 池壁 | `Tile_Pool_池壁_<长>` | 长/2 × 0.70 |
| 池底 | `Tile_Pool_池底_<宽>x<深>` | 宽/2 × 深/2 |
| 池沿 | `Tile_Pool_池沿_<长>` | 长/2 × 0.15 |
| 台阶 | `Tile_Pool_台阶` | 1.50 × 0.45 |

- [x] **步骤 1：确认贴图导入设置（Repeat / sRGB）**

```csharp
var texPath = "Assets/Art/泳池/瓷砖.png";
var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
ti.wrapMode = TextureWrapMode.Repeat;
ti.filterMode = FilterMode.Bilinear;
ti.mipmapEnabled = true;
ti.sRGBTexture = true;
ti.SaveAndReimport();
return ti.wrapMode.ToString();
```

- [x] **步骤 2：建材质工厂（首次调用时按需创建，重复执行幂等）**

```csharp
var TEX = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/泳池/瓷砖.png");
var _cache = new Dictionary<string, Material>();

string Fmt(float v) { return ((float)System.Math.Round(v, 2)).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture); }

Material Tile(string name, float sx, float sy) {
    string path = "Assets/Materials/Level/" + name + ".mat";
    var m = AssetDatabase.LoadAssetAtPath<Material>(path);
    if (m == null) {
        m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        AssetDatabase.CreateAsset(m, path);
    }
    m.SetTexture("_BaseMap", TEX);
    m.SetTextureScale("_BaseMap", new Vector2(sx, sy));
    m.SetFloat("_Smoothness", 0f);
    EditorUtility.SetDirty(m);
    return m;
}

Material FloorMat(float w, float d) { return Tile("Tile_Pool_Floor_" + Fmt(w) + "x" + Fmt(d), w / 2f, d / 2f); }
Material CeilMat(float w, float d)  { return Tile("Tile_Pool_Ceil_" + Fmt(w) + "x" + Fmt(d), w / 2f, d / 2f); }
Material WallMat(float len)         { return Tile("Tile_Pool_Wall_" + Fmt(len), len / 2f, 2.175f); }
Material BasinMat(float len)        { return Tile("Tile_Pool_池壁_" + Fmt(len), len / 2f, 0.70f); }
Material BottomMat(float w, float d){ return Tile("Tile_Pool_池底_" + Fmt(w) + "x" + Fmt(d), w / 2f, d / 2f); }
Material CopingMat(float len)       { return Tile("Tile_Pool_池沿_" + Fmt(len), len / 2f, 0.15f); }
Material StepMat()                  { return Tile("Tile_Pool_台阶", 1.5f, 0.45f); }

AssetDatabase.SaveAssets();
return AssetDatabase.FindAssets("t:Material Tile_Pool_", new[] { "Assets/Materials/Level" }).Length;
```

- [x] **步骤 3：验证**
上面最后一行返回 0（此时还没建任何一份，材质在后续任务里按需生成）。任务 13 结束时再回来统计，预期 **50 份 `Tile_Pool_*`**（另有 `Wall_Pool_Corridor`、`Water_Pool`、`Placeholder_Red` 三份，合计 53 份资产）。
用户可见检查：Project 里选中任意 `Tile_Pool_Floor_25x5.mat`，`Base Map` Tiling = 12.5 × 2.5。

---

## 任务 3：地板 16 块（含池坑洞口）

**文件：** `泳池房.prefab` → `建筑/地板_01..`

- [x] **步骤 1：按附录 A 的 `FLOORS` 建板，跳过「长廊」（已在任务 1 延长）**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var arch = Group(root, "建筑");
int n = 0;
foreach (var row in FLOORS) {
    string room = row[0] as string;
    var v = row[1] as float[];
    if (room == "长廊") continue;
    float x0 = v[0], z0 = v[1], x1 = v[2], z1 = v[3];
    float w = x1 - x0, d = z1 - z0;
    n++;
    var go = Box(arch, "地板_" + n.ToString("00"),
                 new Vector3((x0 + x1) / 2f, -0.15f, (z0 + z1) / 2f),
                 new Vector3(w, 0.3f, d), FloorMat(w, d));
    if (w < 0.5f || d < 0.5f) Debug.Log("薄板 " + go.name + " " + w + "x" + d);
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return n;   // 预期 15
```

- [x] **步骤 2：验证覆盖（0 个未覆盖/重叠采样点）**

```csharp
// 0.1 m 采样：每个非洞区采样点必须恰好被 1 块地板覆盖
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var plates = new List<Bounds>();
foreach (Transform t in Group(root, "建筑")) {
    if (!t.name.StartsWith("地板_")) continue;
    plates.Add(new Bounds(t.localPosition, t.localScale));   // 预制体内容坐标系即局部系
}
int bad = 0;
for (float x = -22.5f; x <= 2.5f; x += 0.1f)
for (float z = 14.3f; z <= 46.3f; z += 0.1f) {
    int hit = 0;
    foreach (var b in plates) if (Mathf.Abs(x - b.center.x) < b.extents.x && Mathf.Abs(z - b.center.z) < b.extents.z) hit++;
    bool inPool = InsideAnyWater(x, z);      // 用附录 A 的 WATER 判洞
    if ((hit == 0 && !inPool) || hit > 1) bad++;
}
PrefabUtility.UnloadPrefabContents(root);
return bad;   // 预期 0
```

- [x] **步骤 3：验证**
`bad == 0`；`read_console` 无报错。

---

## 任务 4：10 段墙（长廊两侧墙不重建）

**文件：** `泳池房.prefab` → `建筑/墙_*`

- [x] **步骤 1：按附录 A 的 `WALLS` 建墙，跳过两条长廊墙**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var arch = Group(root, "建筑");
int n = 0;
foreach (var row in WALLS) {
    string name = row[0] as string;
    if (name.Contains("长廊")) continue;      // 任务 1 已延长
    char axis = (char)row[1];
    float a = (float)row[2], b = (float)row[3], c = (float)row[4];
    float t = (float)row[5], h = (float)row[6];
    float len = Mathf.Abs(b - a);
    n++;
    if (axis == 'x')
        Box(arch, name, new Vector3((a + b) / 2f, h / 2f, c), new Vector3(len, h, t), WallMat(len));
    else
        Box(arch, name, new Vector3(c, h / 2f, (a + b) / 2f), new Vector3(t, h, len), WallMat(len));
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return n;   // 预期 10
```

- [x] **步骤 2：验证门洞净宽**

```csharp
// 更衣室北墙两个门洞：暗门 1.0（x -21.0..-20.0）、更衣室门 1.5（x -3.25..-1.75）
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var tB = root.transform.Find("建筑/墙_更衣室北_B");
var tA = root.transform.Find("建筑/墙_更衣室北_A");
var tC = root.transform.Find("建筑/墙_更衣室北_C");
float gapLeft  = tB.localPosition.x - tB.localScale.x / 2f - (tA.localPosition.x + tA.localScale.x / 2f);
float gapRight = tC.localPosition.x - tC.localScale.x / 2f - (tB.localPosition.x + tB.localScale.x / 2f);
PrefabUtility.UnloadPrefabContents(root);
return "暗门洞 " + gapLeft.ToString("0.00") + " 更衣室门洞 " + gapRight.ToString("0.00");  // 预期 1.00 / 1.50
```

- [x] **步骤 3：验证**
两个洞宽为 1.00 / 1.50；墙高 4.35；`read_console` 无报错。

---

## 任务 5：6 块天花板

**文件：** `泳池房.prefab` → `建筑/天花板_*`

- [x] **步骤 1：按附录 A 的 `CEILINGS` 建板**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var arch = Group(root, "建筑");
foreach (var row in CEILINGS) {
    string room = row[0] as string;
    var v = row[1] as float[];
    float y = (float)row[2];
    float w = v[2] - v[0], d = v[3] - v[1];
    Box(arch, "天花板_" + room, new Vector3((v[0] + v[2]) / 2f, y, (v[1] + v[3]) / 2f),
        new Vector3(w, 0.3f, d), CeilMat(w, d));
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return "ok";
```

- [x] **步骤 2：验证**
泵房与更衣室吊顶 y=3.15、其余 4.5；从泳池房抬头看到 4.5 一整片，走进泵房吊顶压到 3.15。

---

## 任务 6：池体（池壁 20 / 池底 7 / 池沿 20 / 台阶 3）

**文件：** `泳池房.prefab` → `建筑/池坑_*`、`建筑/台阶_*`

- [x] **步骤 1：池壁（y -1.5..-0.1，只在水下）**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var arch = Group(root, "建筑");
int i = 0;
foreach (var r in POOL_WALLS) {
    float w = r[2] - r[0], d = r[3] - r[1];
    float len = Mathf.Max(w, d);
    i++;
    Box(arch, "池坑_池壁_" + i.ToString("00"),
        new Vector3((r[0] + r[2]) / 2f, -0.8f, (r[1] + r[3]) / 2f),
        new Vector3(w, 1.4f, d), BasinMat(len));
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return i;   // 预期 20
```

- [x] **步骤 2：池沿（同一批矩形，y -0.1..0，顶面与地面齐平）**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var arch = Group(root, "建筑");
int i = 0;
foreach (var r in POOL_WALLS) {
    float w = r[2] - r[0], d = r[3] - r[1];
    i++;
    Box(arch, "池沿_" + i.ToString("00"),
        new Vector3((r[0] + r[2]) / 2f, -0.05f, (r[1] + r[3]) / 2f),
        new Vector3(w, 0.1f, d), CopingMat(Mathf.Max(w, d)));
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return i;   // 预期 20
```

- [x] **步骤 3：池底（顶面 -1.5）**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var arch = Group(root, "建筑");
int i = 0;
foreach (var row in POOL_FLOORS) {
    var r = row[1] as float[];
    float w = r[2] - r[0], d = r[3] - r[1];
    i++;
    Box(arch, "池坑_池底_" + i.ToString("00"),
        new Vector3((r[0] + r[2]) / 2f, -1.65f, (r[1] + r[3]) / 2f),
        new Vector3(w, 0.3f, d), BottomMat(w, d));
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return i;   // 预期 7
```

- [x] **步骤 4：3 级台阶（顶面 -0.375 / -0.75 / -1.125）**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var arch = Group(root, "建筑");
foreach (var row in STEPS) {
    string name = row[0] as string;
    var v = row[1] as float[];
    float top = (float)row[2];
    float h = top + 1.5f;                     // 从池底 -1.5 起
    Box(arch, name, new Vector3((v[0] + v[2]) / 2f, top - h / 2f, (v[1] + v[3]) / 2f),
        new Vector3(v[2] - v[0], h, v[3] - v[1]), StepMat());
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return "ok";
```

- [x] **步骤 5：验证竖向不共面**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var arch = root.transform.Find("建筑");
var wall01 = arch.Find("池坑_池壁_01").GetComponent<Renderer>().bounds;
int clash = 0;
foreach (Transform t in arch) {
    if (!t.name.StartsWith("地板_")) continue;
    if (wall01.Intersects(t.GetComponent<Renderer>().bounds)) clash++;
}
PrefabUtility.UnloadPrefabContents(root);
return "池壁与地板相交块数 " + clash;   // 预期 0
```

---

## 任务 7：空气墙 6 个（挡住玩家进水）

**文件：** `泳池房.prefab` → `建筑/空气墙_*`

- [x] **步骤 1：建无渲染器的 BoxCollider**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var arch = Group(root, "建筑");
foreach (var row in AIRWALLS) {
    string name = row[0] as string;
    var v = row[1] as float[];
    var old = arch.Find(name);
    if (old != null) Object.DestroyImmediate(old.gameObject);
    var go = new GameObject(name);
    go.transform.SetParent(arch, false);
    go.transform.localPosition = new Vector3((v[0] + v[2]) / 2f, 0.7f, (v[1] + v[3]) / 2f);
    var bc = go.AddComponent<BoxCollider>();
    bc.size = new Vector3(v[2] - v[0], 1.8f, v[3] - v[1]);   // y -0.2..1.6
    bc.isTrigger = false;
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return "ok";
```

- [x] **步骤 2：验证**
6 个对象都只有 `BoxCollider`、没有 `MeshRenderer`；`isTrigger == false`；尺寸与附录 A `AIRWALLS` 一致。
用户可见检查（进 Play 后）：走到池边会被挡住、沿池边滑动，进不了水面。

---

## 任务 8：三个新道具预制体（Valve / PumpLP / Wrench）

**文件：** 新建 `Assets/Prefabs/Props/{Valve,PumpLP,Wrench}.prefab` 与 `Assets/Materials/Props/*.mat`

- [x] **步骤 1：按 `.agent-guides/props.md` 烘焙源 FBX（100 倍缩放那两个）**
源文件与目标尺寸：`Art/Models/轮阀/source/valve.fbx` → 阀轮直径 **0.90 m**（根 scale 100）；`Art/Models/抽水泵/source/PumpLP.fbx` → 泵体 0.65×0.69×0.63（根 scale 100）；`Art/Models/扳手/source/key.fbx` → 长 0.33 m（根 scale 1，只需换材质）。
顶点烘焙（位置 `t.localToWorldMatrix.MultiplyPoint3x4(v)`、法线 `t.rotation * n`、最后 `RecalculateBounds/Tangents`），结果存到 `Art/Models/<名>/processed/`。
执行时注意：`execute_code` 的 `safety_checks` 会拦 `AssetDatabase.DeleteAsset`，删旧资产用 `AssetDatabase.MoveAssetToTrash`。

- [x] **步骤 2：建材质（URP/Lit + 打包遮罩）**
金属件沿用 `OldMetalWardrobe_Body.mat` 的做法（R=metallic、A=255−roughness，打包脚本 `AITemp/pack_masks.py`）；每份材质 `AssetDatabase.CreateAsset` 到 `Assets/Materials/Props/`。

- [x] **步骤 3：建预制体并核对包围盒**

```csharp
// 逐个：加载 processed 网格 → 建空根 + MeshFilter/MeshRenderer → 设材质 → SaveAsPrefabAsset
// 校验（世界尺寸）：Valve 0.90×0.90、PumpLP 0.65×0.69×0.63、Wrench 0.33×0.10
var pf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/Valve.prefab");
var r = pf.GetComponentInChildren<MeshRenderer>();
return r.bounds.size.ToString("F2");   // 每个预制体都打印一次
```

- [x] **步骤 4：验证**
三个预制体的包围盒与上面一致；Console 无 "100 倍缩放残留"（在场景里看不应高出人）。

---

## 任务 9：道具摆放（储物柜 62 + 真模型 4 + 占位 5）

**文件：** `泳池房.prefab` → `道具/*`

- [x] **步骤 1：62 个储物柜（OldMetalWardrobe 实例，背贴墙）**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var prop = Group(root, "道具");
var shelf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/OldMetalWardrobe.prefab");

// 段: (x0, x1, 数量, z中心, 是否转180)
float northZ = 19.075f, southZ = 14.525f;
var segs = new (float, float, int, float, bool)[] {
    (-21.5f, -14.5f, 14, northZ, true),
    (-13.5f,  -6.5f, 14, northZ, true),
    (-21.5f, -14.5f, 14, southZ, false),
    (-13.5f,  -6.5f, 14, southZ, false),
    ( -5.5f,  -1.5f,  8, southZ, false),
};
int idx = 0, skipped = 0;
foreach (var sgm in segs) {
    for (int k = 0; k < sgm.Item3; k++) {
        float x = sgm.Item1 + 0.5f + k;                      // 0.5 间距
        // 正对暗门洞 x -21.0..-20.0 的位置跳过（北排 2 个）
        if (sgm.Item5 && Mathf.Min(x + 0.245f, -20.0f) - Mathf.Max(x - 0.245f, -21.0f) > 0.1f) {
            skipped++; continue;
        }
        idx++;
        string name = "储物柜_" + idx.ToString("00");
        var old = prop.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(shelf, prop);
        go.name = name;
        go.transform.localPosition = new Vector3(x, 0f, sgm.Item4);
        go.transform.localRotation = Quaternion.Euler(0f, sgm.Item5 ? 180f : 0f, 0f);
    }
}
Debug.Log("柜子 " + idx + " 个，跳过暗门 " + skipped + " 个");   // 预期 62 / 2
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return "ok";
```

- [x] **步骤 2：4 个真模型道具（位置与朝向按附录 A `REAL_PROPS`）**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var prop = Group(root, "道具");
foreach (var row in REAL_PROPS) {
    string name = row[0] as string;
    float x = (float)row[1], z = (float)row[2];
    string pfPath = row[5] as string;
    float target = (float)row[3];              // 目标宽度（米）
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/" + pfPath);
    if (prefab == null) throw new Exception("缺预制体 " + pfPath);
    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, prop);
    go.name = name;
    go.transform.localPosition = new Vector3(x, 0f, z);
    go.transform.localRotation = Quaternion.identity;      // 四件都是自然朝向：阀轮竖立、泵立着、扳手平躺

    // 先按 x 缩放打到目标宽，再自动落地（bounds.min.y 抬到 0）
    var rnd = go.GetComponentInChildren<MeshRenderer>();
    float k = target / rnd.bounds.size.x;
    go.transform.localScale = new Vector3(k, k, k);
    rnd = go.GetComponentInChildren<MeshRenderer>();        // 缩放后重取包围盒
    float lift = -rnd.bounds.min.y;
    if (name == "抽水泵") lift += 0f;                       // 泵站在干区平台（平台顶面 -1.5 之上另行垫高见规格）
    go.transform.localPosition = new Vector3(x, lift, z);
    Debug.Log(name + " 宽 " + rnd.bounds.size.x.ToString("F2") + " 落地 y=" + lift.ToString("F2"));
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return "ok";
```

- [x] **步骤 3：5 个纯红占位（地漏 / 气垫 ×2 / 管道 ×2）**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var prop = Group(root, "道具");
Material red = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level/Placeholder_Red.mat");
if (red == null) {
    red = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    red.color = Color.red;
    AssetDatabase.CreateAsset(red, "Assets/Materials/Level/Placeholder_Red.mat");
}
foreach (var row in PROPS) {
    string name = row[0] as string;
    float x = (float)row[1], z = (float)row[2];
    float w = (float)row[3], d = (float)row[4];
    string note = row[5] as string;
    float y = name.Contains("气垫") ? -0.20f : -1.45f;      // 气垫浮水面、地漏贴池底
    if (name.Contains("管道")) y = 0.2f;
    Box(prop, name, new Vector3(x, y, z), new Vector3(w, 0.05f, d), red);
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return "ok";
```

- [x] **步骤 4：验证**
`道具` 下对象数 = 62 + 4 + 5 = **71**；储物柜北排正面朝南、南排朝北，正对暗门处留 1.0 m 空档；Console 无报错。

---

## 任务 10：两道门（更衣室门 1.5 / 暗门 1.0 假柜）

**文件：** `泳池房.prefab` → `道具/门_更衣室`、`道具/门_暗门`

- [x] **步骤 1：更衣室门（AutoDoor 旋转门，洞 x -3.25..-1.75，z=19.45）**

`AutoDoor.prefab` 本身就是 1.5 m 洞的铰链门（门板 1.54×2.44×0.08、`DoorBox` 网格 + `Door.mat`、`铰链` 子节点、`AutoDoor` 已设 `mode=Hinge` / `autoClose=false` / 未锁定），直接实例化摆位即可：

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var prop = Group(root, "道具");
var doorPf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Level/AutoDoor.prefab");

var old = prop.Find("门_更衣室");
if (old != null) Object.DestroyImmediate(old.gameObject);
var d = (GameObject)PrefabUtility.InstantiatePrefab(doorPf, prop);
d.name = "门_更衣室";
d.transform.localPosition = new Vector3(-2.5f, 0f, 19.45f);
d.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // 门朝走道（-z）方向开

// 门楣：与墙同厚，盖在门洞上方（洞高 2.4，墙高 4.35）
var lintel = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wall_Lintel.mat");
var lm = prop.Find("门_更衣室_门楣");
if (lm != null) Object.DestroyImmediate(lm.gameObject);
Box(prop, "门_更衣室_门楣", new Vector3(-2.5f, (2.4f + 4.35f) / 2f, 19.45f),
    new Vector3(1.9f, 4.35f - 2.4f, 0.30f), lintel);

PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return "ok";
```

- [x] **步骤 2：暗门（OldMetalWardrobe 假柜，X 缩放 2.04）**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var prop = Group(root, "道具");
var shelf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/OldMetalWardrobe.prefab");
var go = (GameObject)PrefabUtility.InstantiatePrefab(shelf, prop);
go.name = "门_暗门";
go.transform.localPosition = new Vector3(-20.5f, 0f, 19.45f);
go.transform.localScale = new Vector3(2.04f, 1f, 1f);      // 0.49 → 1.00 m，柜内净宽 0.98
// 关键：铰链 localPosition.x 要跟着放大，否则门绕柜子中心转
go.transform.Find("Body/Hinge_FrontDoor").localPosition = new Vector3(0.459f, 1.121f, 0.206f);
go.transform.Find("Body/Hinge_BackDoor").localPosition  = new Vector3(-0.459f, 0.900f, 0.225f);
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return "ok";
```

- [x] **步骤 3：验证**
更衣室门洞净宽 1.5、门板不穿透墙；暗门占满 1.0 m 洞、柜门开合不绕中心转；玩家胶囊（0.6 宽）能从柜体空腔穿过（净宽 0.98）。
用户手测：进更衣室开门 → 走到北墙西端 → 打开假柜 → 穿过去到泵房。

---

## 任务 11：水面 shader 与 7 块水面

**文件：** 新建 `Assets/Shaders/PoolWater.shader`、`Assets/Materials/Level/Water_Pool.mat`；`泳池房.prefab` → `道具/水面_*`

- [x] **步骤 1：写 shader（全文见附录 C）**

用 `write` 工具把**附录 C 的全文**保存成 `Assets/Shaders/PoolWater.shader`（逐字照抄，行首不要加缩进），然后让 Unity 导入并确认 shader 能编译：

```csharp
AssetDatabase.ImportAsset("Assets/Shaders/PoolWater.shader", ImportAssetOptions.ForceUpdate);
var sh = Shader.Find("Pool/PoolWater");
if (sh == null) throw new Exception("shader 没编译通过，先看 Console");
return sh.name;
```

- [x] **步骤 2：建水面材质**

```csharp
var sh = Shader.Find("Pool/PoolWater");
var m = new Material(sh);
m.SetColor("_Color", new Color32(0x2E, 0x75, 0xB6, 0x8C));   // #2E75B6, alpha 0.55
m.SetFloat("_WaveSpeed", 0.35f);
m.SetFloat("_WaveScale", 1.6f);
m.SetFloat("_Fresnel", 0.35f);
m.SetFloat("_DepthFade", 1.0f);
AssetDatabase.CreateAsset(m, "Assets/Materials/Level/Water_Pool.mat");
AssetDatabase.SaveAssets();
return "ok";
```

- [x] **步骤 3：7 块水面 Quad（长池 3 + 池核 4，y=-0.25）**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var prop = Group(root, "道具");
var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level/Water_Pool.mat");
int i = 0;
foreach (var row in WATER) {
    var r = row as float[];
    i++;
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
    go.name = "水面_" + i.ToString("00");
    go.transform.SetParent(prop, false);
    go.transform.localPosition = new Vector3((r[0] + r[2]) / 2f, -0.25f, (r[1] + r[3]) / 2f);
    go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);         // 面朝上
    go.transform.localScale = new Vector3(r[2] - r[0], r[3] - r[1], 1f);
    go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    Object.DestroyImmediate(go.GetComponent<MeshCollider>());            // 水面不加碰撞
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return i;   // 预期 7
```

- [x] **步骤 4：验证**
7 块 Quad、无碰撞体、y=-0.25；`read_console` 无 shader 编译错误。
用户手测：水面半透明、能看到池底的瓷砖，波纹缓慢流动。

---

## 任务 12：22 盏灯

**文件：** `泳池房.prefab` → `灯光/*`

- [x] **步骤 1：按附录 A 的 `LIGHTS` 建点光**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var grp = Group(root, "灯光");
Color32 Parse(string hex) {
    hex = hex.TrimStart('#');
    return new Color32(System.Convert.ToByte(hex.Substring(0,2),16),
                       System.Convert.ToByte(hex.Substring(2,2),16),
                       System.Convert.ToByte(hex.Substring(4,2),16), 255);
}
foreach (var row in LIGHTS) {
    string name = row[0] as string;
    float x = (float)row[1], z = (float)row[2], y = (float)row[3];
    Color32 col = Parse(row[4] as string);
    var old = grp.Find(name);
    if (old != null) Object.DestroyImmediate(old.gameObject);
    var go = new GameObject(name);
    go.transform.SetParent(grp, false);
    go.transform.localPosition = new Vector3(x, y, z);
    var L = go.AddComponent<Light>();
    L.type = LightType.Point;
    L.color = col;
    L.range = name.Contains("水面") ? 8f : (name.Contains("泵房") || name.Contains("更衣室") ? 7f : 11f);
    L.intensity = name.Contains("水面") ? 0.6f : (name.Contains("泵房") || name.Contains("更衣室") ? 1.2f : 1.45f);
    L.shadows = LightShadows.None;
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return "ok";
```

- [x] **步骤 2：验证**
22 盏、`shadows=None`；长廊原有 `点光_1..3` 原样保留（共 25 盏点光）。

---

## 任务 13：层级整理、锚点图标、保存

**文件：** `泳池房.prefab` 全量

- [x] **步骤 1：补齐容器并给空锚点设图标**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
Group(root, "长廊"); Group(root, "建筑"); Group(root, "道具"); Group(root, "灯光");
Group(root, "触发"); Group(root, "剧情锚点");

var anchor = Group(root, "剧情锚点");
(string, Vector3)[] pts = {
    ("交互点_主阀", new Vector3(-18.5f, 1.2f, 26.3f)),
    ("交互点_排水", new Vector3(-5.0f, 0.0f, 38.8f)),
    ("交互点_暗门", new Vector3(-20.5f, 1.2f, 19.45f)),
};
foreach (var (n, p) in pts) {
    var old = anchor.Find(n);
    if (old != null) Object.DestroyImmediate(old.gameObject);
    var go = new GameObject(n);
    go.transform.SetParent(anchor, false);
    go.transform.localPosition = p;
    var tex = EditorGUIUtility.IconContent("sv_icon_dot11_pix16_gizmo").image as Texture2D;
    EditorGUIUtility.SetIconForObject(go, tex);
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return "ok";
```

- [x] **步骤 2：全量统计**

```csharp
var root = PrefabUtility.LoadPrefabContents(PREFAB);
int Count(string path) { var t = root.transform.Find(path); return t == null ? -1 : t.childCount; }
var report = "长廊 " + Count("长廊") + " / 建筑 " + Count("建筑") + " / 道具 " + Count("道具")
           + " / 灯光 " + Count("灯光") + " / 剧情锚点 " + Count("剧情锚点");
PrefabUtility.UnloadPrefabContents(root);
return report + " / 材质 " + AssetDatabase.FindAssets("t:Material Tile_Pool_", new[] { "Assets/Materials/Level" }).Length;
```

预期：建筑 15 地板 + 12 墙（含长廊 2 条原有）+ 6 天花板 + 20 池壁 + 20 池沿 + 7 池底 + 3 台阶 + 6 空气墙 ≈ 89；道具 62 柜 + 4 真模型 + 5 占位 + 2 门 + 7 水面 = 80；灯光 22 + 长廊 3；材质 **50 份 `Tile_Pool_*`**（含台阶 1 份，尺寸去重后）。

- [x] **步骤 3：保存并查 Console**
`AssetDatabase.SaveAssets()`；`read_console` 只看 error/warning，预期 0 error。

---

## 任务 14：验收与交接

- [ ] **步骤 1：静态检查**
上面各任务的验证返回值全部符合预期；`mcp__unityMCP__read_console` 无 error。

- [ ] **步骤 2：交给用户手测**（代理不代跑）
请用户按顺序在 Level1 里 Play：
1. 大厅拿门禁卡 → 开安全门 → 传送到长廊，走到尽头进更衣室。
2. 更衣室：两排储物柜（北 26 / 南 36）、北墙正中 1 m 空档是假柜，打开后穿到泵房。
3. 泵房：两条水渠、主阀/轮/泵是真实模型，渠里水面 -0.1。
4. 从更衣室门出来 → 走道 16×4 → 北拐 3×5 → 泳池房。
5. 池边：长池哑铃形、水面低于地面 0.25、池沿与地面齐平、**走不进水里**（空气墙挡住）。
6. 抬头 / 进泵房看吊顶高度差（4.5 vs 3.15）。
7. 期望 Console 无报错；帧率不低于大厅。

- [ ] **步骤 3：按反馈登记问题**
用户报的问题写进本文件顶部「实施状态」，修完再验证。

---

## 附录 A：几何数据（C# 直接粘贴）

> 全部为预制体局部坐标，单位米。与规格第 1/3/4 节一一对应；改动请同步改 `AITemp/pool/gen_spec.py` 并重新导出。

```csharp
// ROOMS：名称, x0, z0, x1, z1, 净高, 吊顶中心y
var ROOMS = new object[][] {
    new object[]{ "长廊",               new[]{ -0.85f,  0.00f,  0.85f, 14.30f, 4.35f,  4.42f } },
    new object[]{ "更衣室",             new[]{-22.50f, 14.30f,  2.50f, 19.30f, 3.00f,  3.15f } },
    new object[]{ "泵房",               new[]{-22.50f, 19.30f,-13.50f, 28.30f, 3.00f,  3.15f } },
    new object[]{ "走道南段",           new[]{-13.50f, 19.30f,  2.50f, 23.30f, 4.35f,  4.50f } },
    new object[]{ "走道北段",           new[]{-13.50f, 23.30f,-10.50f, 28.30f, 4.35f,  4.50f } },
    new object[]{ "泳池房（池区）",     new[]{-22.50f, 28.30f,  2.50f, 46.30f, 4.35f,  4.50f } },
    new object[]{ "泳池房 池核南延段",  new[]{-10.50f, 23.30f,  0.50f, 28.30f, 4.35f,  4.50f } },
};

// WALLS：名称, 轴, a, b, 另一轴坐标, 厚, 高（轴 x = 沿 x 延伸；轴 z = 沿 z 延伸）
var WALLS = new object[][] {
    new object[]{ "墙_北外墙",       'x',-22.80f,  2.80f, 46.45f, 0.30f, 4.35f },
    new object[]{ "墙_西外墙",       'z', 14.00f, 46.60f,-22.65f, 0.30f, 4.35f },
    new object[]{ "墙_东外墙",       'z', 14.00f, 46.60f,  2.65f, 0.30f, 4.35f },
    new object[]{ "墙_更衣室南_西段", 'x',-22.80f, -0.72f, 14.15f, 0.30f, 4.35f },
    new object[]{ "墙_更衣室南_东段", 'x',  0.72f,  2.80f, 14.15f, 0.30f, 4.35f },
    new object[]{ "墙_泵房北",       'x',-22.80f,-13.20f, 28.45f, 0.30f, 4.35f },
    new object[]{ "墙_泵房东",       'z', 19.30f, 28.60f,-13.35f, 0.30f, 4.35f },
    new object[]{ "墙_更衣室北_A",   'x',-22.80f,-21.00f, 19.45f, 0.30f, 4.35f },
    new object[]{ "墙_更衣室北_B",   'x',-20.00f, -3.25f, 19.45f, 0.30f, 4.35f },
    new object[]{ "墙_更衣室北_C",   'x', -1.75f,  2.80f, 19.45f, 0.30f, 4.35f },
    ("墙_长廊左",       'z',  0.00f, 14.30f,  0.79f, 0.13f, 4.35f),   // 已有，任务 1 延长
    new object[]{ "墙_长廊右",       'z',  0.00f, 14.30f, -0.79f, 0.13f, 4.35f },
};

// FLOORS：房间, x0, z0, x1, z1（16 块；「长廊」那块是任务 1 延长的原有地板）
var FLOORS = new object[][] {
    new object[]{ "长廊",             new[]{ -0.85f,  0.00f,  0.85f, 14.30f } },
    new object[]{ "更衣室",           new[]{-22.50f, 14.30f,  2.50f, 19.30f } },
    new object[]{ "泵房",             new[]{-22.50f, 19.30f,-13.50f, 20.30f } },
    new object[]{ "泵房",             new[]{-21.50f, 20.30f,-14.50f, 28.30f } },
    new object[]{ "走道南段",         new[]{-13.50f, 23.00f,-10.80f, 23.30f } },
    new object[]{ "走道南段",         new[]{-13.50f, 19.30f,  2.50f, 23.00f } },
    new object[]{ "走道南段",         new[]{  0.80f, 23.00f,  2.50f, 23.30f } },
    new object[]{ "走道北段",         new[]{-13.50f, 23.30f,-10.80f, 28.30f } },
    new object[]{ "泳池房（池区）",   new[]{-22.50f, 35.60f,-18.80f, 40.00f } },
    new object[]{ "泳池房（池区）",   new[]{-22.50f, 28.30f,-10.80f, 31.00f } },
    new object[]{ "泳池房（池区）",   new[]{-22.50f, 44.60f,  2.50f, 46.30f } },
    new object[]{ "泳池房（池区）",   new[]{-16.20f, 35.60f,-10.80f, 40.00f } },
    new object[]{ "泳池房（池区）",   new[]{-14.20f, 31.00f,-10.80f, 35.60f } },
    new object[]{ "泳池房（池区）",   new[]{-14.20f, 40.00f,-10.80f, 43.60f } },
    new object[]{ "泳池房（池区）",   new[]{-14.20f, 43.60f,  2.50f, 44.60f } },
    new object[]{ "泳池房（池区）",   new[]{  0.80f, 28.30f,  2.50f, 43.60f } },
};

// CEILINGS：房间, x0, z0, x1, z1, 中心 y
var CEILINGS = new object[][] {
    new object[]{ "更衣室",           new[]{-22.50f, 14.30f,  2.50f, 19.30f }, 3.15f },
    new object[]{ "泵房",             new[]{-22.50f, 19.30f,-13.50f, 28.30f }, 3.15f },
    new object[]{ "走道南段",         new[]{-13.50f, 19.30f,  2.50f, 23.30f }, 4.50f },
    new object[]{ "走道北段",         new[]{-13.50f, 23.30f,-10.50f, 28.30f }, 4.50f },
    new object[]{ "泳池房（池区）",   new[]{-22.50f, 28.30f,  2.50f, 46.30f }, 4.50f },
    new object[]{ "泳池房 池核南延段", new[]{-10.50f, 23.30f,  0.50f, 28.30f }, 4.50f },
};

// POOL_WALLS：池壁（y -1.5..-0.1）与池沿（y -0.1..0）共用的 20 个矩形
var POOL_WALLS = new float[][] {
    // 长池哑铃形外轮廓 12 段
    new[]{-22.50f, 44.30f,-14.50f, 44.60f},
    new[]{-14.50f, 40.30f,-14.20f, 44.30f},
    new[]{-16.50f, 40.00f,-14.50f, 40.30f},
    new[]{-16.50f, 35.30f,-16.20f, 40.30f},
    new[]{-16.50f, 35.30f,-14.50f, 35.60f},
    new[]{-14.50f, 31.30f,-14.20f, 35.30f},
    new[]{-22.50f, 31.00f,-14.50f, 31.30f},
    new[]{-22.80f, 31.30f,-22.50f, 35.30f},
    new[]{-22.50f, 35.30f,-18.50f, 35.60f},
    new[]{-18.80f, 35.30f,-18.50f, 40.30f},
    new[]{-22.50f, 40.00f,-18.50f, 40.30f},
    new[]{-22.80f, 40.30f,-22.50f, 44.30f},
    // 池核矩形内壁 4 段
    new[]{-10.50f, 23.00f,  0.50f, 23.30f},
    new[]{-10.50f, 43.30f,  0.50f, 43.60f},
    new[]{-10.80f, 23.30f,-10.50f, 43.30f},
    new[]{  0.50f, 23.30f,  0.80f, 43.30f},
    // 干区平台四边
    new[]{ -7.80f, 26.00f, -2.20f, 26.30f},
    new[]{ -7.80f, 30.30f, -2.20f, 30.60f},
    new[]{ -7.80f, 26.30f, -7.50f, 30.30f},
    new[]{ -2.50f, 26.30f, -2.20f, 30.30f},
};

// POOL_FLOORS：池底（顶面 -1.5）
var POOL_FLOORS = new object[][] {
    new object[]{ "长池_北叶", new[]{-22.50f, 40.30f,-14.50f, 44.30f} },
    new object[]{ "长池_颈",   new[]{-18.50f, 35.30f,-16.50f, 40.30f} },
    new object[]{ "长池_南叶", new[]{-22.50f, 31.30f,-14.50f, 35.30f} },
    new object[]{ "池核",      new[]{-10.50f, 26.30f, -7.50f, 30.30f} },
    new object[]{ "池核",      new[]{-10.50f, 23.30f,  0.50f, 26.30f} },
    new object[]{ "池核",      new[]{-10.50f, 30.30f,  0.50f, 43.30f} },
    new object[]{ "池核",      new[]{ -2.50f, 26.30f,  0.50f, 30.30f} },
};

// STEPS：名称, x0, z0, x1, z1, 踏步顶面 y
var STEPS = new object[][] {
    new object[]{ "台阶_1", new[]{-10.50f, 23.30f, -7.50f, 24.20f}, -0.375f },
    new object[]{ "台阶_2", new[]{-10.50f, 24.20f, -7.50f, 25.10f}, -0.750f },
    new object[]{ "台阶_3", new[]{-10.50f, 25.10f, -7.50f, 26.00f}, -1.125f },
};

// AIRWALLS：名称, x0, z0, x1, z1（高 1.8，中心 y=0.7）
var AIRWALLS = new object[][] {
    new object[]{ "空气墙_长池北叶", new[]{-22.50f, 40.30f,-14.50f, 44.30f} },
    new object[]{ "空气墙_长池颈",   new[]{-18.50f, 35.30f,-16.50f, 40.30f} },
    new object[]{ "空气墙_长池南叶", new[]{-22.50f, 31.30f,-14.50f, 35.30f} },
    new object[]{ "空气墙_池核",     new[]{-10.50f, 23.30f,  0.50f, 43.30f} },
    new object[]{ "空气墙_水渠_西",  new[]{-22.50f, 20.30f,-21.50f, 28.30f} },
    new object[]{ "空气墙_水渠_东",  new[]{-14.50f, 20.30f,-13.50f, 28.30f} },
};

// WATER：7 块水面（长池 3 + 池核 4），y=-0.25
var WATER = new float[][] {
    new[]{-22.50f, 40.30f,-14.50f, 44.30f},
    new[]{-18.50f, 35.30f,-16.50f, 40.30f},
    new[]{-22.50f, 31.30f,-14.50f, 35.30f},
    new[]{-10.50f, 26.30f, -7.50f, 30.30f},
    new[]{-10.50f, 23.30f,  0.50f, 26.30f},
    new[]{-10.50f, 30.30f,  0.50f, 43.30f},
    new[]{ -2.50f, 26.30f,  0.50f, 30.30f},
};

// LIGHTS：名称, x, z, y, 颜色（range/intensity 按名字决定，见任务 12）
var LIGHTS = new object[][] {
    new object[]{ "点光_泳池房_01",-20.0f,32.0f,4.2f,"#ffe2b0" },
    new object[]{ "点光_泳池房_02",-20.0f,37.5f,4.2f,"#ffe2b0" },
    new object[]{ "点光_泳池房_03",-20.0f,43.0f,4.2f,"#ffe2b0" },
    new object[]{ "点光_泳池房_04",-14.0f,32.0f,4.2f,"#ffe2b0" },
    new object[]{ "点光_泳池房_05",-14.0f,37.5f,4.2f,"#ffe2b0" },
    new object[]{ "点光_泳池房_06",-14.0f,43.0f,4.2f,"#ffe2b0" },
    new object[]{ "点光_泳池房_07", -8.0f,32.0f,4.2f,"#ffe2b0" },
    new object[]{ "点光_泳池房_08", -8.0f,37.5f,4.2f,"#ffe2b0" },
    new object[]{ "点光_泳池房_09", -8.0f,43.0f,4.2f,"#ffe2b0" },
    new object[]{ "点光_泳池房_10", -2.0f,32.0f,4.2f,"#ffe2b0" },
    new object[]{ "点光_泳池房_11", -2.0f,37.5f,4.2f,"#ffe2b0" },
    new object[]{ "点光_泳池房_12", -2.0f,43.0f,4.2f,"#ffe2b0" },
    new object[]{ "点光_走道_01",   -8.0f,21.3f,4.2f,"#ffe2b0" },
    new object[]{ "点光_走道_02",   -2.0f,21.3f,4.2f,"#ffe2b0" },
    new object[]{ "点光_走道_03",  -12.0f,25.8f,4.2f,"#ffe2b0" },
    new object[]{ "点光_水面_01",  -17.5f,33.0f,0.6f,"#8fd3ff" },
    new object[]{ "点光_水面_02",  -17.5f,42.0f,0.6f,"#8fd3ff" },
    new object[]{ "点光_水面_03",   -3.5f,25.0f,0.6f,"#8fd3ff" },
    new object[]{ "点光_水面_04",   -3.5f,39.0f,0.6f,"#8fd3ff" },
    new object[]{ "点光_泵房",     -18.0f,23.8f,2.6f,"#ffe2b0" },
    new object[]{ "点光_更衣室_01",-15.0f,16.8f,2.6f,"#ffe2b0" },
    new object[]{ "点光_更衣室_02", -5.0f,16.8f,2.6f,"#ffe2b0" },
};

// REAL_PROPS：名称, x, z, 目标宽, 目标深, 预制体, 说明
var REAL_PROPS = new object[][] {
    new object[]{ "主阀",   -18.50f, 26.30f, 0.90f, 0.90f, "Valve.prefab",  "阀轮直径 0.9，立管落地" },
    new object[]{ "轮",     -17.50f, 23.30f, 0.45f, 0.45f, "Valve.prefab",  "同一模型缩水版，竖立放地面" },
    new object[]{ "抽水泵",  -5.50f, 29.30f, 0.65f, 0.63f, "PumpLP.prefab", "立在池核干区平台上" },
    new object[]{ "扳手",   -11.50f, 14.90f, 0.33f, 0.10f, "Wrench.prefab", "平躺在更衣室南墙内侧" },
};

// PROPS（纯红占位）：名称, x, z, 宽, 深, 说明
var PROPS = new object[][] {
    new object[]{ "占位_地漏",      -5.0f, 38.8f, 0.4f, 0.4f, "池底格栅 0.4×0.4" },
    new object[]{ "占位_气垫_长池",-17.5f, 37.8f, 2.0f, 1.0f, "浮于水面，顶面 y=-0.20" },
    new object[]{ "占位_气垫_池核", -9.0f, 27.3f, 3.0f, 2.0f, "浮于水面，顶面 y=-0.20" },
    new object[]{ "占位_管道_A",   -19.5f, 29.4f, 3.0f, 0.4f, "泵房 → 长池南叶" },
    new object[]{ "占位_管道_B",   -16.0f, 29.4f, 2.0f, 0.4f, "泵房 → 泳池房地面" },
};

// LOCKERS：不列 62 行表，用任务 9 步骤 1 的段循环生成。
// OldMetalWardrobe 0.49×0.45；北排 z=19.075 转 180°（正面朝南）、南排 z=14.525 转 0°（正面朝北）。
// 段（0.5 m 间距）：北排 (-21.5..-14.5) 14 个 + (-13.5..-6.5) 14 个，跳过与暗门洞 [-21.0,-20.0] 相交的 2 个 → 26
//                     南排 (-21.5..-14.5) 14 + (-13.5..-6.5) 14 + (-5.5..-1.5) 8 → 36
// 合计 62。
```

## 附录 B：公共 helper（每个 `execute_code` 开头先粘这一段）

```csharp
var PREFAB = "Assets/Prefabs/Level/泳池房.prefab";

Transform Group(GameObject root, string path) {
    var cur = root.transform;
    foreach (var seg in path.Split('/')) {
        var next = cur.Find(seg);
        if (next == null) {
            var go = new GameObject(seg);
            go.transform.SetParent(cur, false);
            next = go.transform;
        }
        cur = next;
    }
    return cur;
}

// 有渲染器的立方体（默认带 BoxCollider）
GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool collider = true) {
    var old = parent.Find(name);
    if (old != null) Object.DestroyImmediate(old.gameObject);
    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
    go.name = name;
    go.transform.SetParent(parent, false);
    go.transform.localPosition = center;
    go.transform.localScale = size;
    go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    if (!collider) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
    return go;
}

// 判某个平面点是否落在水体里（任务 3 的覆盖校验用）
bool InsideAnyWater(float x, float z) {
    foreach (var r in WATER)
        if (x > r[0] && x < r[2] && z > r[1] && z < r[3]) return true;
    return false;
}
```

## 附录 C：`Assets/Shaders/PoolWater.shader` 全文

```hlsl

Shader "Pool/PoolWater"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.18, 0.46, 0.71, 0.55)
        _WaveSpeed ("Wave Speed", Float) = 0.35
        _WaveScale ("Wave Scale", Float) = 1.6
        _Fresnel ("Fresnel Strength", Float) = 0.35
        _NormalStrength ("Normal Strength", Float) = 0.25
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings  { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _WaveSpeed;
                float _WaveScale;
                float _Fresnel;
                float _NormalStrength;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;
                float2 uv = IN.uv * _WaveScale;

                // 两组正弦叠加的水纹，法线用解析式求导，不依赖 dFdx/dFdy。
                float a = 6.28318 * uv.x + t * 2.0;
                float b = 5.0 * uv.y - t * 1.6;
                float wave = (sin(a) + sin(b)) * 0.5;

                float dx = 0.5 * cos(a) * 6.28318 * _WaveScale;
                float dy = 0.5 * cos(b) * 5.0 * _WaveScale;
                float3 n = normalize(float3(-dx * _NormalStrength, 1.0, -dy * _NormalStrength));

                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float fres = pow(1.0 - saturate(dot(n, viewDir)), 3.0) * _Fresnel;

                float3 col = _Color.rgb + fres + wave * 0.02;
                float alpha = saturate(_Color.a + fres * 0.5);
                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
```

## 附录 D：材质命名与份数对照

| 类别 | 命名 | 份数（预期） |
| --- | --- | --- |
| 地板 | `Tile_Pool_Floor_<宽>x<深>` | 15（16 块板去掉长廊；同尺寸合并） |
| 天花板 | `Tile_Pool_Ceil_<宽>x<深>` | 6 |
| 墙 | `Tile_Pool_Wall_<长>` | 9（建 10 段，其中两段等长合并成 1 份材质） |
| 池壁 | `Tile_Pool_池壁_<长>` | 7（20 段按长度去重） |
| 池底 | `Tile_Pool_池底_<宽>x<深>` | 5（7 块去重） |
| 池沿 | `Tile_Pool_池沿_<长>` | 7（与池壁同段长） |
| 台阶 | `Tile_Pool_台阶` | 1 |
| 其他 | `Water_Pool` / `Placeholder_Red` | 2 |
| **合计** | | **50 份 `Tile_Pool_*`**（15+6+9+7+5+7+1；另有 `Wall_Pool_Corridor` / `Water_Pool` / `Placeholder_Red` 3 份，共 53 份资产） |

---

## 执行记录（2026-11-21 内联执行）

已按任务 1–13 落地，预制体 `Assets/Prefabs/Level/泳池房.prefab` 完成，静态自检 19 项全通过
（地板/墙体/吊顶高度、两个门洞净宽、池体竖向分层、水面 9 块无碰撞、空气墙 6 个、储物柜 62 个朝向、
灯光 22+3、层级容器与 3 个锚点）。任务 14 的场景内手测待用户执行。

执行中与规格/计划的偏差（均已在预制体里落地，规格文档未同步修订）：

1. **长廊原本没有容器**：`TestRoom.prefab` 的子物体（地板/左墙/右墙/天花板/尽头封墙/入口侧壁×2/点光×3/返回传送面/返回传送相机/返回裁剪面/入口门楣）直接挂在根上。任务 1 先新建 `长廊` 容器再把它们搬进去，之后才延长到 14.3。
2. **地板覆盖校验的采样点落在边界上**：以 `x=-22.5` 起算会把「恰好贴在板边」的点判为未覆盖（9872 个假阳性）。改成从 `-22.45/+0.1` 步进取样后为 0，`Bounds` 判定仍用 `|x-cx| < extents - 0.001`。
3. **图纸里两条 1 m 水渠没有底与水面**：补充 `水渠_底_1/2`（顶面 -0.4，材质 `Tile_Pool_池底_1x8`）与 `水渠水面_08/09`（y -0.1）。水面因此从 7 块变 9 块，池底从 7 块变 9 块。
4. **干区平台原先只有四周围墙**：补充实体块 `池坑_干区平台`（x -7.5..-2.5、z 26.3..30.3、顶面 y=0、材质 `Tile_Pool_Floor_5x4`），抽水泵与排水交互点才有立足面。
5. **两个门洞在墙上留的是通高缺口**：补 `门楣_更衣室`（1.5×1.95，y 3.38）与 `门楣_暗门`（1.0×2.55，y 3.08），材质 `Tile_Pool_Wall_1.5` / `_1.0`。
6. **暗门缩放轴向**：`AutoDoor.prefab` 实测是 2.90×2.40×0.20 而不是计划里写的 1.5 m → 实例缩放 `x = 1.5/2.9`。暗门假柜正面原本朝 +X，要朝 -Z 需 `Euler(0,90,0)`，此时柜宽 0.45 映射到世界 X，所以缩放加在 **Z** 轴（`1.0/0.45`），不是计划里写的 X 轴 2.04。
7. **`execute_code` 的安全检查拦 `AssetDatabase.DeleteAsset` 与 `MoveAssetToTrash`**：改成「存在则 `Clear()` 后重写顶点」的幂等方式，不删资产。
8. **`Object` 在 execute_code 里歧义**（`UnityEngine.Object` vs `object`）：一律写全名 `UnityEngine.Object`。
9. **贴图不可读**：打包 `_MetallicGlossMap` 前要把源贴图 importer 的 `isReadable` 打开，否则 `GetPixels32()` 抛 `ArgumentException`。
10. **`AutoDoor` / `OldMetalWardrobe` 的渲染器数是 3 每柜**，不是计划假设的 10，所以预制体渲染器总数 304 而不是 640 左右。
11. **泵房两条水渠在渠底与四周之间有黑缝**：地板板厚 0.3（底面 -0.3）、渠底顶面 -0.4，中间 0.1 m 的竖向缝没有几何；墙脚从 y=0 起，渠边又是地板缺口，于是从渠里能看到「外面」的黑色。补 8 件内衬：`水渠_壁_西外/西内/东内/东外`（0.10×0.45×8.00，y -0.45..0，材质 `Tile_Pool_水渠壁_8x0.45`）+ `水渠_端壁_西前/西后/东前/东后`（1.00×0.45×0.10，材质 `Tile_Pool_水渠壁_1x0.45`）。渠内 12 个采样点 × 4 个方向射线全部命中，无漏缝。
12. **水渠上面的空气墙本来就存在**：`空气墙_水渠_西/东`（BoxCollider 1.00×1.80×8.00、isTrigger=false、无渲染器，世界范围 x -22.5..-21.5 与 -14.5..-13.5、z 20.3..28.3），与渠口完全重合，无需补。


---

## 附：Level2 独立运行测试入口（2026-11-21）

为方便不进 Level1 直接看泳池房，`Assets/Scenes/Level2.unity` 增加了一个只在单独运行该场景时生效的测试容器：

- 根节点 `测试_仅Level2`：挂 `TestSandboxGuard`（`Assets/Scripts/Level/TestSandboxGuard.cs`）+ `GameCursorState`。
- 其下 `= 玩家_测试`：`= 玩家.prefab` 实例，位置 `(-6, 0.05, 20)`（走道南段，正对池核），移除了 `PlayerControlLock`（它需要开场独白的 DialogueTrigger，测试场景没有会每次报错）。
- `Main Camera` 补 `CinemachineBrain`（SmartUpdate / BlendUpdateMethod 1 / EaseInOut 2 s，与 Level1 一致），玩家预制体里的虚拟相机才有画面。
- **不重复的保证**：`TestSandboxGuard.Start()` 里查 `FindObjectsByType<FirstPersonController>()`，数量 > 1（说明正式流程已经带了自己的玩家）就销毁整个 `测试_仅Level2`，连同它的 `GameCursorState`；单独运行 Level2 时数量为 1，测试玩家保留。
- 泳池房实例改名 `TestRoom → 泳池房`。

## 附：水面 shader 的两处编译修正（2026-11-21）

附录 C 的初版在 URP 下渲染成洋红（shader error），原因是：

1. 片元里用了 `dFdx/dFdy` 但没写 `#pragma target 3.0`；
2. `Pass` 缺 `Tags { "LightMode" = "UniversalForward" }`，URP 根本不会执行该 Pass。

现版本改为**解析式求导**（直接对两组正弦求偏导得到法线，不再依赖导数函数），并补上 LightMode 标签，`ShaderUtil.GetShaderMessages` 为 0 条。若以后要改波纹，只需调 `_WaveScale` / `_WaveSpeed` / `_NormalStrength`。

## 附：东侧走道补铺与池沿拐角补块（2026-11-21）

用户指出「墙_东外墙附近天花板和地面空缺」。复查确认：`池核东侧`（局部 x 0.5..2.5、z 23.3..28.3）这块 2×5 m 在房间分解里没有归属任何房间，因此既没铺地板也没吊顶。补法：

- 规格模型新增房间 `泳池房 东侧走道`（x 0.5..2.5 / z 23.3..28.3，净高 4.35、天花 4.5），`AITemp/pool/gen_spec.py` 的 `ROOMS` 同步，重生成规格 HTML（127114 B，地板件 17）。
- 预制体补 `地板_16`（1.7×5，中心 (1.65,-0.15,25.8)，材质 `Tile_Pool_Floor_1.7x5`）与 `天花板_泳池房 东侧走道`（2×5，中心 (1.5,4.5,25.8)，材质 `Tile_Pool_Ceil_2x5`）。
- 顺带发现池沿环带在段接头处有 8 个 0.3×0.3 缺口（长池东侧 4 个、池核西侧 2 个、池核东侧 2 个），补 `池沿_补1..8`（0.3×0.3×0.1，材质 `Tile_Pool_池沿_8`）。
- 复核：0.1 m 网格采样（x -22.45..2.46、z 14.35..46.26，扣除 6 处水域）→ **缺地板 0、缺天花板 0**；覆盖件 47、天花板 7、渲染器 323、灯光 25、碰撞体 313。

## 附：气垫换成真实模型（2026-11-21）

`Assets/Art/Models/气垫.fbx`（6 个子网格：`GEO-AirMattress` 2×1.4×0.233 含 2 个子网格 + 5 个阀门件）导入后按 Y 向上烘焙（`bakeAxisConversion=true`，关相机/灯光/动画，`ImportStandard`），包围盒变成 2 × 0.233 × 1.416、轴心在垫底中心。

- FBX 内嵌材质是**近黑色占位**（`_BaseColor` ≈ 0.027）且没有任何贴图，直接实例化会是黑块 → 在 `Assets/Materials/Props/` 新建三份 URP/Lit：`气垫_垫体`(#A8D5E2, smooth 0.35)、`气垫_阀件`(深灰 0.10/0.11/0.13, 0.45)、`气垫_阀芯`(#2E6BD6, 0.60)。**`GEO-AirMattress` 有 2 个子网格槽，必须整组 `sharedMaterials` 替换**，只设 `sharedMaterial` 会留一个槽还是内嵌近黑材质（第一版就是这么渲成深蓝黑的）。
- 摊平保存为 `Assets/Prefabs/Props/气垫.prefab`（`UnpackPrefabInstance(Completely)` 后存，不嵌套 FBX 实例，避免材质覆盖散落在嵌套层里）。
- 替换预制体里两个纯红占位：
  - `气垫_长池`：`(-17.5, -0.32, 37.8)`、缩放 0.90 → 1.80 × 0.21 × 1.27（长池颈 `x -18.5..-16.5` 内，左右各留 10 cm）
  - `气垫_池核`：`(-9.0, -0.40, 27.3)`、缩放 1.45 → 2.90 × 0.338 × 2.05（贴着干区平台西缘 `x=-7.5`）
  - 都不带碰撞体；水面 y=-0.25，所以两只都是半沉在水中（顶面 -0.11 / -0.06）
- 第一版试过缩放 1.0 / 1.5 再带 ±6° 偏转，旋转后的 AABB 会长到 3.20 m，顶穿池壁和平台 → 改成不旋转 + 缩小到上面的数值。
- 规格文档同步：`REAL_PROPS` 增加两件气垫，`PROPS` 只剩地漏 + 管道 A/B（占位从 5 件降到 3 件），`floors: 17 lockers: 62 lights: 22 props: 3`。
