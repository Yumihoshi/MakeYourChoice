using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡二打印室里的文件柜 A：协调四格抽屉、绿色描边与两段剧情。
    /// 第一次拉开任意抽屉播“只是反光”（触发 14）；
    /// 拿到临时卡并试过安全门之后，拉开第三个抽屉交互门禁卡播第二段（触发 15）并收走卡。
    /// </summary>
    public sealed class GlowCabinetInteract : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("第一次交互的剧情")]
        DialogueTrigger firstDialogue;

        [SerializeField, Required, LabelText("第二次交互的剧情")]
        DialogueTrigger secondDialogue;

        /// <summary>两段柜子剧情是否正在播放；播放期间抽屉和卡片都不响应交互。</summary>
        public bool IsDialogueRunning
        {
            get
            {
                return (firstDialogue != null && firstDialogue.IsRunning)
                       || (secondDialogue != null && secondDialogue.IsRunning);
            }
        }

        /// <summary>进过打印室、柜子剧情没在播时，抽屉才允许开合。</summary>
        public bool CanUseDrawers
        {
            get { return state != null && state.EnteredPrintRoom && !IsDialogueRunning; }
        }

        /// <summary>第一次拉开抽屉时调用，只播一次“只是反光”（触发 14）。</summary>
        public void OnDrawerOpened()
        {
            if (state == null || firstDialogue == null || !state.EnteredPrintRoom || state.CheckedCabinetA)
            {
                return;
            }

            state.MarkCheckedCabinetA();
            firstDialogue.StartDialogue();
        }

        /// <summary>
        /// 玩家交互第三个抽屉里的门禁卡：满足触发 13/14 后拿到真卡并播第二段剧情（触发 15），
        /// 否则只重复“只是反光”。返回是否真的拿到了卡。
        /// </summary>
        public bool TryTakeRealCard()
        {
            if (state == null || firstDialogue == null || secondDialogue == null || !state.EnteredPrintRoom)
            {
                return false;
            }

            if (state.GotRealCard || state.CabinetsLocked || IsDialogueRunning)
            {
                return false;
            }

            if (!state.CheckedCabinetA)
            {
                state.MarkCheckedCabinetA();
                firstDialogue.StartDialogue();
                return false;
            }

            if (!state.TriedSafeDoorWithTempCard)
            {
                firstDialogue.StartDialogue();
                return false;
            }

            state.TakeRealCard();
            secondDialogue.StartDialogue();
            return true;
        }
    }
}
