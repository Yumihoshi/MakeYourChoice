# 泳池房流程逻辑 实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 在不改泳池房几何的前提下，把更衣室 00 暗门 → 轮/长池退水 → 池底钥匙 → 37 号柜扳手 → 主阀/池核退水 → 地漏下坠这 8 步接成可玩流程。

**架构：** 每个场景实例挂一个 `PoolFlowState` 记录只读布尔；交互脚本持同一引用做门控，状态由 C# 先 Mark 再调 `DialogueTrigger.StartDialogue()`。复用 `InteractableBase`（新增按住钩子）、`CabinetDoorInteract`（新增锁模式）、`DialogueTrigger`，不新增事件总线、不做存档/背包 UI。

**技术栈：** Unity 2022.3.62f3c1、URP 14、QFramework、Yarn Spinner 3.2.8、UniTask、DOTween、Odin Inspector、Unity MCP。

**规格：** `docs/superpowers/specs/2026-11-21-pool-room-flow-design.html`（实现时以该 HTML 为准；本计划是分步落地记录）。

> **Git 约定：** 本项目全局 `AGENTS.md` 只允许查询类 git 操作。下面的“检查点”**不包含** `git add/commit`；每个任务结束由用户或主控决定是否提交。代理不要执行任何 git 写操作。

---

## 文件结构

### 新增

| 文件 | 职责 |
| --- | --- |
| `Assets/Scripts/Level/Pool/PoolFlowState.cs` | 11 个只读流程状态 + `Mark/Take` 方法 |
| `Assets/Scripts/Level/Pool/PoolStoryTrigger.cs` | 触发区一次性剧情：泵平台提示、暗门内侧“要扳手” |
| `Assets/Scripts/Level/Pool/PoolNumberSightTrigger.cs` | 池底 00 的视口/角度/距离/观察区判定；解锁并打开暗门双门 |
| `Assets/Scripts/Level/Pool/PoolPickupInteract.cs` | 轮 / 钥匙 / 扳手共用拾取 |
| `Assets/Scripts/Level/Pool/PoolValveWheelInteract.cs` | 长池阀：装轮 + 按住转 3 圈 + 触发长池退水 |
| `Assets/Scripts/Level/Pool/PoolMainValveInteract.cs` | 主阀：无扳手提示；有扳手启动并触发池核退水 |
| `Assets/Scripts/Level/Pool/PoolWaterDrain.cs` | 两次退水：水面下降、禁用、关空气墙、置状态、播完成台词 |
| `Assets/Scripts/Level/Pool/PoolDrainExitTrigger.cs` | 竖井底一次性触发，发下一关 UnityEvent |
| `Assets/Prefabs/Props/Key.prefab` | 池底钥匙预制体（由 `key.fbx` 处理） |
| `Assets/Prefabs/Props/Pump.prefab` | 抽水泵预制体（由 `pumpa_LP.obj` 处理） |

### 修改

| 文件 | 改动 |
| --- | --- |
| `Assets/Scripts/Interaction/InteractableBase.cs` | 加 `IsHoldInteraction` / `OnInteractHeld(float)`，`Update` 分流“按住/按下” |
| `Assets/Scripts/Interaction/CabinetDoorInteract.cs` | 加 `LockMode`、`lockedDialogue`、`Unlock()`、`Open()`；`ToggleAsync` 改 `SetOpenAsync(bool)` |
| `Assets/Yarn/Level2.yarn` | 追加 12 个 `LV2_Pool_*` 节点 |
| `Assets/Scenes/Level2.unity` | 复制 `= 对话`、加流程对象、接线、测试出生点 |
| `Assets/Scenes/Level1.unity` | 在 `口袋空间/TestRoom` 实例下接同一套流程 |

### 新建对象（预制体或场景）

| 对象 | 用途 |
| --- | --- |
| `泳池房/流程` | 流程控制根 |
| `泳池房/流程/退水控制` | `PoolWaterDrain` |
| `泳池房/流程/剧情` | 12 个 `DialogueTrigger` 的常驻载体 |
| `泳池房/流程/触发_泵平台` | `PoolStoryTrigger(PumpHint)` + BoxCollider |
| `泳池房/流程/触发_暗门内侧` | `PoolStoryTrigger(NeedWrench)` + BoxCollider |
| `泳池房/流程/观察区_池底数字` | 覆盖泳池区的 BoxCollider，供 `PoolNumberSightTrigger` 用 |
| `泳池房/剧情锚点/交互点_轮` | 轮的原位常驻飘字锚点 |
| `泳池房/剧情锚点/交互点_钥匙` | 钥匙的原位常驻飘字锚点 |
| `泳池房/剧情锚点/交互点_37柜` | 37 柜前的常驻飘字锚点 |
| `泳池房/道具/阀_长池` | 长池阀交互根 + BoxCollider（`PoolValveWheelInteract` 在任务 5 的场景实例上加） |
| `泳池房/建筑/竖井` | 地洞下四壁 + 底地板 + 触发区 |

---

## 任务 1：流程状态、按住钩子与柜门锁

**文件：**
- 创建：`Assets/Scripts/Level/Pool/PoolFlowState.cs`
- 修改：`Assets/Scripts/Interaction/InteractableBase.cs`
- 修改：`Assets/Scripts/Interaction/CabinetDoorInteract.cs`

- [ ] **步骤 1：创建 `PoolFlowState.cs`**

```csharp
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 泳池房八步流程的只读状态。只由各交互点置位，不广播事件、不存档。
    /// </summary>
    public sealed class PoolFlowState : MonoBehaviour
    {
        public bool PumpHintHeard { get; private set; }
        public bool PoolNumberSeen { get; private set; }
        public bool WrenchNeedLearned { get; private set; }
        public bool HasWheel { get; private set; }
        public bool WheelInstalled { get; private set; }
        public bool LongPoolDrained { get; private set; }
        public bool HasKey { get; private set; }
        public bool HasWrench { get; private set; }
        public bool MainValveOpened { get; private set; }
        public bool CoreDrained { get; private set; }
        public bool NextLevelReached { get; private set; }

        public void MarkPumpHintHeard() => PumpHintHeard = true;
        public void MarkPoolNumberSeen() => PoolNumberSeen = true;
        public void MarkWrenchNeedLearned() => WrenchNeedLearned = true;
        public void TakeWheel() => HasWheel = true;
        public void MarkWheelInstalled() => WheelInstalled = true;
        public void MarkLongPoolDrained() => LongPoolDrained = true;
        public void TakeKey() => HasKey = true;
        public void TakeWrench() => HasWrench = true;
        public void MarkMainValveOpened() => MainValveOpened = true;
        public void MarkCoreDrained() => CoreDrained = true;
        public void MarkNextLevelReached() => NextLevelReached = true;
    }
}
```

- [ ] **步骤 2：改 `InteractableBase.cs`，加按住钩子**

把 `Update()` 改为下面的版本，并在 `OnInteract` 抽象方法后加两个虚成员：

```csharp
        void Update()
        {
            if (!IsAimed())
            {
                InteractFocus.Clear(this);
                return;
            }

            InteractFocus.Set(this);

            var input = PlayerInputRouter.Instance;
            if (input == null)
            {
                return;
            }

            if (IsHoldInteraction)
            {
                if (input.Interact.IsPressed())
                {
                    OnInteractHeld(Time.deltaTime);
                }
            }
            else if (input.Interact.WasPressedThisFrame())
            {
                OnInteract();
            }
        }

        /// <summary>为 true 时走按住分支，由子类在 <see cref="OnInteractHeld"/> 里累计。</summary>
        protected virtual bool IsHoldInteraction => false;

        /// <summary>按住交互键期间每帧调用；只有 <see cref="IsHoldInteraction"/> 为 true 时才会走到。</summary>
        protected virtual void OnInteractHeld(float deltaTime) { }
```

- [ ] **步骤 3：改 `CabinetDoorInteract.cs`，加锁模式**

整个文件替换为：

