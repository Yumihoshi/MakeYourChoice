# 本地补丁（Local patches）

- 上游：https://github.com/WooshiiDev/HierarchyDecorator
- 版本：0.12.1，commit `599e13a184`
- 本目录是嵌入包（embedded package），`Packages/manifest.json` 里已移除原来的 git 依赖，
  Package Manager 会改用这一份。改动 Unity 会直接生效，不用重新解析 git。

## 为什么要用本地副本

上游把「自定义脚本的组件图标」取成 `AssetDatabase.GetCachedIcon(script 路径)`，并且要求
图标非空才算有效条目（`ComponentType.IsValid()`）。对包内脚本（例如 Cinemachine 的
`CinemachineBrain`）这个图标会拿不到，于是同一条目永远 Invalid：`TryGetCustomComponent`
找不到它 → 每帧重新注册 → 报 `Attempted to register a component that already exists`，
并在 `ComponentItem.UpdateActiveState` 抛 `NullReferenceException`，随后 IMGUI 报
`You are pushing more GUIClips than you are popping`。

对应上游 issue：WooshiiDev/HierarchyDecorator#134（当前仍开着，master 代码与本副本打补丁前一致）。

## 改了什么

1. `Scripts/Editor/Data/Types/ComponentType.cs` — `GetTypeContent()`：自定义脚本图标为空时
   回退到 `EditorGUIUtility.IconContent("cs Script Icon").image`，条目不再因图标缺失而失效。
2. `Scripts/Editor/Data/Types/ComponentGroup.cs` — `TryGetComponent()`：只按 `Type` 匹配，
   不再要求 `IsValid()`；图标缺失不再被误判成「没注册过」。
3. `Scripts/Editor/Data/ComponentData.cs` — `RegisterCustomComponent(ComponentType)`：
   已存在时静默返回 false（层级重绘本来就会反复走这条路径），不再 `LogError`。
4. `Scripts/Editor/ComponentItem.cs` — `Content` / `DisplayName` / `IsBuiltIn` / `HasToggle`
   与 `UpdateActiveState()` 对 `Type == null` 兜底；`GetActiveState()` 对 `ToggleProperty == null` 兜底。
5. `Scripts/Editor/Hierarchy/Info/ComponentIconInfo.cs` — `CanShow()`：`Type == null` 时直接不画。

## 更新上游时

拉新版本后按上面 5 处重新对齐；如果上游已修（#134 关闭且代码里有等效逻辑），可以直接删掉本目录、
把 `Packages/manifest.json` 的 git 依赖加回来。

## 注意

上游仓库的 `AGENTS.md` 要求提交 PR / issue 时不要使用 AI 生成的代码与评论，所以这些补丁只用于本项目
本地，不要原样提给上游。
