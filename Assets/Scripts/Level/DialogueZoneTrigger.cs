using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 玩家站在触发区内、光标已锁定且当前没有对话时，播放一次剧情。
    /// 与 TriggerVolume 的区别：对话播放中进入不会把一次性机会吃掉，而是等对话结束再补播，
    /// 适合“走进去就该听到一段话”的剧情点。
    /// </summary>
    public sealed class DialogueZoneTrigger : MonoBehaviour
    {
        [SerializeField, Required, LabelText("对话")]
        DialogueTrigger dialogue;

        [SerializeField, Required, LabelText("玩家")]
        Transform player;

        [SerializeField, Required, LabelText("触发范围")]
        BoxCollider area;

        bool _fired;

        void Update()
        {
            if (_fired || dialogue == null || player == null || area == null)
            {
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked || dialogue.IsRunning)
            {
                return;
            }

            if (!area.bounds.Contains(player.position))
            {
                return;
            }

            _fired = true;
            dialogue.StartDialogue();
        }
    }
}