```csharp
using System;
using Cysharp.Threading.Tasks;
using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 柜门的交互：玩家对准门板按交互键，门绕自身铰链在关闭与打开角度之间缓慢旋转。
    /// 只做视觉与阻挡，不参与物理推挤；转动期间把玩家层排除在门板碰撞之外，避免门把玩家顶开。
    /// </summary>
    public sealed class CabinetDoorInteract : InteractableBase
    {
        public enum LockMode
        {
            Unlocked,
            SilentLocked,
            FeedbackLocked
        }

        [SerializeField, Required, LabelText("铰链节点")]
        Transform hinge;

        [SerializeField, LabelText("开门角度"), Tooltip("绕铰链本地 Y 轴旋转的角度，正负决定开向")]
        float openAngle = 90f;

        [SerializeField, LabelText("开合时长")]
        float duration = 0.45f;

        [SerializeField, LabelText("初始锁状态")]
        LockMode lockMode = LockMode.Unlocked;

        [SerializeField, ShowIf(nameof(lockMode), LockMode.FeedbackLocked), LabelText("上锁提示剧情")]
        DialogueTrigger lockedDialogue;

        Collider _panelCollider;
        Quaternion _closedRotation;
        bool _open;
        bool _moving;
        bool _locked;

        /// <summary>锁着时是否仍然显示准心并给短句反馈。</summary>
        bool IsFeedbackLocked => _locked && lockMode == LockMode.FeedbackLocked;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            if (hinge == null)
            {
                Debug.LogError("CabinetDoorInteract 缺少铰链节点引用", this);
                enabled = false;
                return;
            }

            _closedRotation = hinge.localRotation;
            _panelCollider = GetComponentInChildren<Collider>();
            _locked = lockMode != LockMode.Unlocked;

            if (IsFeedbackLocked && lockedDialogue == null)
            {
                Debug.LogError("CabinetDoorInteract 配置为 FeedbackLocked，但没有上锁提示剧情", this);
            }
        }

        protected override bool CanInteract()
        {
            if (hinge == null || _moving)
            {
                return false;
            }

            return !(_locked && lockMode == LockMode.SilentLocked);
        }

        protected override void OnInteract()
        {
            if (_locked)
            {
                if (IsFeedbackLocked && lockedDialogue != null)
                {
                    lockedDialogue.StartDialogue();
                }

                return;
            }

            SetOpenAsync(!_open).Forget();
        }

        /// <summary>解锁；之后由玩家自己开门，或由剧情调用 <see cref="Open"/>。</summary>
        public void Unlock() => _locked = false;

        /// <summary>解锁后由剧情直接打开；已开或正在转时忽略。</summary>
        public void Open()
        {
            if (_locked || _open || _moving || hinge == null)
            {
                return;
            }

            SetOpenAsync(true).Forget();
        }

        async UniTaskVoid SetOpenAsync(bool open)
        {
            _moving = true;
            var target = open ? _closedRotation * Quaternion.Euler(0f, openAngle, 0f) : _closedRotation;
            var from = hinge.localRotation;
            var originalExclude = _panelCollider != null ? _panelCollider.excludeLayers : default;

            if (_panelCollider != null)
            {
                _panelCollider.excludeLayers = PlayerLayerMask();
            }

            try
            {
                for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
                {
                    var t = Mathf.Clamp01(elapsed / duration);
                    hinge.localRotation = Quaternion.SlerpUnclamped(from, target, t * t * (3f - 2f * t));
                    await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken);
                }

                hinge.localRotation = target;
            }
            catch (OperationCanceledException)
            {
                // 物件在转动途中被销毁 / 禁用，直接停止即可。
            }
            finally
            {
                if (_panelCollider != null)
                {
                    _panelCollider.excludeLayers = originalExclude;
                }

                _open = open;
                _moving = false;
            }
        }

        static LayerMask PlayerLayerMask()
        {
            var layer = LayerMask.NameToLayer("Player");
            if (layer < 0)
            {
                Debug.LogWarning("找不到 Player 层，柜门转动时无法放行玩家");
                return 0;
            }

            return 1 << layer;
        }
    }
}
```

> 注意：`FeedbackLocked` 缺台词时只 `LogError`，**不要** `enabled=false`。否则钥匙解锁后组件不再更新，37 门永远打不开；报错已足够暴露配置问题。

- [ ] **步骤 4：编译验证**

在 Unity 里等待编译结束，或用 MCP 读 Console：

```text
read_console(types: ["error"], count: 20)
```

预期：0 条 `PoolFlowState` / `InteractableBase` / `CabinetDoorInteract` 相关 error。若 `InteractableBase` 的 `Update` 是 private，确认仍能编译；它不需要改成 public。

- [ ] **步骤 5：检查点**

把改动文件列给用户看；不执行 git 写操作。

---

## 任务 2：拾取、长池阀与主阀

**文件：**
- 创建：`Assets/Scripts/Level/Pool/PoolPickupInteract.cs`
- 创建：`Assets/Scripts/Level/Pool/PoolValveWheelInteract.cs`
- 创建：`Assets/Scripts/Level/Pool/PoolMainValveInteract.cs`

- [ ] **步骤 1：创建 `PoolPickupInteract.cs`**

```csharp
using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>轮 / 钥匙 / 扳手共用的拾取交互；按流程状态决定能不能点。</summary>
    public sealed class PoolPickupInteract : InteractableBase
    {
        public enum Kind
        {
            Wheel,
            Key,
            Wrench
        }

        [SerializeField, LabelText("拾取物")]
        Kind kind;

        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, Required, LabelText("拾取剧情")]
        DialogueTrigger dialogue;

        [SerializeField, LabelText("拾取后解锁的柜门")]
        CabinetDoorInteract[] unlockDoors;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            if (state == null)
            {
                Debug.LogError("PoolPickupInteract 缺少流程状态引用", this);
                enabled = false;
                return;
            }

            if (dialogue == null)
            {
                Debug.LogError("PoolPickupInteract 缺少拾取剧情", this);
                enabled = false;
            }
        }

        protected override bool CanInteract()
        {
            if (state == null)
            {
                return false;
            }

            return kind switch
            {
                Kind.Wheel => state.WrenchNeedLearned && !state.HasWheel && !state.WheelInstalled,
                Kind.Key => state.LongPoolDrained && !state.HasKey,
                Kind.Wrench => state.HasKey && !state.HasWrench,
                _ => false
            };
        }

        protected override void OnInteract()
        {
            if (state == null || dialogue == null)
            {
                return;
            }

            switch (kind)
            {
                case Kind.Wheel:
                    state.TakeWheel();
                    break;
                case Kind.Key:
                    state.TakeKey();
                    if (unlockDoors != null)
                    {
                        foreach (var door in unlockDoors)
                        {
                            if (door != null)
                            {
                                door.Unlock();
                            }
                        }
                    }

                    break;
                case Kind.Wrench:
                    state.TakeWrench();
                    break;
            }

            dialogue.StartDialogue();
            gameObject.SetActive(false);
        }
    }
}
```

- [ ] **步骤 2：创建 `PoolValveWheelInteract.cs`**

```csharp
using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 长池阀：先左键把轮装上，再按住左键累计转到 3 圈，触发长池退水。
    /// </summary>
    public sealed class PoolValveWheelInteract : InteractableBase
    {
        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, Required, LabelText("已安装的阀轮")]
        Transform wheel;

        [SerializeField, Required, LabelText("旋转轴")]
        Transform rotateAxis;

        [SerializeField, Required, LabelText("拾取物视觉")]
        GameObject pickupVisual;

        [SerializeField, Required, LabelText("退水控制")]
        PoolWaterDrain drain;

        [SerializeField, Required, LabelText("安装剧情")]
        DialogueTrigger installDialogue;

        [SerializeField, LabelText("旋转速度（度/秒）")]
        float degreesPerSecond = 240f;

        [SerializeField, LabelText("需要旋转角度")]
        float requiredDegrees = 1080f;

        float _degrees;
        bool _draining;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            if (state == null || wheel == null || rotateAxis == null || pickupVisual == null || drain == null
                || installDialogue == null)
            {
                Debug.LogError("PoolValveWheelInteract 缺少流程状态 / 轮 / 旋转轴 / 拾取物 / 退水控制 / 安装剧情引用", this);
                enabled = false;
            }
        }

        protected override bool CanInteract()
        {
            return state != null && !state.LongPoolDrained && !_draining
                   && (state.HasWheel || state.WheelInstalled);
        }

        protected override bool IsHoldInteraction
        {
            get { return state != null && state.WheelInstalled && !state.LongPoolDrained && !_draining; }
        }

        protected override void OnInteract()
        {
            if (state == null || state.WheelInstalled)
            {
                return;
            }

            if (wheel == null || pickupVisual == null || installDialogue == null)
            {
                Debug.LogError("PoolValveWheelInteract 缺少轮 / 拾取物 / 安装剧情引用", this);
                return;
            }

            state.MarkWheelInstalled();
            pickupVisual.SetActive(false);
            wheel.gameObject.SetActive(true);
            installDialogue.StartDialogue();
        }

        protected override void OnInteractHeld(float deltaTime)
        {
            if (state == null || wheel == null || rotateAxis == null || drain == null)
            {
                return;
            }

            var step = degreesPerSecond * deltaTime;
            wheel.Rotate(rotateAxis.forward, step, Space.World);
            _degrees += step;

            if (_degrees < requiredDegrees)
            {
                return;
            }

            _draining = true;
            drain.DrainLongPool();
        }
    }
}
```

- [ ] **步骤 3：创建 `PoolMainValveInteract.cs`**

```csharp
using System;
using Cysharp.Threading.Tasks;
using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 主阀：没扳手只播提示；有扳手时轮转一圈，随后池核退水。
    /// </summary>
    public sealed class PoolMainValveInteract : InteractableBase
    {
        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, Required, LabelText("主阀轮")]
        Transform valveWheel;

        [SerializeField, Required, LabelText("旋转轴")]
        Transform rotateAxis;

        [SerializeField, Required, LabelText("退水控制")]
        PoolWaterDrain drain;

        [SerializeField, Required, LabelText("无扳手剧情")]
        DialogueTrigger noWrenchDialogue;

        [SerializeField, Required, LabelText("启动剧情")]
        DialogueTrigger openDialogue;

        [SerializeField, LabelText("启动时长")]
        float openDuration = 1.2f;

        bool _busy;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            if (state == null || valveWheel == null || rotateAxis == null || drain == null)
            {
                Debug.LogError("PoolMainValveInteract 缺少流程状态 / 主阀轮 / 旋转轴 / 退水控制引用", this);
                enabled = false;
            }
        }

        protected override bool CanInteract()
        {
            return state != null && !state.CoreDrained && !_busy;
        }

        protected override void OnInteract()
        {
            if (state == null)
            {
                return;
            }

            if (!state.HasWrench)
            {
                if (noWrenchDialogue != null)
                {
                    noWrenchDialogue.StartDialogue();
                }
                else
                {
                    Debug.LogError("PoolMainValveInteract 缺少无扳手剧情", this);
                }

                return;
            }

            if (_busy || drain == null || valveWheel == null || rotateAxis == null)
            {
                Debug.LogError("PoolMainValveInteract 缺少主阀轮 / 旋转轴 / 退水控制引用", this);
                return;
            }

            _busy = true;
            if (openDialogue != null)
            {
                openDialogue.StartDialogue();
            }
            else
            {
                Debug.LogError("PoolMainValveInteract 缺少启动剧情", this);
            }

            OpenAsync().Forget();
        }

        async UniTaskVoid OpenAsync()
        {
            try
            {
                var axis = rotateAxis.forward;
                var duration = Mathf.Max(0.01f, openDuration);
                for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
                {
                    var step = 360f * Time.deltaTime / duration;
                    valveWheel.Rotate(axis, step, Space.World);
                    await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken);
                }

                state.MarkMainValveOpened();
                drain.DrainCorePool();
            }
            catch (OperationCanceledException)
            {
                // 场景卸载时静默退出。
            }
        }
    }
}
```

