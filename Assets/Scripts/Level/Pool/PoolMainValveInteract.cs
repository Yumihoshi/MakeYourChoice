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
                state.MarkWrenchNeedLearned();
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
