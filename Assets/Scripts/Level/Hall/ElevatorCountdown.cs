using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡一：电梯门一直没打开时，按玩家在电梯里累计停留的时间播两段剧情——满 2 分钟提醒，满 5 分钟进入「最速传说」结局台词。
    /// 菜单打开、任一段剧情播放时不累计。
    /// </summary>
    public sealed class ElevatorCountdown : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        HallFlowState state;

        [SerializeField, Required, LabelText("玩家")]
        Transform player;

        [SerializeField, Required, LabelText("电梯范围")]
        BoxCollider area;

        [SerializeField, Required, LabelText("等待旁白剧情")]
        DialogueTrigger hintDialogue;

        [SerializeField, LabelText("等待旁白秒数")]
        float hintSeconds = 120f;

        [SerializeField, Required, LabelText("结局剧情")]
        DialogueTrigger endingDialogue;

        [SerializeField, LabelText("结局秒数")]
        float limitSeconds = 300f;

        float _elapsed;
        bool _hintFired;
        bool _endingFired;
        bool _invalidReported;

        void Update()
        {
            if (_endingFired)
            {
                return;
            }

            if (state == null || player == null || area == null || hintDialogue == null || endingDialogue == null)
            {
                if (!_invalidReported)
                {
                    _invalidReported = true;
                    Debug.LogError("ElevatorCountdown 缺少必要引用：关卡状态 / 玩家 / 电梯范围 / 两段剧情", this);
                }

                return;
            }

            if (state.DoorOpened || !GameCursorState.IsPlaying)
            {
                return;
            }

            if (hintDialogue.IsRunning || endingDialogue.IsRunning)
            {
                return;
            }

            if (!area.bounds.Contains(player.position))
            {
                return;
            }

            _elapsed += Time.deltaTime;

            if (!_hintFired && _elapsed >= hintSeconds)
            {
                _hintFired = true;
                hintDialogue.StartDialogue();
                return;
            }

            if (_elapsed >= limitSeconds)
            {
                _endingFired = true;
                endingDialogue.StartDialogue();
            }
        }
    }
}
