using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡二打印室里发绿光的柜子 A：第一次交互只是“反光”（触发 14）；
    /// 拿临时卡试过安全门之后再交互，从夹层里拿到真门禁卡（触发 15）。
    /// </summary>
    public sealed class Level2GlowCabinet : Level2InteractBase
    {
        [SerializeField, Required, LabelText("关卡状态")]
        Level2State state;

        [SerializeField, Required, LabelText("第一次交互的剧情")]
        DialogueTrigger firstDialogue;

        [SerializeField, Required, LabelText("第二次交互的剧情")]
        DialogueTrigger secondDialogue;

        /// <summary>进过打印室、还没拿到真卡、柜子没被锁上时才允许交互。</summary>
        protected override bool CanInteract()
        {
            if (state == null || firstDialogue == null || secondDialogue == null)
            {
                return false;
            }

            if (!state.EnteredPrintRoom || state.GotRealCard || state.CabinetsLocked)
            {
                return false;
            }

            return !firstDialogue.IsRunning && !secondDialogue.IsRunning;
        }

        protected override void OnInteract()
        {
            // 触发 15 的前置是触发 13：没拿临时卡试过安全门之前，反复交互只会重复“只是反光”。
            if (state.CheckedCabinetA && state.TriedSafeDoorWithTempCard)
            {
                state.TakeRealCard();
                secondDialogue.StartDialogue();
                return;
            }

            state.MarkCheckedCabinetA();
            firstDialogue.StartDialogue();
        }
    }
}
