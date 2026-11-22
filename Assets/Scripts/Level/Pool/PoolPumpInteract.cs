using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 抽水泵的按键交互：每次按一下累计一次，第 10 次与第 100 次各播一段旁白；
    /// 压满 100 次可以直接排空池核，与主阀 + 扳手是并行的两条路径。
    /// </summary>
    public sealed class PoolPumpInteract : InteractableBase
    {
        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, Required, LabelText("退水控制")]
        PoolWaterDrain drain;

        [SerializeField, Required, LabelText("第一次按压剧情")]
        DialogueTrigger firstDialogue;

        [SerializeField, Required, LabelText("第十次按压剧情")]
        DialogueTrigger tenDialogue;

        [SerializeField, Required, LabelText("第一百次按压剧情")]
        DialogueTrigger hundredDialogue;

        [SerializeField, Required, LabelText("池核已排空后的剧情")]
        DialogueTrigger emptyDialogue;

        [SerializeField, LabelText("十次阈值")]
        int tenThreshold = 10;

        [SerializeField, LabelText("百次阈值")]
        int hundredThreshold = 100;

        bool _draining;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            if (state == null || drain == null || firstDialogue == null || tenDialogue == null
                || hundredDialogue == null || emptyDialogue == null)
            {
                Debug.LogError("PoolPumpInteract 缺少流程状态 / 退水控制 / 按压剧情引用", this);
                enabled = false;
            }
        }

        protected override bool CanInteract()
        {
            return state != null && state.PumpHintHeard && !_draining;
        }

        protected override void OnInteract()
        {
            if (state == null || _draining)
            {
                return;
            }

            if (state.CoreDrained)
            {
                emptyDialogue.StartDialogue();
                return;
            }

            var presses = state.RegisterPumpPress();
            if (presses >= hundredThreshold && state.PumpTenHeard)
            {
                _draining = true;
                hundredDialogue.StartDialogue();
                drain.DrainCorePool();
                return;
            }

            if (presses >= tenThreshold && !state.PumpTenHeard)
            {
                state.MarkPumpTenHeard();
                tenDialogue.StartDialogue();
                return;
            }

            if (presses == 1)
            {
                firstDialogue.StartDialogue();
            }
        }
    }
}
