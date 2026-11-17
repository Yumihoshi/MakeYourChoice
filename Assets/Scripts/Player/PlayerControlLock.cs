using Game.Dialogue;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 在指定剧情播放期间收回玩家控制权：进入场景先禁用控制器，等该剧情播完再交还。
    /// 仅用于开场独白；其他剧情播放时玩家仍应能移动和交互，所以不要把这个组件挂到别的剧情上。
    /// </summary>
    public sealed class PlayerControlLock : MonoBehaviour
    {
        /// <summary>开场剧情锁定期间为 true；其他剧情播放不影响这个状态。</summary>
        public static bool IsLocked { get; private set; }

        [LabelText("玩家控制器"), SerializeField] FirstPersonController controller;
        [LabelText("需要播完才能动的剧情"), SerializeField] DialogueTrigger gate;
        [LabelText("等待剧情开始的最长秒数"), SerializeField] float waitTimeout = 8f;

        bool _locked;
        float _elapsed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsLocked = false;

        void Awake()
        {
            if (controller == null || gate == null)
            {
                Debug.LogError("PlayerControlLock 缺少玩家控制器或剧情引用", this);
                enabled = false;
                return;
            }

            controller.enabled = false;
            IsLocked = true;
        }

        void OnEnable()
        {
            TypeEventSystem.Global.Register<DialogueEndedEvent>(OnDialogueEnded)
                .UnRegisterWhenDisabled(this);
        }

        /// <summary>
        /// 本段剧情播完就交还控制权。
        /// 不能用 <see cref="DialogueTrigger.IsRunning"/> 判断：Yarn 的对话在结束事件发出时还没标记为非运行，
        /// 那时它仍为 true，会把交还推迟到超时兜底。
        /// </summary>
        void OnDialogueEnded(DialogueEndedEvent e)
        {
            if (gate == null || e.Trigger != gate || !e.Completed)
            {
                return;
            }

            Release();
        }

        void Update()
        {
            if (controller == null || gate == null)
            {
                return;
            }

            if (!_locked)
            {
                if (!gate.IsRunning)
                {
                    if (!GameCursorState.IsPlaying)
                    {
                        _elapsed = 0f;
                    }

                    return;
                }

                _locked = true;
                _elapsed = 0f;
                controller.enabled = false;
                return;
            }

            if (gate.IsRunning)
            {
                _elapsed = 0f;
                return;
            }

            // 兜底：台词已经不在播，但结束事件没到（例如剧情从未真正启动），超时后照样交还。
            _elapsed += Time.deltaTime;
            if (_elapsed < waitTimeout)
            {
                return;
            }

            Release();
        }

        /// <summary>交还控制权并关掉自己；之后的剧情结束事件不再关心。</summary>
        void Release()
        {
            _locked = false;
            _elapsed = 0f;
            controller.enabled = true;
            IsLocked = false;
            enabled = false;
        }

        void OnDisable()
        {
            // 组件在锁定期间被禁用或销毁时，别把「不可交互」留在静态状态里。
            if (_locked)
            {
                IsLocked = false;
            }
        }
    }
}
