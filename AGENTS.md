# AGENTS.md — MakeYourChoice

游趣无限2026 × 紫与灰gj 48 小时 Game Jam 之后的迭代项目，已有可玩原型，11 月 22 日截止。Unity 2022.3.62f3c1，URP 14.0.12（3D Universal Renderer；2026-11-14 由 2D 渲染转 3D）。
项目自有场景为 `Assets/Scenes/Level1.unity` 与 `Assets/Scenes/Level2.unity`（旧的 `Game.unity` 已删除；其余 `.unity` 都在插件示例与场景模板目录里）。

## 策划案（需求与玩法的唯一来源）

飞书 wiki《做出选择！！！》：https://bqt4a2kph6x.feishu.cn/wiki/WTnnwjY2Pi1a7kkdBgFctWRRnze
实现玩法前先读它，不要自行发明设计。正文文字很少，实质内容在画板里：用 `lark-cli docs +fetch --doc <URL>` 取 `<whiteboard token=...>`，再 `lark-cli docs +media-download --type whiteboard --token <token> --output <路径>` 导出图片查看。

## 本项目硬约束

- **不要引入复杂抽象层**：不自研框架、DI 容器、ECS、事件总线或额外分层；需要架构时用 QFramework 现有的 MVC。
- **QFramework 自带的事件系统可以用**（禁止的是自研事件总线，不是这些现成设施）：`TypeEventSystem.Global`（类型事件，`Send<T>()` / `Send<T>(T)` / `Register<T>(Action<T>)`，返回 `IUnRegister`）、`EnumEventSystem.Global` / `StringEventSystem.Global`（枚举 / 字符串键事件）。优先类型事件（有编译期检查）；`QEventSystem` 与 `StringEventSystem` 的静态快捷方法是 `[Obsolete]`，不要用旧写法。**订阅返回的 `IUnRegister` 必须落掉**：`.UnRegisterWhenGameObjectDestroyed(gameObject)`、`.UnRegisterWhenDisabled(this)` 或在 `OnDisable` 手动注销——本工程关闭了 Domain Reload，`TypeEventSystem.Global` 的订阅会跨 Play 残留，漏掉注销会出现一个事件触发多次的幽灵回调。能用显式引用或单次回调讲清的关系就别改成广播。
- **先查再用，不重复造轮子**：动手实现前，先确认下表中的插件/框架是否已经提供了这个能力。
- **第三方与框架目录只读**：`Assets/Plugins`（DOTween Pro、Easy Save 3、Odin Inspector、vFolders）与 `Assets/QFramework` 不在本项目维护范围内，不要修改；需要扩展时在 `Assets/Scripts` 下写自己的代码。

## 已有能力（实现前先看这里）

| 需求 | 用什么 |
| --- | --- |
| UI 面板、层级、面板代码生成 | QFramework UIKit + CodeGenKit（命名空间 `Game.UI`，脚本 `Assets/Scripts/UI`，预制体 `Assets/Prefabs/UI`，配置见 `Assets/QFrameworkData/ProjectConfig/ProjectConfig.json`） |
| 音频播放、BGM | QFramework AudioKit |
| 资源加载 | QFramework ResKit |
| 中文文本渲染 | `Assets/Fonts/SourceHanSansCN-Medium SDF.asset`（UI 中文一律用它，不要运行时自建字体资产） |
| 补间动画 | DOTween Pro |
| 存档、读档 | Easy Save 3 |
| 剧情对话、3D 飘字 | Yarn Spinner 3.2.8 + `Assets/Scripts/Dialogue`；台词改 `Assets/Yarn/*.yarn`，不要自己写解析器 |
| 关卡流程、门与传送门 | `Assets/Scripts/Level`（Hall / BackRooms / Portal / Elevator）与 `Assets/Scripts/Player` |
| 输入（键位 / 手柄） | `Assets/Scripts/Player/PlayerInputRouter.cs`（新版 Input System 动作表） |
| 交互聚焦与准心 HUD | `Assets/Scripts/Interaction/InteractFocus.cs` + `Assets/Scripts/UI/InteractPromptView.cs` |
| 可交互物（按键交互） | `Assets/Scripts/Interaction`：`InteractableBase`（抽象基类，视线 Raycast + 交互键）、`DoorInteract`（AutoDoor 门）、`CabinetDoorInteract`（无刚体柜门，绕铰链 Slerp）；用法见 `.agent-guides/doors.md` |
| 关卡几何、第一人称视角 | ProBuilder（5.2.4）搭 3D 几何 + Cinemachine 第一人称 |
| 编辑器增强 | Odin Inspector、vFolders（编辑器工具，运行时代码不要依赖） |
| 道具模型、外部 FBX 预处理 | `Assets/Prefabs/Props` + `Assets/Art/Models`；Blender 拆分与缩放烘焙见 `.agent-guides/props.md` |

