using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡一：开门前回头看向红字“别开”时播放一次剧情，并记录玩家已经看过红字。
    /// 门一旦打开就不再触发。
    /// </summary>
    public sealed class Level1RedTextTrigger : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        Level1State state;

        [SerializeField, Required, LabelText("玩家")]
        Transform player;

        [SerializeField, Required, LabelText("红字剧情")]
        DialogueTrigger dialogue;

        [SerializeField, LabelText("可视距离")]
        float maxDistance = 7f;

        [SerializeField, LabelText("对准角度")]
        float maxAngle = 25f;

        bool _fired;

        void Update()
        {
            if (_fired || state == null || player == null || dialogue == null)
            {
                return;
            }

            if (state.DoorOpened || state.SawRedText || dialogue.IsRunning)
            {
                return;
            }

            Vector3 toText = transform.position - player.position;
            toText.y = 0f;
            if (toText.magnitude > maxDistance)
            {
                return;
            }

            if (Vector3.Angle(player.forward, toText) > maxAngle)
            {
                return;
            }

            _fired = true;
            state.MarkRedTextSeen();
            dialogue.StartDialogue();
        }
    }
}