- [ ] **步骤 4：编译验证**

```text
read_console(types: ["error"], count: 20)
```

预期：0 条 error。重点检查 `PoolValveWheelInteract.CanInteract` 是否同时接受 `HasWheel` 与 `WheelInstalled`；否则装上轮后按住无效。

- [ ] **步骤 5：检查点**

不执行 git 写操作；把新增文件列给用户。

---

## 任务 3：退水、竖井与剧情触发

**文件：**
- 创建：`Assets/Scripts/Level/Pool/PoolWaterDrain.cs`
- 创建：`Assets/Scripts/Level/Pool/PoolDrainExitTrigger.cs`
- 创建：`Assets/Scripts/Level/Pool/PoolStoryTrigger.cs`
- 创建：`Assets/Scripts/Level/Pool/PoolNumberSightTrigger.cs`

- [ ] **步骤 1：创建 `PoolWaterDrain.cs`**

```csharp
using System;
using Cysharp.Threading.Tasks;
using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 一次退水：水面本地 y 在 duration 内降到 lowerY，随后禁用；同一帧关空气墙。
    /// 长池和池核各调用一次，互不影响。
    /// </summary>
    public sealed class PoolWaterDrain : MonoBehaviour
    {
        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, LabelText("长池水面")]
        Transform[] longPoolSurfaces;

        [SerializeField, LabelText("长池空气墙")]
        Collider[] longPoolBlockers;

        [SerializeField, LabelText("池核水面")]
        Transform[] coreSurfaces;

        [SerializeField, LabelText("池核空气墙")]
        Collider[] coreBlockers;

        [SerializeField, LabelText("退水时长")]
        float duration = 2f;

        [SerializeField, LabelText("退水后水面高度")]
        float lowerY = -1.55f;

        [SerializeField, LabelText("长池退水剧情")]
        DialogueTrigger longPoolDialogue;

        [SerializeField, LabelText("池核退水剧情")]
        DialogueTrigger coreDialogue;

        bool _longDraining;
        bool _coreDraining;

        public void DrainLongPool()
        {
            if (state == null)
            {
                Debug.LogError("PoolWaterDrain 缺少流程状态引用", this);
                return;
            }

            if (_longDraining || state.LongPoolDrained)
            {
                return;
            }

            _longDraining = true;
            DrainAsync(true).Forget();
        }

        public void DrainCorePool()
        {
            if (state == null)
            {
                Debug.LogError("PoolWaterDrain 缺少流程状态引用", this);
                return;
            }

            if (_coreDraining || state.CoreDrained)
            {
                return;
            }

            _coreDraining = true;
            DrainAsync(false).Forget();
        }

        async UniTaskVoid DrainAsync(bool longPool)
        {
            var surfaces = longPool ? longPoolSurfaces : coreSurfaces;
            var blockers = longPool ? longPoolBlockers : coreBlockers;
            var dialogue = longPool ? longPoolDialogue : coreDialogue;

            if (surfaces == null || surfaces.Length == 0)
            {
                Debug.LogError("PoolWaterDrain 没有配置水面", this);
                return;
            }

            if (blockers == null || blockers.Length == 0)
            {
                Debug.LogError("PoolWaterDrain 没有配置空气墙", this);
            }

            try
            {
                var starts = new float[surfaces.Length];
                for (var i = 0; i < surfaces.Length; i++)
                {
                    starts[i] = surfaces[i] != null ? surfaces[i].localPosition.y : lowerY;
                }

                var d = Mathf.Max(0.01f, duration);
                for (var elapsed = 0f; elapsed < d; elapsed += Time.deltaTime)
                {
                    var k = Mathf.Clamp01(elapsed / d);
                    for (var i = 0; i < surfaces.Length; i++)
                    {
                        if (surfaces[i] == null)
                        {
                            continue;
                        }

                        var p = surfaces[i].localPosition;
                        p.y = Mathf.Lerp(starts[i], lowerY, k);
                        surfaces[i].localPosition = p;
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken);
                }

                foreach (var surface in surfaces)
                {
                    if (surface == null)
                    {
                        continue;
                    }

                    var p = surface.localPosition;
                    p.y = lowerY;
                    surface.localPosition = p;
                    surface.gameObject.SetActive(false);
                }

                if (blockers != null)
                {
                    foreach (var blocker in blockers)
                    {
                        if (blocker != null)
                        {
                            blocker.gameObject.SetActive(false);
                        }
                    }
                }

                if (longPool)
                {
                    state.MarkLongPoolDrained();
                }
                else
                {
                    state.MarkCoreDrained();
                }

                if (dialogue != null)
                {
                    dialogue.StartDialogue();
                }
                else
                {
                    Debug.LogError("PoolWaterDrain 缺少退水完成剧情", this);
                }
            }
            catch (OperationCanceledException)
            {
                // 场景卸载时静默退出。
            }
        }
    }
}
```

- [ ] **步骤 2：创建 `PoolDrainExitTrigger.cs`**

```csharp
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Level
{
    /// <summary>竖井底的一次性触发；只发下一关事件，不做加载/淡出。</summary>
    public sealed class PoolDrainExitTrigger : MonoBehaviour
    {
        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, LabelText("下一关事件")]
        UnityEvent onNextLevel;

        bool _fired;

        void OnTriggerEnter(Collider other)
        {
            if (_fired)
            {
                return;
            }

            if (state == null)
            {
                Debug.LogError("PoolDrainExitTrigger 缺少流程状态引用", this);
                return;
            }

            if (!state.CoreDrained || !other.CompareTag("Player") || !GameCursorState.IsPlaying)
            {
                return;
            }

            _fired = true;
            state.MarkNextLevelReached();
            onNextLevel?.Invoke();
        }
    }
}
```

- [ ] **步骤 3：创建 `PoolStoryTrigger.cs`**

```csharp
using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>区域触发的一次性剧情：泵平台提示、暗门内侧“要扳手”。</summary>
    public sealed class PoolStoryTrigger : MonoBehaviour
    {
        public enum Beat
        {
            PumpHint,
            NeedWrench
        }

        [SerializeField, LabelText("剧情节点")]
        Beat beat;

        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, Required, LabelText("剧情")]
        DialogueTrigger dialogue;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || !GameCursorState.IsPlaying)
            {
                return;
            }

            if (state == null || dialogue == null)
            {
                Debug.LogError("PoolStoryTrigger 缺少流程状态或剧情引用", this);
                return;
            }

            if (beat == Beat.PumpHint)
            {
                if (state.PumpHintHeard)
                {
                    return;
                }

                state.MarkPumpHintHeard();
            }
            else
            {
                if (state.WrenchNeedLearned)
                {
                    return;
                }

                state.MarkWrenchNeedLearned();
            }

            dialogue.StartDialogue();
        }
    }
}
```

- [ ] **步骤 4：创建 `PoolNumberSightTrigger.cs`**

