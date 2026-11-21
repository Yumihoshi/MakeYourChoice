---
description: 改动编辑器工具、层级美化包或编辑器增强用法时使用。
---

# 编辑器工具

改动本域代码前通读本文件；项目级约束见根 `AGENTS.md`，同目录其他条目按需加载。

## 编辑器增强

Odin Inspector、vFolders（编辑器工具，运行时代码不要依赖）

## 层级窗口美化（HierarchyDecorator）

本地嵌入包 `Packages/com.wooshii.hierarchydecorator`（上游 0.12.1 / commit 599e13a，打了 5 处补丁，清单见 `Packages/com.wooshii.hierarchydecorator/PATCHES.md`）。

**不要改回 `Packages/manifest.json` 的 git 依赖**：上游对 Cinemachine 这类包内脚本取不到图标就把条目判为无效，每次层级重绘都报 `Attempted to register a component that already exists`，并在 `ComponentItem.UpdateActiveState` 抛 `NullReferenceException`（上游 issue #134 未修）。
