using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 打印室里带绿色高光的文件柜 A：协调四格抽屉与两段剧情。
    /// 第一次拉开第三层播“只是反光”（触发 21）；第二次拉开第三层播“你比我想的细心”并拿到真门禁卡（触发 22）。
    /// </summary>
    public sealed class GlowCabinetInteract : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("第一次拉开第三层的剧情")]
        DialogueTrigger firstDialogue;

        [SerializeField, Required, LabelText("第二次拉开第三层的剧情")]
        DialogueTrigger secondDialogue;

        int _thirdLayerOpens;

        /// <summary>两段柜子剧情是否正在播放；播放期间抽屉不响应交互。</summary>
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

        /// <summary>第三层被拉开时调用：第一次播“只是反光”，第二次发真门禁卡。</summary>
        /// <param name="isThirdLayer">拉开的这一格是不是柜子 A 的第三层。</param>
        public void OnDrawerOpened(bool isThirdLayer)
        {
            if (state == null || !state.EnteredPrintRoom || !isThirdLayer || IsDialogueRunning || state.GotRealCard)
            {
                return;
            }

            _thirdLayerOpens++;
            if (_thirdLayerOpens == 1)
            {
                state.MarkCheckedCabinetA();
                firstDialogue.StartDialogue();
                return;
            }

            state.TakeRealCard();
            secondDialogue.StartDialogue();
        }
    }
}