各模块的脚本职责、场景坐标、材质参数与踩坑记录不在本文件重复，按下一节协议加载。

## 条件性规范（按需加载）

本文件只保留项目级约束；按任务触发的细节规范放在各作用域旁的 `.agent-guides/` 里。**改动某个模块前，必须按下列协议加载对应条目**：

1. 先确定要改的文件属于哪个 `AGENTS.md` 作用域：根文件，或文件树中更近的嵌套 `AGENTS.md`（`Assets/Plugins`、`Assets/QFramework` 是只读第三方，不需要）。按修改后的最终位置判断，不按临时路径。
2. 查看该作用域旁的 `.agent-guides/` 现有条目。
3. 先读条目的 `description`（触发条件），判断是否与本次任务相关；**只加载相关条目**，不要通读全部。
4. 条目若形如 `*/GUIDE.md` 的包，只跟进其中条件成立的 `references/` 文件，不要下钻更深的层级。
5. 条目与根文件冲突时以根文件为准；条目没覆盖的历史设计细节，去 `docs/superpowers` 找对应 spec。

**路由与细节分离**：本文件对项目记忆（模块职责、场景坐标、修改流程、踩坑记录等**局部性信息**）只承担**路由职责**——写清「这件事由哪个模块负责、什么条件下加载哪条规范」；具体细节一律写进对应作用域的 `.agent-guides/` 条目。笼统的细节不要堆回本文件：本文件变长通常说明该拆条目了；只有跨模块、所有任务都必须遵守的约束才留在本文件。

新增或改名条目时，`description` 写成「改动什么时使用」的触发句；根文件不维护条目索引，发现以目录实际内容为准。条目内部同样按需分层：短条目单文件即可，长条目再拆 `references/`，不要一个文件塞进整个子系统。

## 代码约定

- 游戏代码写在 `Assets/Scripts`（目前没有 asmdef，全部编入 Assembly-CSharp）；不要为小功能新建程序集。
- 编辑器专用代码放 `Editor` 目录，运行时程序集不得引用 `UnityEditor`。
- 输入使用新版 Input System（`com.unity.inputsystem` 1.14.2 已安装；`ProjectSettings/ProjectSettings.asset` 里 `activeInputHandler: 2` 表示新旧都开，旧 API 仍能编译，只能靠本约定约束）：一律走 `UnityEngine.InputSystem`（`Keyboard.current` / `Mouse.current` / `InputAction`），不要写旧版 `UnityEngine.Input`。`Assets/Scripts` 下已无旧版调用。
- 异步与等待一律用 UniTask（`Cysharp.Threading.Tasks`；`Assets/Plugins/UniTask` 的 asmdef 已 `autoReferenced`，`Assets/Scripts` 直接 `using` 即可，不要为它新建程序集）：**不要写协程**，`Assets/Scripts` 下 `StartCoroutine` / `IEnumerator` / `yield return` / `WaitForSeconds` 零容忍。等待用 `UniTask.Delay(TimeSpan, DelayType.DeltaTime, cancellationToken: ct)`、逐帧用 `UniTask.NextFrame(ct)`；入口方法写 `async UniTaskVoid` + `.Forget()`（不要 `async void`；`DialogueTrigger.StartDialogue` 因 UnityEvent 与 Yarn 接口保留 `async void`，属唯一例外），可取消方法最外层 `catch (OperationCanceledException)` 静默退出，否则取消会被 UniTaskScheduler 当异常上报。生命周期取消用 `destroyCancellationToken`，需要重启动的用 `CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken)` 并在重启前 `Cancel()` + `Dispose()`。等 DOTween 补间一律 `await tween.WaitForCompletionAsync(ct)`（`Assets/Scripts/Core/TweenAwaiter.cs`），不要用 UniTask 自带的 `AsyncWaitForCompletion`——本项目 DOTween 不是 UPM 包，`UniTask.DOTween` 支持程序集不会参与编译。
- 对外开放到 Inspector 的序列化字段（`public` 或 `[SerializeField]`），一律加 Odin 的 `[LabelText("中文")]` 标中文名（`using Sirenix.OdinInspector;`，属性程序集在 `Assets/Plugins/Sirenix/Assemblies/`），不让英文变量名直接出现在 Inspector 里。CodeGenKit 生成的 `*.Designer.cs` 会被工具重新生成覆盖，属例外，不要手改。
- 物体优先在场景/预制体里摆好，不要在代码里 `new`：相机、Canvas、UI、管理器这类数量固定的对象，直接在场景或预制体里加好，脚本只负责取引用和驱动逻辑；只有运行时才能确定数量或内容的（按数据实例化、运行时生成的纹理等）才用代码创建。
- 已开启 Enter Play Mode Options，只关闭了 Domain Reload（`ProjectSettings/EditorSettings.asset`：`m_EnterPlayModeOptionsEnabled: 1`、`m_EnterPlayModeOptions: 1`，场景仍会重载）：静态字段、静态事件订阅、单例状态会跨 Play 残留，二次 Play 前必须自己重置（`[RuntimeInitializeOnLoadMethod]` 清静态、退订静态事件），不要依赖域重载帮你清理。已知踩坑：QFramework UIKit 的 `UIKit.Config`（含 PanelLoaderPool）与 `UIKit.Table` 跨 Play 残留，会让面板只实例化出空壳（`Info == null`、按钮无监听、Table 计数错乱），入口用 `Assets/Scripts/Core/GameBootstrap.cs` 的 `ResetUIKitState()` 在 `SubsystemRegistration` 阶段重置；新增场景入口时照此重置一次。
- 不手改 `.meta`；移动、重命名、删除资产时连同 `.meta` 一起处理。
- 场景、预制体、`.asset` 等序列化文件通过 Unity 编辑器或 Unity MCP 修改，不手写 YAML。
- **AI 的一次性脚本、临时目录与中间产物统一放仓库根目录的 `AITemp/`**（该目录已在 `.gitignore` 中忽略）：不要散落在仓库根、`Assets/` 或 `docs/` 下；需要长期保留的产物再移到正式位置。