```csharp
using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 池底数字的视线判定：玩家在观察区内、距离够近、数字在视口内且水平角对准时触发一次。
    /// 刻意不用 ScreenVisibility：池边空气墙的 Collider 会挡住它的 Physics.Linecast。
    /// </summary>
    public sealed class PoolNumberSightTrigger : MonoBehaviour
    {
        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, LabelText("视线起点（玩家眼睛）")]
        Transform eye;

        [SerializeField, LabelText("判定相机")]
        Camera viewCamera;

        [SerializeField, Required, LabelText("池底数字")]
        Transform target;

        [SerializeField, Required, LabelText("观察区")]
        BoxCollider observationArea;

        [SerializeField, Required, LabelText("剧情")]
        DialogueTrigger dialogue;

        [SerializeField, LabelText("要解锁的柜门")]
        CabinetDoorInteract[] unlockDoors;

        [SerializeField, LabelText("可视距离")]
        float maxDistance = 15f;

        [SerializeField, LabelText("对准角度")]
        float maxAngle = 20f;

        bool _fired;

        void Awake()
        {
            if (state == null || target == null || observationArea == null || dialogue == null)
            {
                Debug.LogError("PoolNumberSightTrigger 缺少流程状态 / 池底数字 / 观察区 / 剧情引用", this);
                enabled = false;
                return;
            }

            var mainCamera = Camera.main;
            if (eye == null)
            {
                if (mainCamera == null)
                {
                    Debug.LogError("PoolNumberSightTrigger 的视线起点为空，且场景里没有 Main Camera", this);
                    enabled = false;
                    return;
                }

                eye = mainCamera.transform;
            }

            if (viewCamera == null)
            {
                viewCamera = mainCamera;
                if (viewCamera == null)
                {
                    Debug.LogError("PoolNumberSightTrigger 的判定相机为空，且场景里没有 Main Camera", this);
                    enabled = false;
                    return;
                }
            }

            if (unlockDoors == null || unlockDoors.Length == 0)
            {
                Debug.LogError("PoolNumberSightTrigger 没有配置要解锁的暗门", this);
            }
        }

        void Update()
        {
            if (_fired || state == null || target == null || observationArea == null || dialogue == null)
            {
                return;
            }

            if (state.PoolNumberSeen || dialogue.IsRunning || !GameCursorState.IsPlaying)
            {
                return;
            }

            if (!observationArea.bounds.Contains(eye.position))
            {
                return;
            }

            if (Vector3.Distance(eye.position, target.position) > maxDistance)
            {
                return;
            }

            var viewport = viewCamera.WorldToViewportPoint(target.position);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
            {
                return;
            }

            var toTarget = target.position - eye.position;
            var flatForward = eye.forward;
            toTarget.y = 0f;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.0001f || toTarget.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (Vector3.Angle(flatForward, toTarget) > maxAngle)
            {
                return;
            }

            _fired = true;
            state.MarkPoolNumberSeen();

            if (unlockDoors != null)
            {
                foreach (var door in unlockDoors)
                {
                    if (door != null)
                    {
                        door.Unlock();
                        door.Open();
                    }
                }
            }

            dialogue.StartDialogue();
        }
    }
}
```

- [ ] **步骤 5：编译验证**

```text
read_console(types: ["error"], count: 20)
```

预期：0 条 error。检查 `PoolNumberSightTrigger` 没有引用 `ScreenVisibility`。

- [ ] **步骤 6：检查点**

不执行 git 写操作；把新增文件列给用户。

---

## 任务 4：场景道具、碰撞与几何（改预制体）

**文件/场景：**
- 修改：`Assets/Prefabs/Level/泳池房.prefab`（在 Prefab Mode 里改）
- 创建：`Assets/Prefabs/Props/Key.prefab`
- 创建：`Assets/Prefabs/Props/Pump.prefab`

> **只改预制体：** 下面的几何、道具、碰撞都加在 `泳池房.prefab` 里，Level2 的 `泳池房` 实例与 Level1 的 `TestRoom` 实例会自动继承。**交互组件与场景引用**（`PoolPickupInteract`、`PoolValveWheelInteract`、`PoolMainValveInteract`、`PoolDrainExitTrigger`、`CabinetDoorInteract.lockedDialogue`）一律留到任务 5/6 在各自场景实例上接，不要写进预制体。

> 改完预制体后，如果两个实例的 Inspector 上出现 `Lock Mode` / `Hinge` / `Open Angle` 之类的 **override**，选中该组件右键 `Revert`（或手动改成与预制体一致）；否则预制体的锁门设置不会生效。

> **碰撞体归属：** `InteractableBase.IsAimed()` 用的是 `hit.collider.transform.IsChildOf(transform)`，所以 Collider 必须在交互组件所在物体**或它的子物体**上（同一物体也算；`IsChildOf` 包含自身）。不要挂到父级、兄弟或另一个对象上，否则准心永远不会亮。下文凡是写“子物体 `碰撞`”的地方，是为了让模型/碰撞盒好摆，不是因为同一物体不合法。

- [ ] **步骤 1：删掉没有子物体的重复轮**

打开 `Assets/Prefabs/Level/泳池房.prefab`，进入 Prefab Mode，在 `道具` 下找到两个同名 `轮`：

- 保留有 `Valve_part1` 子物体、位置约 `(-15.843, 0.348, 21.935)`、缩放 `0.486` 的那个。
- 删除没有子物体的那个（位置约 `(-17.50, 0.23, 23.30)`）。

预期：`道具` 下只剩一个 `轮`。删除后保存预制体。

- [ ] **步骤 2：给轮加碰撞**

1. 选中 `泳池房/道具/轮`，确认它的子物体 `Valve_part1` 有 `MeshRenderer`。
2. 给 `Valve_part1` **加** `SphereCollider`：`Center (0,0,0)`、`Radius 0.28`（局部尺寸，父级 0.486 缩放会自动带上）。也可以加在 `轮` 根上；这里放在子物体上是为了跟模型缩放对齐。只要 Collider 在 `PoolPickupInteract` 所在物体或其子物体上就能被 `IsAimed()` 命中。
3. **不要**在 `轮` 根下建飘字锚点：拾取后 `轮` 会被 `SetActive(false)`，锚点会一起消失。`交互点_轮` 统一在任务 5 步骤 5 里挂到常驻的 `泳池房/剧情锚点` 下。`PoolPickupInteract` 也在任务 5 步骤 9 在场景实例上添加，不写进预制体。

- [ ] **步骤 3：配置 37 号柜的锁门与扳手**

1. 选中 `泳池房/道具/储物柜_37/Door_Front`，加 `CabinetDoorInteract`：
   - `Hinge` = `储物柜_37/Hinge_FrontDoor`
   - `Open Angle` = `-90`
   - `Duration` = `0.45`
   - `Lock Mode` = `FeedbackLocked`
   - `Locked Dialogue` = **留空**（预制体不能引用场景里的剧情；任务 5 在 Level2 实例上指到 `剧情_37锁着`，任务 6 在 Level1 实例上指到本地同名剧情）
2. 检查 `Door_Front` 自身或子物体上有没有 Collider（当前门板自带 `MeshCollider`）；只有都没有时才新建空物体 `碰撞`，加 `BoxCollider`：`Center (0,0,0.02)`、`Size (0.44,1.05,0.04)`。
3. 把 `泳池房/道具/扳手` 从 `(-11.5,0.02,14.90)` 移到 `(-11.05,0.85,14.45)`，旋转 `(0,0,90)`，让它竖着靠在柜内。缩放保持原值。
4. 给扳手的网格子物体加 `BoxCollider`：`Center (0,0,0)`、`Size (0.08,0.45,0.08)`；如果模型已有 MeshCollider，直接用，不要重复。

- [ ] **步骤 4：清理 `储物柜_54` 的重复柜门组件**

`储物柜_54` 每扇门现在有两个 `CabinetDoorInteract`。在 Prefab Mode 里手动处理：

1. 选中 `泳池房/道具/储物柜_54/Door_Front`，在 Inspector 里找到两个 `CabinetDoorInteract`，保留 `Hinge=Hinge_FrontDoor`、`Open Angle=-90` 的那个，另一个右键 `Remove Component`。
2. 对 `Door_Back` 做同样的事，保留 `Hinge=Hinge_BackDoor`、`Open Angle=+90` 的那个。
3. `Duration` 都保持 `0.45`。

> 不要在这一步用 `GameObject.Find` 批量删组件：Prefab Mode 打开时它可能抓到 Level2 场景里的实例，而不是预制体资产。手动删最稳。

预期：每扇门只剩一个组件。

- [ ] **步骤 5：锁住暗门并加 00 号牌**

1. `泳池房/道具/门_暗门/Door_Front` 与 `Door_Back` 的 `CabinetDoorInteract`：`Lock Mode` 都设成 `SilentLocked`，`Locked Dialogue` 留空。两扇门都按原配置（`-90` / `+90`）。
2. 在 `门_暗门` 下新建 Cube `号牌_00`：
   - 世界位置 `(-20.5, 1.35, 19.20)`
   - 旋转 `(0,180,0)`（朝更衣室，即朝南）
   - 缩放 `(0.42, 0.30, 0.03)`
   - 材质用深色金属（可复用 `Assets/Materials/Props/` 下现有深色材质）
   - 删除它的 `BoxCollider`（号牌只做视觉，不要挡交互射线）
3. 在 `号牌_00` 下新建 3D TextMeshPro `文字_00`：
   - 本地位置 `(0,0,0.03)`（ plaque 旋转 180° 后 +Z 朝南，文字要在这面之外，别嵌进牌子里）
   - 本地旋转 `(0,0,0)`
   - 文本 `00`，字号约 `0.22`，对齐居中
   - 字体 `Assets/Fonts/SourceHanSansCN-Medium SDF.asset`
   - 颜色浅灰；3D TMP 不是 UGUI Graphic，没有 `Raycast Target`，不要加 Collider 即可

- [ ] **步骤 6：抽水泵基座与泵**

1. 在 `泳池房/道具` 下新建 Cube `基座_抽水泵`：
   - 位置 `(-15.8, 0.075, 27.0)`
   - 缩放 `(1.8, 0.15, 1.8)`
   - 保留 `BoxCollider`；材质用池核干区同款混凝土。
2. 用 Unity MCP 导入泵模型：

```text
import_model_file(
  source_path: "Assets/Art/Models/抽水泵/source/pumpa_LP.obj",
  name: "Pump",
  output_folder: "Assets/Prefabs/Props",
  target_size: 1.2,
  animation_type: "none"
)
```

