---
description: 改动 Yarn 台词、对话触发或 3D 飘字时使用。
---

# 剧情对话与 3D 飘字

改动本域代码前通读本文件；项目级约束见根 `AGENTS.md`，同目录其他条目按需加载。

## Yarn Spinner 与 3D 飘字

Yarn Spinner 3.2.8 + `Assets/Scripts/Dialogue`（`FloatingDialoguePresenter` 决定显示 3D 字还是对话框、`DialogueTrigger`、`WorldLineView`、`FloatingLine`、`DialogueBoxView`、`Dialogue3DOverlay`）；

对话文本改 `Assets/Yarn/*.yarn`，不要自己写解析器；

3D 字必须在 `Dialogue3D` 层（由 `Dialogue3DCamera` 全分辨率叠加，主相机 `cullingMask` 排除该层）。

"走进去就播一段剧情"用 `DialogueZoneTrigger`（放在触发区上，配 `DialogueTrigger` + `BoxCollider`）；

`Assets/Prefabs/Dialogue/FloatingLine.prefab` 的 root 缩放必须是 0.02，改成 0.08 会让一句字宽到 100 m。

`WorldLineView.minReadableDistance`（默认 1.2 m）决定"太近就不用 3D 字改用对话框"——锚点贴脸时字会糊满屏幕。

`DialogueTrigger` 用静态标记让全局同一时刻只跑一段 Yarn 剧情：剧情播放中再触发另一段不会被打断，新请求排队等当前这段播完再开始（同一对象重复请求会被去重）

## 台词换行写法

Yarn 台词要换行时写 `<br>`（TMP 富文本标签，逐字显示时也能正确换行）；

**不要写 `\n`**——Yarn 3 会在编译期报 `Syntax error: Unexpected "n" while reading a line statement`，整份 `.yarn` 编译失败、`YarnProject.Program.Nodes` 变空，运行时报 `Cannot load node ...: No nodes have been loaded`。
