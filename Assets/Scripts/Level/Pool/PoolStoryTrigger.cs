using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>区域触发的一次性剧情：进入更衣室、站上泵平台、在暗门内侧意识到需要扳手。</summary>
    public sealed class PoolStoryTrigger : MonoBehaviour
    {
        public enum Beat
        {
            ChangingRoom,
            PumpHint,
            NeedWrench
        }

        [SerializeField, LabelText("剧情节点")]
        Beat beat;

        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, Required, LabelText("剧情")]
        DialogueTrigger dialogue;

        [SerializeField, LabelText("大厅安全门状态（仅进入更衣室需要）")]
        BackRoomFlowState backRoomState;

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

            switch (beat)
            {
                case Beat.ChangingRoom:
                    if (state.EnteredChangingRoom)
                    {
                        return;
                    }

                    if (backRoomState != null && !backRoomState.SafeDoorOpened)
                    {
                        return;
                    }

                    state.MarkEnteredChangingRoom();
                    break;

                case Beat.PumpHint:
                    if (state.PumpHintHeard)
                    {
                        return;
                    }

                    state.MarkPumpHintHeard();
                    break;

                case Beat.NeedWrench:
                    if (state.WrenchNeedLearned)
                    {
                        return;
                    }

                    state.MarkWrenchNeedLearned();
                    break;
            }

            dialogue.StartDialogue();
        }
    }
}
