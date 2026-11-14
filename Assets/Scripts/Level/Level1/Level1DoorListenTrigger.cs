using Game.Dialogue;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡一：玩家看过红字之后，在电梯门前站满 3 秒听门外声音，播放一次剧情。
    /// 风声剧情播完之前电梯门一直锁着（见 <see cref="Level1State.CanOpenDoor"/>），播完才允许开门。
    /// 门一旦打开就不再触发。
    /// </summary>
    public sealed class Level1DoorListenTrigger : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        Level1State state;

        [SerializeField, Required, LabelText("玩家")]
        Transform player;

        [SerializeField, Required, LabelText("听门剧情")]
        DialogueTrigger dialogue;

        [SerializeField, LabelText("停留秒数")]
        float dwellSeconds = 3f;

        [SerializeField, LabelText("触发距离")]
        float distance = 1.8f;

        float _elapsed;
        AudioPlayer _windPlayer;

        void Update()
        {
            if (state == null || player == null || dialogue == null)
            {
                return;
            }

            if (state.ListenStarted)
            {
                // 风声剧情播完（或没能播起来）之后才解锁电梯门，同时停掉循环的风声。
                if (!state.ListenFinished && !dialogue.IsRunning)
                {
                    state.MarkListenFinished();
                    StopWind();
                }

                return;
            }

            if (state.DoorOpened || !state.SawRedText || dialogue.IsRunning)
            {
                _elapsed = 0f;
                return;
            }

            if (Vector3.Distance(player.position, transform.position) > distance)
            {
                _elapsed = 0f;
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed < dwellSeconds)
            {
                return;
            }

            state.MarkListenStarted();
            _windPlayer = GameSfx.PlayLoop(GameSfx.Wind);
            dialogue.StartDialogue();
        }

        void OnDisable()
        {
            StopWind();
        }

        /// <summary>停止循环播放的风声；没有在播时什么都不做。</summary>
        void StopWind()
        {
            if (_windPlayer == null)
            {
                return;
            }

            _windPlayer.Stop();
            _windPlayer = null;
        }
    }
}
