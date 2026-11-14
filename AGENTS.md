# AGENTS.md — MakeYourChoice

48 小时 Game Jam 项目（游趣无限2026 × 紫与灰gj）。Unity 2022.3.62f3c1，URP 14.0.12（3D Universal Renderer；2026-11-14 由 2D 渲染转 3D）。
项目自有场景只有 `Assets/Scenes/Game.unity`（其余 `.unity` 都在插件示例与场景模板目录里）。

## 策划案（需求与玩法的唯一来源）

飞书 wiki《做出选择！！！》：https://bqt4a2kph6x.feishu.cn/wiki/WTnnwjY2Pi1a7kkdBgFctWRRnze
实现玩法前先读它，不要自行发明设计。正文文字很少，实质内容在画板里：用 `lark-cli docs +fetch --doc <URL>` 取 `<whiteboard token=...>`，再 `lark-cli docs +media-download --type whiteboard --token <token> --output <路径>` 导出图片查看。

## 本项目硬约束

- **48h 优先出可玩版本**：能跑 > 优雅。不要引入复杂抽象层——不自研框架、DI 容器、ECS、事件总线或额外分层；需要架构时用 QFramework 现有的 MVC。
- **先查再用，不重复造轮子**：动手实现前，先确认下表中的插件/框架是否已经提供了这个能力。
- **第三方与框架目录只读**：`Assets/Plugins`（DOTween Pro、Easy Save 3、Odin Inspector、vFolders）与 `Assets/QFramework` 不在本项目维护范围内，不要修改；需要扩展时在 `Assets/Scripts` 下写自己的代码。
- **不用 superpowers 的 `brainstorming` skill**：需求有疑问就直接问用户确认，不要走该 skill 的问答流程。

## 已有能力（实现前先看这里）