3. 把 `Pump` 实例放到 `道具` 下，位置 `(-15.8, 0.75, 27.0)`（泵底贴基座顶面 y=0.15；若轴心不在中心，按实际包围盒调 y）。旋转让它朝向泳池房中央。
4. 用 `Assets/Art/Models/抽水泵/textures/pumpa_albedo.png`、`pumpa_normal.png`、`pumpa_roughness.png` 建 `Assets/Materials/Props/Pump.mat`（URP/Lit），赋给泵的所有 Renderer；`internal_ground_ao_texture.jpeg` 可忽略。
5. 给泵加一个 `BoxCollider` 或使用导入的 MeshCollider，保证玩家站在平台上能看见、但不挡住射线。泵本身不挂交互脚本。

- [ ] **步骤 7：主阀碰撞**

1. 选中 `泳池房/道具/主阀/PumpLP_part1`。
2. 如果它没有 Collider，给它加 `BoxCollider`：`Center (0,0,0)`、`Size (0.8,0.8,0.8)`（按实际网格微调）。**不要**加在 `主阀` 根上。
3. 保留 `主阀` 根上的 `PumpLP_part1` 模型；不要替换成 `Valve.prefab`（用户已确认 `PumpLP_part1` 就是主阀）。

- [ ] **步骤 8：长池阀接口**

1. 在 `泳池房/道具` 下新建空物体 `阀_长池`：
   - 世界位置 = `管道_轮阀接口/锚点_轮阀安装位` 的世界位置 `(-22.095, 2.18, 37.8)`
   - 世界旋转 `(0,90,0)`（forward = +X，朝房间内）
2. 在 `阀_长池` 下新建空物体 `碰撞`，加 `BoxCollider`：`Center (0,0,0)`、`Size (0.6,0.6,0.4)`。这个碰撞体从流程开始就激活，玩家才能先瞄准安装。
3. 在 `阀_长池` 下复制一份 `道具/轮/Valve_part1`，命名 `轮_已安装`：
   - 本地位置 `(0,0,0)`，本地旋转按轮面贴合法兰调整（必要时 `(0,90,0)`）
   - 缩放约 `0.35`
   - **初始 SetActive(false)**
   - 不需要 Collider
4. 保存预制体。`PoolValveWheelInteract` 留在任务 5 步骤 10 在场景实例上添加。

- [ ] **步骤 9：长池南叶三级台阶**

在 `泳池房/建筑` 下新建空物体 `台阶_长池`，再建 12 块 Cube（每级 4 块并排，视觉上是一级 4 m 宽的台阶），每块缩放 `(1.0, 0.375, 0.5)`：

| 级 | 顶面 y | 中心 y | 中心 z | x 中心 |
| --- | --- | --- | --- | --- |
| 1 | -0.375 | -0.5625 | 31.55 | -20.0 / -19.0 / -18.0 / -17.0 |
| 2 | -0.750 | -0.9375 | 32.05 | -20.0 / -19.0 / -18.0 / -17.0 |
| 3 | -1.125 | -1.3125 | 32.55 | -20.0 / -19.0 / -18.0 / -17.0 |

- 材质用 `池坑_池底_06` 同款池底材质。
- 保留每块 Cube 自带的 `BoxCollider`。
- 台阶顶面必须与相邻池底/池壁接上，不能留缝；完成后在 Scene 视图从南往北看，确认是三级连续台阶。

- [ ] **步骤 10：池底数字 00**

在 `泳池房/建筑` 下新建 3D TextMeshPro `池底数字_00`：

- 世界位置 `(-18.5, -1.48, 33.0)`
- 世界旋转 `(-90, 180, 0)`（平躺、朝上、字头朝南）
- 文本 `00`，字号 `0.5`
- 字体 `Assets/Fonts/SourceHanSansCN-Medium SDF.asset`
- 颜色深青灰，稍微加一点自发光，保证透过 `PoolWater.shader` 的 0.55 alpha 仍能看清
- 不要加 Collider；`PoolNumberSightTrigger.Target` 直接引用这个 Transform

- [ ] **步骤 11：池底钥匙**

1. 按 `.agent-guides/props.md` 处理 `Assets/Art/Models/扳手/source/key.fbx`：
   - 生成 `Key_mesh` 网格资产
   - 用 `key_blinn4_BaseColor/Normal/Metallic/Roughness` 建 `Assets/Materials/Props/Key.mat`（URP/Lit）
   - 生成 `Assets/Prefabs/Props/Key.prefab`
2. 把 `Key` 实例放到 `泳池房/道具` 下，位置 `(-18.5, -1.42, 33.7)`，旋转 `(0,0,0)`，缩放按预制体实际大小调到约 0.18 m 长。
3. 给钥匙的网格子物体加 `BoxCollider`：`Center (0,0,0)`、`Size (0.06,0.04,0.18)`（按实际调）。
4. 保存预制体。`PoolPickupInteract` 留在任务 5 步骤 9 在场景实例上添加。

- [ ] **步骤 12：地漏洞与竖井**

1. 找到 `泳池房/建筑/池坑_池底_06`（池核池底，包围盒中心 `(-5,-1.65,36.8)`、尺寸 `(11,0.3,13)`，顶面 y=-1.5）。以它的世界包围盒为准，在 `(-5, 38.8)` 处留一个 `0.9 × 0.9` 的洞：
   - 禁用原 `池坑_池底_06` 的 `MeshRenderer` 与 `MeshCollider`（或删除该对象），保留其 Transform 作为尺寸参照。
   - 在洞四周补四块同材质 Cube，顶面仍是 y=-1.5，**中心 y 均为 -1.65、y 尺寸均为 0.3**：

     | 块 | 中心 | 尺寸 |
     | --- | --- | --- |
     | 西 | `(-7.975, -1.65, 36.8)` | `(5.05, 0.3, 13)` |
     | 东 | `(-2.025, -1.65, 36.8)` | `(5.05, 0.3, 13)` |
     | 北 | `(-5, -1.65, 41.275)` | `(0.9, 0.3, 4.05)` |
     | 南 | `(-5, -1.65, 34.325)` | `(0.9, 0.3, 8.05)` |

   - 四块都要有 BoxCollider；拼完后从洞口往下看，四周地板与洞口边缘不能有缝。
2. 在 `泳池房/建筑` 下新建 `竖井`：
   - 四面墙：围绕 `(-5, 38.8)` 的 0.9×0.9 开口，从 y=-1.5 到 y=-7.3；每面墙厚 0.1 m，内壁贴住洞口；加 BoxCollider。按下面精确值建四个 Cube：

     | 墙 | 中心 | 尺寸 |
     | --- | --- | --- |
     | 北 | `(-5, -4.4, 39.30)` | `(1.1, 5.8, 0.1)` |
     | 南 | `(-5, -4.4, 38.30)` | `(1.1, 5.8, 0.1)` |
     | 西 | `(-5.50, -4.4, 38.8)` | `(0.1, 5.8, 1.1)` |
     | 东 | `(-4.50, -4.4, 38.8)` | `(0.1, 5.8, 1.1)` |

   - 底地板：中心 `(-5, -7.3, 38.8)`，尺寸 `(1.2, 0.1, 1.2)`，加 BoxCollider。
   - 材质用深色混凝土/金属，内部可加一盏点光或让玩家自然看不见底。
3. 处理 `道具/地漏`：
   - 位置改到 `(-5, -1.49, 38.8)`，缩放 `(0.9, 0.02, 0.9)`
   - **删除/禁用它的 Collider**，只保留视觉；或者改成洞口的金属框。
4. 在 `竖井` 下新建空物体 `触发_下坠`：
   - 位置 `(-5, -6.9, 38.8)`
   - 加 `BoxCollider`：`Is Trigger = true`、`Center (0,0,0)`、`Size (1.2, 1.5, 1.2)`
   - **不要**在预制体上挂 `PoolDrainExitTrigger`（它要引用场景里的 `PoolFlowState` 和 `UnityEvent`）；任务 5 步骤 11 在 Level2 实例上加，任务 6 在 Level1 实例上加。

- [ ] **步骤 13：预制体自检**

在 Prefab Mode 里逐个选中下列对象，确认“几何/碰撞齐了，交互组件留给场景”：

- `道具/轮`：子物体 `Valve_part1` 上有 `SphereCollider`（还没有 `PoolPickupInteract`，任务 5 加）
- `道具/扳手`：网格子物体上有 Collider（还没有 `PoolPickupInteract`，任务 5 加）
- `道具/钥匙`：网格子物体上有 Collider（还没有 `PoolPickupInteract`，任务 5 加）
- `道具/阀_长池`：子物体 `碰撞` 上有 `BoxCollider`；`轮_已安装` 初始隐藏（还没有 `PoolValveWheelInteract`，任务 5 加）
- `道具/主阀`：子物体 `PumpLP_part1` 上有 Collider（还没有 `PoolMainValveInteract`，任务 5 加）
- `储物柜_37/Door_Front`：有 `CabinetDoorInteract(FeedbackLocked)`，`Locked Dialogue` 留空；门板自身或子物体上有 Collider
- `门_暗门/Door_Front`、`Door_Back`：有 `CabinetDoorInteract(SilentLocked)`，各自门板自身或子物体上有 Collider
- `建筑/竖井/触发_下坠`：只有 `BoxCollider`，还没有 `PoolDrainExitTrigger`（任务 5 加）

保存预制体后，Level2 与 Level1 的两个实例都应该出现同一套几何/道具/碰撞。

- [ ] **步骤 14：检查点**

