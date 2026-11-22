using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 触发 20：在打印室拿到临时门禁卡之前，玩家打开任意柜子时播一次
    /// “这些柜子里没有东西。看看别的地方吧……比如打印机。”
    /// </summary>
    public sealed class PrintRoomCabinetQuest : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("剧情")]
        DialogueTrigger dialogue;

        /// <summary>抽屉被拉开时调用；只在打印室、还没拿临时卡、还没播过时触发一次。</summary>
        public void RegisterOpened()
        {
            if (state == null || dialogue == null)
            {
                return;
            }

            if (!state.EnteredPrintRoom || state.GotTempCard || state.OpenedPrintCabinet || dialogue.IsRunning)
            {
                return;
            }

            state.MarkOpenedPrintCabinet();
            dialogue.StartDialogue();
        }
    }
}