| 需求 | 用什么 |
| --- | --- |
| UI 面板、层级、面板代码生成 | QFramework UIKit + CodeGenKit（命名空间 `Game.UI`，脚本 `Assets/Scripts/UI`，预制体 `Assets/Prefabs/UI`，配置见 `Assets/QFrameworkData/ProjectConfig/ProjectConfig.json`） |
| 音频播放、BGM | QFramework AudioKit |
| 资源加载 | QFramework ResKit |
| 中文文本渲染 | `Assets/Fonts/SourceHanSansCN-Medium SDF.asset`（思源黑体 Medium，OFL 可随游戏分发；Dynamic + 4096 图集，已预烘 `Assets/Fonts/常用字.txt` 的 3654 字，生僻字运行时会自动补字形）。UI 中文一律用它，不要运行时自建字体资产。世界空间 TMP 文字的朝向：正面法线 = `(sinθ, 0, cosθ)`（θ=eulerAngles.y），θ=0 时正面朝 -Z、相机在 -Z 侧才看得到正面；字形「→」指向 `(cosθ, 0, -sinθ)`。想同时"字朝玩家"又"箭头指向某方向"要先算这个组合，不要靠补 180° 兜底（会看到背面镜像） |
| 补间动画 | DOTween Pro |
| 存档、读档 | Easy Save 3 |
| 关卡几何、第一人称视角 | ProBuilder（5.2.4）搭 3D 几何 + Cinemachine 第一人称；运行时组件在 `Assets/Scripts/Level`（`LowResDisplay` 低分辨率显示、`FirstPersonController`、`AutoDoor`、`TriggerVolume`）。`AutoDoor` 有两种开门方式：`Rise`（整扇向上，旧的卷帘门）与 `SlideApart`（左右门扇 `Leaf_Left`/`Leaf_Right` 各向两侧滑 `slideDistance`，电梯门用这个）；滑动门的两扇要略宽于门洞并在中缝重叠，滑到底还要多滑几厘米藏进门洞侧壁后面，否则会露出薄片或与侧壁共面闪。几何用 `Assets/Materials/Level` 下的材质（`Floor_Office` / `Floor_OfficeElevator` / `Door_Office` / `Door_Elevator` / `Wall_ElevatorSide` / `Wall_ElevatorBack` / `YellowBox`，贴图来自 `Assets/Art/素材`，导入用 Point + 不压缩）。**平铺必须写进材质资产的 `_BaseMap_ST`**：`MaterialPropertyBlock` 不随场景序列化，Play 里正常、退出 Play 就丢了；同一种贴图尺寸不同要做成两份材质 |
| 关卡贴图与招牌 | `Assets/Art/素材` 的原图带透明边距，直接用 URP/Unlit 不透明画会出现黑边——要先用 Pillow 裁成 `*_trim.png`（`办公室门_trim`、`黄箱子立方_trim`），黄箱子这种还留透明角的材质要开 Alpha Clipping（`_AlphaClip=1`、`_Cutoff=0.5`、`renderQueue=2450`）。黄箱子不是立方体而是 **Quad + `YBillboard`**（`Assets/Scripts/Level/YBillboard.cs`，始终面向相机；注意 Unity Quad 正面法线是 `(0,0,-1)`）。门板要和墙同厚（0.3）藏住侧脸，且别和墙内表面共面（门放 z=4.63，墙内表面 4.5），否则 z-fighting。TMP 招牌不要当带非均匀缩放的物体的子级（父级 scale 会拉扯字形、还会让 TMP 缓存包围盒失效被视锥剔除），红字这类低对比文字用 `Assets/Materials/Level/Sign_Red.mat`（描边 `_OutlineWidth` + `_FaceDilate`）；材质要在编辑期 `AssetDatabase.CreateAsset` 存成资产，运行时 `new Material` 保存场景后会丢 |
| 剧情对话、3D 飘字 | Yarn Spinner 3.2.8 + `Assets/Scripts/Dialogue`（`FloatingDialoguePresenter` 决定显示 3D 字还是对话框、`DialogueTrigger`、`WorldLineView`、`FloatingLine`、`DialogueBoxView`、`Dialogue3DOverlay`）；对话文本改 `Assets/Yarn/*.yarn`，不要自己写解析器；3D 字必须在 `Dialogue3D` 层（由 `Dialogue3DCamera` 全分辨率叠加，主相机 `cullingMask` 排除该层）。"走进去就播一段剧情"用 `DialogueZoneTrigger`（放在触发区上，配 `DialogueTrigger` + `BoxCollider`）；`Assets/Prefabs/Dialogue/FloatingLine.prefab` 的 root 缩放必须是 0.02，改成 0.08 会让一句字宽到 100 m。`WorldLineView.minReadableDistance`（默认 1.2 m）决定"太近就不用 3D 字改用对话框"——锚点贴脸时字会糊满屏幕 |
| 关卡一的流程触发 | `Assets/Scripts/Level/Level1`（`Level1State` 关卡开关状态、`Level1DoorInteract` 瞄准电梯门按交互键开门、`Level1RedTextTrigger` 红字要在可视距离内、视线对准且真的出现在画面里、`Level1DoorListenTrigger` 门前听 3 s、`Level1ElevatorCountdown` 电梯内倒计时结局），触发区在 `Level/Level1/Triggers`。**看过红字后电梯门上锁**：`Level1State.CanOpenDoor` 在风声剧情（`LV1_Listen`）播完前为 false，门既不显示交互提示也开不了，`Level1DoorListenTrigger` 在剧情播完后才 `MarkListenFinished()` 解锁。玩家侧还有 `PlayerControlLock`（挂在 Player 上，开场剧情播完前禁用 `FirstPersonController`，即开场不能走动/回头） |
| 输入（键位/手柄） | `Assets/Scripts/Game/PlayerInputRouter.cs`：代码里建一张 `InputActionMap("Player")`，动作 Move / Look / Interact / Sprint / Menu，键位按飞书 3C 表（WASD + 左摇杆、鼠标 + 右摇杆、鼠标左键 + 手柄 A、Shift + 扳机、Esc + Start）。`CurrentDevice` 会在键鼠/手柄之间自动切换（`DeviceChanged` 事件），新增输入一律从 `PlayerInputRouter.Instance` 取动作，不要直接读 `Keyboard.current`。注意本版本 Input System 的 `AddAction` 没有 `expectedControlType` 参数 |
| 交互聚焦与准心 HUD | `Assets/Scripts/Game/InteractFocus.cs`（静态登记"当前瞄准的可交互物"）+ `Assets/Scripts/UI/InteractPromptView.cs`（场景 `HUD` 的 Overlay Canvas：四角矩形准心 `Assets/Art/UI/hud_crosshair.png` + 右侧交互键图标 `key_mouse_left.png` / `key_pad_a.png`，按 `CurrentDevice` 切换）。可交互物在 `Update` 里用 `Physics.Raycast(eye.position, eye.forward, ~0, QueryTriggerInteraction.Ignore)` 打自己后 `InteractFocus.Set(this)`，`OnDisable` 清掉；注意别用 `~(1 << 玩家层)` 排除自己（Eye 在 Default 层会把墙也排掉） |
| 编辑器增强 | Odin Inspector、vFolders（编辑器工具，运行时代码不要依赖） |