不执行 git 写操作；把预制体改动清单给用户。

---

## 任务 5：Level2 对话系统、Yarn 与流程接线

**文件/场景：**
- 修改：`Assets/Yarn/Level2.yarn`
- 修改：`Assets/Scenes/Level2.unity`

- [ ] **步骤 1：追加泳池房 Yarn 节点**

在 `Assets/Yarn/Level2.yarn` 末尾追加以下 12 个节点（不要用 `\n` 换行，台词内换行只用 `<br>`）：

```yarn
title: LV2_Pool_PumpHint
---
抽水泵没有反应。<br>得先打开主阀，水压才上得来。
===

title: LV2_Pool_Number
---
池底有个数字……00。<br>更衣室那排柜子里，00 号是假的。
===

title: LV2_Pool_NeedWrench
---
主阀锈死了。<br>得找一把扳手才能拧动。
===

title: LV2_Pool_WheelPicked
---
一个阀轮。<br>长池西墙的管道上缺的就是它。
===

title: LV2_Pool_InstallWheel
---
轮装上了。<br>按住左键，把它转到底。
===

title: LV2_Pool_LongDrained
---
长池的水退下去了。<br>池底露出来了。
===

title: LV2_Pool_Locked37
---
37 号柜锁着。<br>钥匙应该在长池池底。
===

title: LV2_Pool_Key
---
一把小钥匙。<br>上面刻着 37。
===

title: LV2_Pool_Wrench
---
扳手到手了。<br>去主阀那边。
===

title: LV2_Pool_NoWrench
---
主阀的螺母太大，徒手拧不动。<br>需要扳手。
===

title: LV2_Pool_MainValveOpen
---
主阀松动了。<br>池核的水开始往下退。
===

title: LV2_Pool_CoreDrained
---
池核的水也退干净了。<br>地漏下面有风。
===
```

保存后等 Yarn 编译；`TestDialogue.yarnproject` 的 `sourceFiles` 是 `**/*.yarn`，不需要改。

- [ ] **步骤 2：把 `= 对话` 复制到 Level2**

1. 先保存 `Level2`。
2. 用 Additive 方式打开 `Assets/Scenes/Level1.unity`（不要卸载 Level2）。
3. 在 Level1 层级里选中根对象 `= 对话`，`Ctrl+C`。
4. 关闭 Level1（不保存），回到 Level2，`Ctrl+V`，把复制出来的根对象改名为 `= 对话`。
5. 确认 `= 对话` 的五个子层级都在：`飘字画面`、`飘字相机`、`对话画面`、`对话系统`、`飘字实例`。

- [ ] **步骤 3：重指 Level2 的相机引用**

跨场景复制会丢外部引用，逐个修：

1. `= 对话/飘字相机`：
   - `Tag` 改成 `Untagged`（不能是 `MainCamera`）。
   - `Dialogue3DOverlay.mainCamera` = 场景里的 `Main Camera`。
2. `= 对话/飘字实例` 的 `WorldLineView.worldCamera` = 场景里的 `Main Camera`。
3. `= 对话/对话系统`：
   - `DialogueRunner.yarnProject` = `Assets/Yarn/TestDialogue.yarnproject`
   - `FloatingDialoguePresenter.dialogueBox` = `= 对话/对话画面/对话框`
   - `FloatingDialoguePresenter.worldLines` = `= 对话/飘字实例`
4. 选中 `= 对话/对话系统`，确认 `variableStorage`、`lineProvider`、`dialoguePresenters` 没有 Missing。

- [ ] **步骤 4：创建流程状态与退水控制**

1. 在 `泳池房` 下新建空物体 `流程`。
2. 在 `流程` 下新建 `流程状态`，加 `PoolFlowState`。
3. 在 `流程` 下新建 `退水控制`，加 `PoolWaterDrain`，按下面填：

| 字段 | 值 |
| --- | --- |
| `State` | `流程/流程状态` |
| `Long Pool Surfaces` | `泳池房/道具/水面_01`、`水面_02`、`水面_03`（3 个） |
| `Long Pool Blockers` | `泳池房/建筑/空气墙_长池北叶`、`空气墙_长池颈`、`空气墙_长池颈_北`、`空气墙_长池南叶`（4 个） |
| `Core Surfaces` | `泳池房/道具/水面_04`、`水面_05`、`水面_06`、`水面_07`（4 个） |
| `Core Blockers` | `泳池房/建筑/空气墙_池核`、`空气墙_池核_北`、`空气墙_池核_北中`、`空气墙_池核_南`、`空气墙_池核_东`（5 个） |
| `Duration` | `2` |
| `Lower Y` | `-1.55` |
| `Long Pool Dialogue` | `流程/剧情/剧情_长池退水`（步骤 6 创建） |
| `Core Dialogue` | `流程/剧情/剧情_池核退水`（步骤 6 创建） |

> 水面实际父级是 `泳池房/道具`，空气墙实际父级是 `泳池房/建筑`；按上面路径找，不要新建同名对象。

- [ ] **步骤 5：创建剧情锚点**

在 `泳池房/剧情锚点` 下补这些空物体（已有的 `交互点_暗门`、`交互点_排水` 保留；已有的 `交互点_主阀` 改到下表的精确位置）：

| 锚点 | 世界位置 | 用途 |
| --- | --- | --- |
| `交互点_泵平台` | `(-15.8, 1.2, 27.0)` | 泵提示 |
| `交互点_轮` | `(-15.843, 0.60, 21.935)` | 捡轮 |
| `交互点_阀_长池` | `(-22.0, 2.18, 37.8)` | 装轮 |
| `交互点_池底数字` | `(-18.5, -0.9, 33.0)` | 池底数字 |
| `交互点_暗门内侧` | `(-20.5, 1.0, 20.15)` | 暗门内“要扳手” |
| `交互点_池底` | `(-18.5, 0.5, 35.0)` | 长池退水完成 |
| `交互点_钥匙` | `(-18.5, -1.2, 33.7)` | 捡钥匙 |
| `交互点_37柜` | `(-11.0, 1.2, 14.8)` | 37 锁着 / 捡扳手 |
| `交互点_主阀` | `(-18.185, 1.2, 27.38)` | 主阀无扳手 / 启动（改已有锚点） |
| `交互点_地漏` | `(-5.0, 0.5, 38.8)` | 池核退水完成 |

- [ ] **步骤 6：创建 12 个常驻 DialogueTrigger**

在 `流程` 下新建空物体 `剧情`，再建 12 个空物体。每个都挂 `DialogueTrigger`，字段统一为：

- `Runner` = `= 对话/对话系统` 上的 `DialogueRunner`
- `Presenter` = `= 对话/对话系统` 上的 `FloatingDialoguePresenter`
- `Text Anchor` = 下表对应锚点
- `Node Name` = 下表对应节点

| 对象 | Text Anchor | Node Name |
| --- | --- | --- |
| `剧情_泵提示` | `交互点_泵平台` | `LV2_Pool_PumpHint` |
| `剧情_池底数字` | `交互点_池底数字` | `LV2_Pool_Number` |
| `剧情_暗门要扳手` | `交互点_暗门内侧` | `LV2_Pool_NeedWrench` |
| `剧情_捡轮` | `交互点_轮` | `LV2_Pool_WheelPicked` |
| `剧情_装轮` | `交互点_阀_长池` | `LV2_Pool_InstallWheel` |
| `剧情_长池退水` | `交互点_池底` | `LV2_Pool_LongDrained` |
| `剧情_37锁着` | `交互点_37柜` | `LV2_Pool_Locked37` |
| `剧情_钥匙` | `交互点_钥匙` | `LV2_Pool_Key` |
| `剧情_扳手` | `交互点_37柜` | `LV2_Pool_Wrench` |
| `剧情_主阀无扳手` | `交互点_主阀` | `LV2_Pool_NoWrench` |
| `剧情_主阀启动` | `交互点_主阀` | `LV2_Pool_MainValveOpen` |
| `剧情_池核退水` | `交互点_地漏` | `LV2_Pool_CoreDrained` |

> 拾取物会在拾取后 `SetActive(false)`，所以 `DialogueTrigger` 必须挂在 `剧情` 下的常驻对象上，不能挂在轮/钥匙/扳手本身。

- [ ] **步骤 7：接泵平台与暗门内侧触发**

1. 在 `流程` 下新建 `触发_泵平台`：
   - `BoxCollider`：`Is Trigger = true`、`Center (-15.8, 0.9, 27.0)`、`Size (2.2, 1.5, 2.2)`
   - `PoolStoryTrigger`：`Beat` = `PumpHint`、`State` = `流程状态`、`Dialogue` = `剧情_泵提示`
2. 在 `流程` 下新建 `触发_暗门内侧`：
   - `BoxCollider`：`Is Trigger = true`、`Center (-20.5, 1.0, 20.15)`、`Size (1.5, 2.0, 1.0)`
   - `PoolStoryTrigger`：`Beat` = `NeedWrench`、`State` = `流程状态`、`Dialogue` = `剧情_暗门要扳手`

- [ ] **步骤 8：接池底数字判定**

1. 在 `流程` 下新建 `观察区_池底数字`：
   - `BoxCollider`：`Is Trigger = true`、`Center (-18.5, 1.5, 36.0)`、`Size (10, 3, 14)`
2. 在 `流程` 下新建 `池底数字判定`，加 `PoolNumberSightTrigger`：

