using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡一：开门前回头看红字“别开”时播放一次剧情，并记录玩家已经看过红字。
    /// 需要同时满足：在可视距离内、视线对着红字、红字真的在画面里且没被挡住。门一旦打开就不再触发。
    /// </summary>
    public sealed class Level1RedTextTrigger : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        Level1State state;

        [SerializeField, Required, LabelText("视线起点（玩家眼睛）")]
        Transform eye;

        [SerializeField, Required, LabelText("判定相机")]
        Camera viewCamera;

        [SerializeField, Required, LabelText("红字物体")]
        Transform target;

        [SerializeField, Required, LabelText("红字剧情")]
        DialogueTrigger dialogue;

        [SerializeField, LabelText("可视距离")]
        float maxDistance = 7f;

        [SerializeField, LabelText("对准角度")]
        float maxAngle = 25f;

        bool _fired;

        void Update()
        {
            if (_fired || state == null || eye == null || target == null || dialogue == null)
            {
                return;
            }

            if (state.DoorOpened || state.SawRedText || dialogue.IsRunning)
            {
                return;
            }

            if (!IsLookingAtRedText())
            {
                return;
            }

            _fired = true;
            state.MarkRedTextSeen();
            dialogue.StartDialogue();
        }

        /// <summary>红字在画面内、距离够近，且玩家确实把视线对准了它。</summary>
        bool IsLookingAtRedText()
        {
            int mask = ~LayerMask.GetMask("Player");
            if (!ScreenVisibility.IsOnScreen(viewCamera, target.position, maxDistance, mask))
            {
                return false;
            }

            Vector3 toTarget = target.position - eye.position;
            Vector3 flatForward = eye.forward;
            toTarget.y = 0f;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.0001f || toTarget.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            return Vector3.Angle(flatForward, toTarget) <= maxAngle;
        }
    }
}
