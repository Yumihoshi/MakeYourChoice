using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 在指定剧情播放期间收回玩家控制权：进入场景先禁用控制器，等该剧情开始并播放结束后再交还。
    /// 用于开场独白，避免玩家在台词还没播完时就走动或回头。
    /// </summary>
    public sealed class PlayerControlLock : MonoBehaviour
    {
        [LabelText("玩家控制器"), SerializeField] FirstPersonController controller;
        [LabelText("需要播完才能动的剧情"), SerializeField] DialogueTrigger gate;
        [LabelText("等待剧情开始的最长秒数"), SerializeField] float waitTimeout = 8f;

        bool _locked;
        float _elapsed;

        void Awake()
        {
            if (controller == null || gate == null)
            {
                Debug.LogError("PlayerControlLock 缺少玩家控制器或剧情引用", this);
                enabled = false;
                return;
            }

            controller.enabled = false;
        }

        void Update()
        {
            if (_locked)
            {
                if (gate.IsRunning)
                {
                    return;
                }

                _locked = false;
                controller.enabled = true;
                enabled = false;
                return;
            }

            if (gate.IsRunning)
            {
                _locked = true;
                controller.enabled = false;
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                _elapsed = 0f;
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed < waitTimeout)
            {
                return;
            }

            controller.enabled = true;
            enabled = false;
        }
    }
}