| 字段 | 值 |
| --- | --- |
| `State` | `流程/流程状态` |
| `Eye` | 留空（运行时取 Main Camera） |
| `View Camera` | 留空（运行时取 Main Camera） |
| `Target` | `道具/池底数字_00` |
| `Observation Area` | `流程/观察区_池底数字` |
| `Dialogue` | `剧情_池底数字` |
| `Unlock Doors` | `门_暗门/Door_Front`、`门_暗门/Door_Back` 上的 `CabinetDoorInteract`（2 个） |
| `Max Distance` | `15` |
| `Max Angle` | `20` |

- [ ] **步骤 9：接拾取物**

在 Level2 的 `泳池房` 实例上给下列对象 **Add Component**（这是场景覆盖，不要改预制体）：

| 对象 | 组件字段 |
| --- | --- |
| `道具/轮` | `PoolPickupInteract`：`Kind=Wheel`、`State=流程状态`、`Dialogue=剧情_捡轮` |
| `道具/钥匙` | `PoolPickupInteract`：`Kind=Key`、`State=流程状态`、`Dialogue=剧情_钥匙`、`Unlock Doors=[储物柜_37/Door_Front 的 CabinetDoorInteract]` |
| `道具/扳手` | `PoolPickupInteract`：`Kind=Wrench`、`State=流程状态`、`Dialogue=剧情_扳手` |
| `储物柜_37/Door_Front` | 已有 `CabinetDoorInteract`：`Lock Mode=FeedbackLocked`（预制体已设）、`Locked Dialogue=剧情_37锁着`（场景实例上补） |

- [ ] **步骤 10：接长池阀与主阀**

在 Level2 的 `泳池房` 实例上给两个阀对象 **Add Component**：

1. `道具/阀_长池` 的 `PoolValveWheelInteract`：

| 字段 | 值 |
| --- | --- |
| `State` | `流程/流程状态` |
| `Wheel` | `阀_长池/轮_已安装` |
| `Rotate Axis` | `阀_长池`（forward +X） |
| `Pickup Visual` | `道具/轮` |
| `Drain` | `流程/退水控制` |
| `Install Dialogue` | `剧情_装轮` |
| `Degrees Per Second` | `240` |
| `Required Degrees` | `1080` |

2. `道具/主阀` 的 `PoolMainValveInteract`：

| 字段 | 值 |
| --- | --- |
| `State` | `流程/流程状态` |
| `Valve Wheel` | `主阀/PumpLP_part1` |
| `Rotate Axis` | `主阀` |
| `Drain` | `流程/退水控制` |
| `No Wrench Dialogue` | `剧情_主阀无扳手` |
| `Open Dialogue` | `剧情_主阀启动` |
| `Open Duration` | `1.2` |

- [ ] **步骤 11：接竖井出口**

在 Level2 的 `泳池房` 实例上给 `竖井/触发_下坠` **Add Component** `PoolDrainExitTrigger`：

- `State` = `流程/流程状态`
- `On Next Level` 留空（下一关接口；不要在这里加载场景或淡出）

- [ ] **步骤 12：把 Level2 测试出生点挪到更衣室**

选中 `测试_仅Level2` 下的玩家测试实例：

- `Position` = `(-2, 0.05, 17)`
- `Rotation` = `(0, 0, 0)`（朝北，面向泳池房门）

这只是 Level2 调试用的出生点；Level1 的玩家位置不动。

- [ ] **步骤 13：检查 Missing 与编译**

1. 在 Hierarchy 里逐个选中 `流程状态`、`退水控制`、`剧情/*`、`池底数字判定`、`触发_泵平台`、`触发_暗门内侧`、`轮`、`Key`、`扳手`、`阀_长池`、`主阀`、`触发_下坠`，确认 Inspector 没有红色 Missing。
2. 读 Console：

```text
read_console(types: ["error", "warning"], count: 40)
```

预期：0 条 error；允许出现“Yarn 节点找不到”之外的 warning，但必须修掉节点名不匹配。

- [ ] **步骤 14：检查点**

不执行 git 写操作；把 Level2 接线清单给用户。

---

## 任务 6：Level1 场景实例接线

**文件/场景：**
- 修改：`Assets/Scenes/Level1.unity`

> 泳池房在 Level1 里是 `口袋空间` 下的 `TestRoom` 实例，整体 y=-1000。下面所有坐标都填**实例局部坐标**；不要按世界坐标填，也不要把流程对象加到预制体里。

- [ ] **步骤 1：打开 Level1 并定位实例**

1. 打开 `Assets/Scenes/Level1.unity`。
2. 展开 `口袋空间`，找到 `TestRoom`（泳池房实例）。
3. 确认 `TestRoom` 的本地 Transform 仍是 `(0,0,0)`；它的父级在 y=-1000，所以世界坐标会偏移。

- [ ] **步骤 2：在实例下加流程状态与退水控制**

1. 在 `TestRoom` 下新建 `流程`。
2. `流程/流程状态` 加 `PoolFlowState`。
3. `流程/退水控制` 加 `PoolWaterDrain`，数组与 Level2 相同：
   - 长池水面 `道具/水面_01..03`，长池空气墙 `建筑/` 下 4 块
   - 池核水面 `道具/水面_04..07`，池核空气墙 `建筑/` 下 5 块
   - `Duration=2`、`Lower Y=-1.55`
   - `Long Pool Dialogue` / `Core Dialogue` 指向本地 `剧情/剧情_长池退水`、`剧情_池核退水`

- [ ] **步骤 3：在实例下加剧情锚点与 DialogueTrigger**

1. 在 `TestRoom/剧情锚点` 下补与 Level2 同名的锚点（局部坐标；已有的 `交互点_暗门`、`交互点_排水` 保留，已有的 `交互点_主阀` 改到下表位置）：
   - `交互点_泵平台 (-15.8,1.2,27.0)`
   - `交互点_轮 (-15.843,0.60,21.935)`
   - `交互点_阀_长池 (-22.0,2.18,37.8)`
   - `交互点_池底数字 (-18.5,-0.9,33.0)`
   - `交互点_暗门内侧 (-20.5,1.0,20.15)`
   - `交互点_池底 (-18.5,0.5,35.0)`
   - `交互点_钥匙 (-18.5,-1.2,33.7)`
   - `交互点_37柜 (-11.0,1.2,14.8)`
   - `交互点_主阀 (-18.185,1.2,27.38)`
   - `交互点_地漏 (-5.0,0.5,38.8)`
2. 在 `TestRoom/流程/剧情` 下建 12 个 `DialogueTrigger`，节点名与 Level2 的 12 个 `LV2_Pool_*` 完全一致。
3. `Runner`/`Presenter` 都指向 Level1 根下的 `= 对话/对话系统` 上的 `DialogueRunner` 与 `FloatingDialoguePresenter`；`Text Anchor` 指向第 1 步的本地锚点。

> 不要从 Level2 复制 `= 对话` 到 Level1；Level1 已经有完整的对话系统，直接复用。

- [ ] **步骤 4：接触发、数字判定、拾取、阀与出口**

在 Level1 的 `TestRoom` 实例上 **Add Component**，按任务 5 步骤 7–11 的字段逐个接，差异只有三点：

1. 所有对象都在 `TestRoom` 实例下，坐标用局部值。
2. 所有 `DialogueTrigger` 引用都指向 `TestRoom/流程/剧情` 下的本地副本，不要指向 Level2 的对象。
3. `储物柜_37/Door_Front` 的 `CabinetDoorInteract.Locked Dialogue` 指向本地 `剧情_37锁着`；`PoolDrainExitTrigger.On Next Level` 仍然留空（如果后续要在 Level1 接下一关，由场景自己决定）。

- [ ] **步骤 5：检查更衣室普通柜子**

1. 确认除 `储物柜_37` 与 `储物柜_54` 外的普通储物柜都没有 `CabinetDoorInteract`；它们保持纯装饰，满足“刚进更衣室不能与储物柜交互”。
2. `储物柜_54` 是长廊↔更衣室的贯通门，保留它的 `CabinetDoorInteract`，不要当普通储物柜锁掉。
3. `储物柜_37` 是 `FeedbackLocked`；`门_暗门` 两扇是 `SilentLocked`。

- [ ] **步骤 6：编译与 Missing 检查**

1. 等 Unity 编译结束。
2. 读 Console：

```text
read_console(types: ["error", "warning"], count: 40)
```

3. 逐个选中 `TestRoom/流程` 下的组件，确认没有红色 Missing；重点是 `退水控制` 的 7 个水面与 9 块空气墙数组。

- [ ] **步骤 7：检查点**

不执行 git 写操作；把 Level1 接线圈给用户。

---

## 任务 7：验收与手测

**目标：** 由用户在 Unity 里实际走完 8 步；代理只做编译检查与静态检查，不操控键鼠。

- [ ] **步骤 1：Level2 启动检查**

1. 打开 `Assets/Scenes/Level2.unity`，确认 Player 出生在更衣室 `(-2,0.05,17)`，朝向泳池房门。
2. 进入 Play。
3. 预期：
   - 更衣室普通储物柜没有准心提示。
   - `门_更衣室` 可以正常开。
   - `00` 暗门是关着的，瞄准时没有准心提示（SilentLocked）。
   - `37` 号柜瞄准时有准心；按左键只播“37 号柜锁着……”短句，门不开。

- [ ] **步骤 2：泵平台与池底数字**