## 外部工具

- **Blender 5.2 / 5.3** 已安装在本机：`F:\SteamLibrary\steamapps\common\Blender\blender.exe`。FBX / GLB 的网格拆分、门扇轴心对齐、比例修正与重新导出都可以用它；无头执行用 `blender.exe -b <输入.blend> -P <脚本.py>`（或 `--python-expr`）。这是 Steam 安装目录，只读使用，不要把工程资产写进去。

## 文档

- 改了功能后**只需要**同步 `docs/superpowers/specs/` 下的规格/设计文档（`*-design.*` 设计决定与设计规格 HTML），再把新的**能力路由**补进本文件的「已有能力」表；模块细节、坐标、修改流程与踩坑写进 `.agent-guides/` 对应条目，**不要堆回本文件**。**不需要**回头更新 `plans/` 的落地文档——plan 是当时的分步实施记录，写完即冻结，后续变更以 spec/设计文档和代码为准。
- **规格/设计文档写成 HTML**：`specs/` 下的设计规格、方案对比与平面示意图用自包含 `.html`（内联 CSS/SVG，无外部依赖，可直接用浏览器打开），不要写成 `.md`；需要给用户看的取舍、尺寸标注、流程节点都画进图里。`plans/` 的分步实施仍可用 `.md`（只写一次，后续变更不再回改）。

## 协作方式

- **涉及视觉效果的测试交给用户手测。** 画面观感、动态场景（相机运动、动画、光照、粒子、UI 布局等）不要自己 Play 截图后下结论——思考与工具调用的延迟会错过动态画面，容易误判。完成代码与静态检查后，把「在 Unity 里点哪里、按什么、预期看到什么」写清楚，由用户手测并反馈；确实需要截图证据时，先说明要测什么，请用户截图或确认后再进行。
- **有困惑或需要决策时先提问，不要默认直接动手。** 设计或落地细节不明确（交互方式、数值、层级归属、与现有系统的边界等）时，向用户提问并给出可选方案与推荐，确认后再实现；必要时参考 `grilling`（grill-me）技能，先把方案问清楚再落代码。

## 验证

- **不要操控电脑替你手测**：不要用鼠标/键盘模拟（按住 W 走动、点游戏按钮、点对话推进等）去跑 Play 流程。编译通过、代码自查完成后，把「需要在 Unity 里点哪里、按什么、预期看到什么」写清楚告诉用户，由用户自己手测并反馈结果。MCP 只用于刷新资源、读 Console、必要时读取运行时状态，不要代替用户操作游戏。
- 本项目没有 CI，也没有无头构建/测试脚本；不要臆造 Unity `-batchmode` 命令。
- 改完脚本在 Unity 编辑器里等编译结束，确认 Console 无报错。
- 本项目已安装 `com.coplaydev.unity-mcp`：可用 Unity MCP 刷新资源、读 Console、进入 Play 模式检查；MCP 不可用时改用手动检查。**Play 检查时保持 Unity 是前台窗口**：`ProjectSettings.asset` 的 `runInBackground: 0`，编辑器一失焦 Play 就暂停（`Time.time`、楼号、动画都不推进，`ScreenCapture` 也可能不落盘），切回窗口才继续；不要误判成脚本卡死。
- 用 MCP 联调时，`pwsh Start-Sleep` 单次不要超过 10 秒（长等待拆成多次，便于及时观测）；不要在 `execute_code` 里 `Thread.Sleep` 等动画/对话——会冻结编辑器主线程，用协程采样代替。
- 交付前至少在涉及的自有场景（`Assets/Scenes/Level1.unity` / `Level2.unity`）里 Play 一次，确认核心玩法流程能走通；打包前确认 Build Settings 的场景列表包含这些场景。
