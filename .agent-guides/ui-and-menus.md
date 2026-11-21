---
description: 改动 UI 面板、菜单、准心 HUD、焦点导航或开场电梯演出时使用。
---

# UI、菜单与开场演出

改动本域代码前通读本文件；项目级约束见根 `AGENTS.md`，同目录其他条目按需加载。

## UI 面板与层级（UIKit + CodeGenKit）

QFramework UIKit + CodeGenKit（命名空间 `Game.UI`，脚本 `Assets/Scripts/UI`，预制体 `Assets/Prefabs/UI`，配置见 `Assets/QFrameworkData/ProjectConfig/ProjectConfig.json`）

## 交互聚焦与准心 HUD

`Assets/Scripts/Interaction/InteractFocus.cs`（静态登记"当前瞄准的可交互物"）+ `Assets/Scripts/UI/InteractPromptView.cs`（场景 `HUD` 的 Overlay Canvas：四角矩形准心 `Assets/Art/UI/hud_crosshair.png` + 右侧交互键图标 `key_mouse_left.png` / `key_pad_a.png`，按 `CurrentDevice` 切换）。

准心与按键提示只在光标未锁定或开场剧情锁定（`PlayerControlLock.IsLocked`）时隐藏，**其他剧情播放中照常显示**，玩家可以边听台词边开门/拿卡。

可交互物在 `Update` 里用 `Physics.Raycast(eye.position, eye.forward, ~0, QueryTriggerInteraction.Ignore)` 打自己后 `InteractFocus.Set(this)`，`OnDisable` 清掉；

注意别用 `~(1 << 玩家层)` 排除自己（Eye 在 Default 层会把墙也排掉）

## 开始菜单与开场电梯演出

`Assets/Scripts/Core/GameBootstrap.cs`（`Start` 先 `floorDisplay.BeginIdleDescent()` + `rideMotion.Ride()` 再开 `StartPanel`；点「开始游戏」后 `OnStartRequested` 里立刻 `ClosePanel<StartPanel>()` 隐藏主界面，再 `ArriveAt(arrivalFloor=1)` 驱动到站演出，`OnArrived` 只做 `rideMotion.Arrive()` 镜头回弹）、`Assets/Scripts/Level/Elevator/ElevatorFloorDisplay.cs`（电梯门外红墙上的竖版楼层牌：上方楼号滚动、下方两枚「▼」循环下移，到站减速停稳后箭头淡出、楼号滑到面板竖直中央再回调；

最后一格显示由 `到站楼层显示` 决定（默认 `NaN`），内部楼层号仍按数字递减，所以会看到「02 → NaN」并保持；箭头起点/滚动距离/到站高度在组件上配，版式改动不改代码常量；箭头颜色取场景 TMP 原色、代码只改 alpha（`Awake` 里缓存），到站标签（`NaN`）的颜色由 `到站楼层颜色` 配，留空 `到站楼层显示` 时退回琥珀 `DigitColor`；静态 `Instance`）、`Assets/Scripts/Level/Elevator/ElevatorRideMotion.cs`（挂在 PlayerVCam 上：菜单期间轻微晃动、到站下沉回弹）。

面板不直接引用场景，靠 `Assets/Scripts/UI/StartPanel.cs` 的 `StartPanelData.StartRequested` 回调（`Func<bool>`，返回是否接受本次请求）驱动；

点「开始游戏」时先 `SetButtonsInteractable(false)` 防连点（配色由 `SelectableHighlight` 的禁用色负责——按钮 `transition` 是 None，只设 `interactable` 不会自动变灰），回调被接受后面板立即关闭，只有回调返回 false 或重新 `OnShow` 时才恢复按钮；

面板背景是半透明压暗，让开场相机画面透出来

## 菜单面板的手柄导航与返回

场景 `EventSystem` 必须挂新版 `InputSystemUIInputModule`：十字键/摇杆靠它的默认 UI 动作表导航，旧的 `StandaloneInputModule` 只认 `InputManager` 里配的轴（本项目只配了左摇杆，十字键根本没映射）。

QFramework 的 `UIRoot` 在首次开面板时还会生成一个带旧模块的 `EventSystem`，所以 `Assets/Scripts/Core/GameBootstrap.cs` 开完面板后调 `Assets/Scripts/UI/UiNavigation.cs` 的 `EnsureSingleEventSystem()`：保留带新模块的那一个、销毁其余，并把 `deselectOnBackgroundClick` 关掉（默认开着，鼠标点面板空白会清掉选中，十字键随即失去导航起点）。

面板要自己给初始焦点并在丢失时补回：`StartPanel.OnShow` 选 `Btn_Start`、`PausePanel` 选 `Btn_Continue`，两者 `Update` 里调 `UiNavigation.RecoverFocusIfLost(默认项)`；

按钮间用 `UiNavigation.ChainVertical(...)` 显式串上下邻居（首尾循环），左右不设邻居——`Slider.OnMove` 在没有左右邻居时才按 `stepSize`（区间 10%）调值。

选中视效在 `Assets/Scripts/UI/SelectableHighlight.cs`（底色/描边/文字/缩放插值 + 左侧 `FocusBar` 指示条），挂它的按钮必须把 `Selectable.transition` 设成 `None`，否则和 ColorTint 打架；

滑条这类「整条轨道不能被点亮」的控件要把它的 `tintPlate` 关掉并把 `plate` 置空，否则轨道背景会随焦点整条变成焦点色，0% 时看起来像满格。

返回键不接 EventSystem 的 Cancel（会和 Esc 双触发），用 `PlayerInputRouter.Back`（`<Gamepad>/buttonEast`，手柄 B）在 `PausePanel.Update` 里 `CloseSelf()`，Esc 仍走 `Menu`