1. 走进泳池房，站到抽水泵基座上。
2. 预期：一次性播 `LV2_Pool_PumpHint`，`PoolFlowState.PumpHintHeard=true`。
3. 走到长池边，面向池底 `00` 数字（距离 15 m 内、水平角 20° 内）。
4. 预期：播 `LV2_Pool_Number`，`PoolNumberSeen=true`；随后 `门_暗门` 两扇门自动打开。

- [ ] **步骤 3：暗门、轮与长池退水**

1. 进入 00 暗门，走到 `触发_暗门内侧`。
2. 预期：播 `LV2_Pool_NeedWrench`，`WrenchNeedLearned=true`。
3. 回到走廊/泵房，瞄准地上的轮：此时才出现准心；按左键播 `LV2_Pool_WheelPicked`，轮消失，`HasWheel=true`。
4. 到长池西墙 `阀_长池`，瞄准并按左键：播 `LV2_Pool_InstallWheel`，`WheelInstalled=true`，`轮_已安装` 显示。
5. **按住左键**：轮开始转；约 4.5 秒（1080° / 240°/s）后长池退水：
   - `水面_01..03` 在 2 秒内从 -0.25 降到 -1.55，然后隐藏。
   - 4 块长池空气墙同时隐藏。
   - 播 `LV2_Pool_LongDrained`，`LongPoolDrained=true`。
6. 预期：长池池底可走；新加的三级台阶能下到池底。

- [ ] **步骤 4：钥匙、37 柜与扳手**

1. 从新台阶下到长池南叶池底，走到钥匙处。
2. 预期：只有 `LongPoolDrained=true` 后钥匙才有准心；按左键播 `LV2_Pool_Key`，`HasKey=true`；37 号柜解锁。
3. 回到 37 号柜，按左键开门（不再播“锁着”）；看到柜内扳手。
4. 瞄准扳手按左键：播 `LV2_Pool_Wrench`，`HasWrench=true`，扳手消失。

- [ ] **步骤 5：主阀与池核退水**

1. 不拿扳手时先试主阀：播 `LV2_Pool_NoWrench`，主阀不转。
2. 拿到扳手后再试：主阀轮在 1.2 秒内转一圈，播 `LV2_Pool_MainValveOpen`；`MainValveOpened=true`。
3. 池核退水：
   - `水面_04..07` 2 秒内下降并隐藏。
   - 5 块池核空气墙隐藏。
   - 播 `LV2_Pool_CoreDrained`，`CoreDrained=true`。
4. 预期：池核池底可走；西侧原有三级台阶仍可用。

- [ ] **步骤 6：地漏下坠**

1. 走到池底地漏洞口 `(-5,38.8)`。
2. 预期：真实坠入 5.8 m 竖井，没有淡出、没有黑屏。
3. 落到底部触发区：
   - `PoolDrainExitTrigger` 触发一次。
   - `NextLevelReached=true`。
   - `On Next Level` 为空，不切场景、不报错。

- [ ] **步骤 7：全状态检查**

在 Play 中选中 `流程/流程状态`，确认 11 个布尔最终全部为 true：

```text
PumpHintHeard, PoolNumberSeen, WrenchNeedLearned, HasWheel, WheelInstalled,
LongPoolDrained, HasKey, HasWrench, MainValveOpened, CoreDrained, NextLevelReached
```

读 Console：

```text
read_console(types: ["error"], count: 50)
```

预期：0 条 error；允许有 Yarn 台词相关的 warning，但不能有 MissingReference / NullReference。

- [ ] **步骤 8：Level1 回归**

1. 打开 `Assets/Scenes/Level1.unity`，从大厅安全门后的传送门进入泳池房。
2. 按任务 7 步骤 1–6 再走一遍。
3. 重点检查：
   - 3D 飘字是否出现在 `TestRoom` 的局部锚点附近（y=-1000 偏移不能错位）。
   - 池底数字、钥匙、扳手、门锁在 Level1 实例里是否正常。
   - 退水后 Level1 的池底可走，地漏能下坠，`NextLevelReached=true`。

- [ ] **步骤 9：验收记录**

把用户手测结果记录下来：

- 哪一步失败、Console 报什么、`PoolFlowState` 停在哪一个 bool。
- 失败时优先查：Collider 是否在子物体、`InteractableBase` 是否被 `CanInteract` 挡住、`DialogueTrigger` 的 `textAnchor` 是否失效、空气墙数组是否漏项。
- 若用户要求改设计，先改 `docs/superpowers/specs/2026-11-21-pool-room-flow-design.html`，再改代码；不要回改本 plan。

---

## 自检

### 1. 规格覆盖度

| 规格步骤 | 实现任务 |
| --- | --- |
| 1. 更衣室不能与储物柜交互，只能去泳池房 | 任务 4 步骤 3/5/13（普通柜无组件、00 SilentLocked、37 FeedbackLocked）；任务 5 步骤 9/10 |
| 2. 抽水泵平台弹剧情 | 任务 5 步骤 7（`触发_泵平台`） |
| 3. 长池附近看到池底数字，对应暗门柜号 00 | 任务 4 步骤 5/10（00 号牌 + 池底 TMP）；任务 5 步骤 8（视口/角度/距离/观察区） |
| 4. 进入暗门，得知主阀需要扳手 | 任务 5 步骤 7（`触发_暗门内侧`） |
| 5. 捡轮、装到长池阀、旋转后长池退水 | 任务 2（拾取/装轮/按住）；任务 5 步骤 10；任务 3（退水） |
| 6. 从台阶下池底，捡 37 柜钥匙 | 任务 4 步骤 9/11；任务 5 步骤 9 |
| 7. 打开 37 柜拿扳手，启动主阀，池核退水 | 任务 1（柜门锁）；任务 2（主阀）；任务 4 步骤 3；任务 5 步骤 10 |
| 8. 池核碰撞盒消失，坠入地漏进下一关 | 任务 3（退水/出口触发）；任务 4 步骤 12；任务 5 步骤 11 |

### 2. 占位符扫描

- 计划中没有 `TODO`、`待定`、“类似任务 N”。
- `On Next Level` 留空是规格要求的“下一关接口”，不是未完成项；`PoolDrainExitTrigger` 仍然会置 `NextLevelReached`。
- Yarn 台词是可直接运行的占位文案；用户要改台词只改 `Assets/Yarn/Level2.yarn`，不影响流程。
- 场景坐标是精确值；需要按实际网格微调的地方（泵的 y、碰撞盒尺寸）都给了调整依据，不是“随便调”。

### 3. 类型一致性

- `PoolFlowState` 的 11 个属性与 `Mark/Take` 方法在任务 1 定义；后续只调用这些名字：
  - `MarkPumpHintHeard` / `MarkPoolNumberSeen` / `MarkWrenchNeedLearned`
  - `TakeWheel` / `MarkWheelInstalled` / `MarkLongPoolDrained`
  - `TakeKey` / `TakeWrench` / `MarkMainValveOpened` / `MarkCoreDrained` / `MarkNextLevelReached`
- `CabinetDoorInteract.Unlock()` / `Open()` 在任务 1 定义；任务 5 的 `PoolPickupInteract`（Key）与 `PoolNumberSightTrigger` 调用。
- `PoolWaterDrain.DrainLongPool()` / `DrainCorePool()` 在任务 3 定义；任务 2 的长池阀与主阀调用。
- `InteractableBase.IsHoldInteraction` / `OnInteractHeld(float)` 在任务 1 定义；任务 2 的 `PoolValveWheelInteract` 重写。
- `DialogueTrigger.StartDialogue()` 使用现有签名；所有 `DialogueTrigger` 的 `nodeName` 与 Yarn 节点一一对应。
- `PoolPickupInteract.Kind` 的 `Wheel/Key/Wrench`、`PoolStoryTrigger.Beat` 的 `PumpHint/NeedWrench`、`CabinetDoorInteract.LockMode` 的 `Unlocked/SilentLocked/FeedbackLocked` 在任务 1/2/3 定义，任务 4/5 的 Inspector 值一致。

### 4. 已知实现风险

1. **Collider 归属**：`IsAimed` 用 `IsChildOf`，Collider 必须在交互组件所在物体或其子物体上；挂在父级/兄弟上准心不亮，同一物体是合法的。
2. **拾取物隐藏后飘字锚点失效**：`DialogueTrigger` 必须挂在 `流程/剧情` 常驻对象上。
3. **`FeedbackLocked` 缺台词**：只报错，不禁用组件；否则钥匙解锁后 37 门仍打不开。
4. **空气墙漏关**：长池 4 块、池核 5 块，数组少一个就会出现“水退了但走不过去”。
5. **Level1 的 y=-1000**：所有锚点用实例局部坐标；`PoolNumberSightTrigger` 用世界坐标 `WorldToViewportPoint`，但 `observationArea.bounds` 是世界包围盒，能正确包含玩家。
6. **Level2 出生点**：只改 `测试_仅Level2` 的测试玩家；不要改 Level1 的玩家出生点。

---

## 执行交接

计划已保存到 `docs/superpowers/plans/2026-11-21-pool-room-flow.md`。两种执行方式：

1. **子代理驱动（推荐）**：每个任务派一个新子代理，任务间做两阶段审查；适合任务 1–3 的代码与任务 4–6 的场景接线分开跑。
2. **内联执行**：在当前会话里用 `executing-plans` 逐任务执行，按检查点停下让用户看。

选哪种方式？

