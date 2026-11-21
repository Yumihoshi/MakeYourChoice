---
description: 改动传送门/空间叠加或瞬移行为时使用。
---

# 传送门（空间叠加）

改动本域代码前通读本文件；项目级约束见根 `AGENTS.md`，同目录其他条目按需加载。

## PortalPair / PortalSide

`Assets/Scripts/Level/Portal`：`PortalPair`（`[DefaultExecutionOrder(1000)]`，管两张传送面的相机、斜近裁剪、瞬移与 RT 生命周期）与 `PortalSide`（quad/相机/裁剪面/材质/运行时 RT，`FrontNormal => -quad.forward`），设计与实施见 `docs/superpowers/specs/2026-11-19-space-stacking-design.md`。

安全门 `SafeDoorInteract.exitOpened` 调 `PortalPair.Activate()` 后才建两张 320×180 RT 并启用 quad/相机；

玩家在传送面**前方** `triggerDistance`（0.18 m）内、朝门移动且门已完全打开时瞬移，落点在对侧传送面前方 `landingOffset`（0.25 m），朝向保持不变（注意 `triggerDistance < landingOffset`，才不会落地就立刻被弹回）。

**触发前还要用 `spaceHeightTolerance`（默认 3 m）确认玩家与源传送面处于同一空间**：口袋空间与大厅在水平面上完全重叠、只隔 1000 m 竖直距离，少了这一步，玩家在走廊里走到 z≈1.22..1.4 会误触发大厅侧传送，落点 y 每次再叠 -1000 m（实测被"传"到 y≈-3000 后自由落体）。

Scene 视图选中 `空间叠加` 会画出白（传送面）/橙（触发距离）/绿（落点前移）三张面并标注数值，`showGuideGizmos` 可关；

传送相机用目的侧裁剪面做斜近裁剪，且只在 quad 进主相机视锥时渲染
