using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡一：玩家第一次走进触发点 B（中间墙两侧的缝隙）时播放一次剧情。
    /// 两侧缝隙各挂一个，共用 <see cref="HallFlowState.EnteredRoom"/>，所以从哪一侧进去都只播一次。
    /// </summary>
    public sealed class HallEntryTrigger : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        HallFlowState state;

        [SerializeField, Required, LabelText("剧情")]
        DialogueTrigger dialogue;

        [SerializeField, Required, LabelText("玩家")]
        Transform player;

        [SerializeField, Required, LabelText("触发范围")]
        BoxCollider area;

        void Update()
        {
            if (state == null || dialogue == null || player == null || area == null)
            {
                return;
            }

            if (state.EnteredRoom)
            {
                return;
            }

            if (!GameCursorState.IsPlaying || dialogue.IsRunning)
            {
                return;
            }

            if (!area.bounds.Contains(player.position))
            {
                return;
            }

            state.MarkEnteredRoom();
            dialogue.StartDialogue();
        }
    }
}
