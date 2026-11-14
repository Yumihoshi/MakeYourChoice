using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡一：电梯门一直没打开时，在电梯里累计待满 5 分钟触发结局剧情。
    /// 菜单打开、对话播放时不累计。
    /// </summary>
    public sealed class Level1ElevatorCountdown : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        Level1State state;

        [SerializeField, Required, LabelText("玩家")]
        Transform player;

        [SerializeField, Required, LabelText("电梯范围")]
        BoxCollider area;

        [SerializeField, Required, LabelText("结局剧情")]
        DialogueTrigger dialogue;

        [SerializeField, LabelText("限时秒数")]
        float limitSeconds = 300f;

        float _elapsed;
        bool _fired;

        void Update()
        {
            if (_fired || state == null || player == null || area == null || dialogue == null)
            {
                return;
            }

            if (state.DoorOpened || Cursor.lockState != CursorLockMode.Locked || dialogue.IsRunning)
            {
                return;
            }

            if (!area.bounds.Contains(player.position))
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed < limitSeconds)
            {
                return;
            }

            _fired = true;
            dialogue.StartDialogue();
        }
    }
}