## 代码约定

- 游戏代码写在 `Assets/Scripts`（目前没有 asmdef，全部编入 Assembly-CSharp）；不要为小功能新建程序集。
- 编辑器专用代码放 `Editor` 目录，运行时程序集不得引用 `UnityEditor`。
- 输入使用新版 Input System（`com.unity.inputsystem` 1.14.2 已安装；`ProjectSettings/ProjectSettings.asset` 里 `activeInputHandler: 2` 表示新旧都开，旧 API 仍能编译，只能靠本约定约束）：一律走 `UnityEngine.InputSystem`（`Keyboard.current` / `Mouse.current` / `InputAction`），不要写旧版 `UnityEngine.Input`。`Assets/Scripts` 下已无旧版调用。
- 对外开放到 Inspector 的序列化字段（`public` 或 `[SerializeField]`），一律加 Odin 的 `[LabelText("中文")]` 标中文名（`using Sirenix.OdinInspector;`，属性程序集在 `Assets/Plugins/Sirenix/Assemblies/`），不让英文变量名直接出现在 Inspector 里。CodeGenKit 生成的 `*.Designer.cs` 会被工具重新生成覆盖，属例外，不要手改。
- 物体优先在场景/预制体里摆好，不要在代码里 `new`：相机、Canvas、UI、管理器这类数量固定的对象，直接在场景或预制体里加好，脚本只负责取引用和驱动逻辑；只有运行时才能确定数量或内容的（按数据实例化、运行时生成的纹理等）才用代码创建。
- 已开启 Enter Play Mode Options，只关闭了 Domain Reload（`ProjectSettings/EditorSettings.asset`：`m_EnterPlayModeOptionsEnabled: 1`、`m_EnterPlayModeOptions: 1`，场景仍会重载）：静态字段、静态事件订阅、单例状态会跨 Play 残留，二次 Play 前必须自己重置（`[RuntimeInitializeOnLoadMethod]` 清静态、退订静态事件），不要依赖域重载帮你清理。已知踩坑：QFramework UIKit 的 `UIKit.Config`（含 PanelLoaderPool）与 `UIKit.Table` 跨 Play 残留，会让面板只实例化出空壳（`Info == null`、按钮无监听、Table 计数错乱），入口用 `Assets/Scripts/Game/GameBootstrap.cs` 的 `ResetUIKitState()` 在 `SubsystemRegistration` 阶段重置；新增场景入口时照此重置一次。
- Yarn 台词要换行时写 `<br>`（TMP 富文本标签，逐字显示时也能正确换行）；**不要写 `\n`**——Yarn 3 会在编译期报 `Syntax error: Unexpected "n" while reading a line statement`，整份 `.yarn` 编译失败、`YarnProject.Program.Nodes` 变空，运行时报 `Cannot load node ...: No nodes have been loaded`。
- 不手改 `.meta`；移动、重命名、删除资产时连同 `.meta` 一起处理。
- 场景、预制体、`.asset` 等序列化文件通过 Unity 编辑器或 Unity MCP 修改，不手写 YAML。

## 文档

- 改了功能后**不需要**同步 `docs/superpowers` 下的 spec/plan 文档（它们只是当时的过程产物）；只需同步本文件的「已有能力」表。

## 验证

- 本项目没有 CI，也没有无头构建/测试脚本；不要臆造 Unity `-batchmode` 命令。
- 改完脚本在 Unity 编辑器里等编译结束，确认 Console 无报错。
- 本项目已安装 `com.coplaydev.unity-mcp`：可用 Unity MCP 刷新资源、读 Console、进入 Play 模式检查；MCP 不可用时改用手动检查。
- 用 MCP 联调时，`pwsh Start-Sleep` 单次不要超过 10 秒（长等待拆成多次，便于及时观测）；不要在 `execute_code` 里 `Thread.Sleep` 等动画/对话——会冻结编辑器主线程，用协程采样代替。
- 交付前至少在 `Game.unity` 里 Play 一次，确认核心玩法流程能走通；打包前确认 Build Settings 的场景列表包含 `Game.unity`。
