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
