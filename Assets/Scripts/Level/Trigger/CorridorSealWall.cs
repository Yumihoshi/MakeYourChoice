using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 长廊入口的单向封路：玩家进入触发区后启用预摆好的封闭墙，物理上封死来路。
    /// 墙只出现一次，不做开关动画与音效；触发体与墙都预先摆在场景里，脚本只负责启用。
    /// </summary>
    public sealed class CorridorSealWall : MonoBehaviour
    {
        [SerializeField, Required, LabelText("封闭墙")]
        GameObject sealWall;

        bool _sealed;

        void OnTriggerEnter(Collider other)
        {
            TrySeal(other);
        }

        /// <summary>传送或出生时玩家已经站在触发区内，靠 Stay 补触发。</summary>
        void OnTriggerStay(Collider other)
        {
            TrySeal(other);
        }

        void TrySeal(Collider other)
        {
            if (_sealed || !other.CompareTag("Player") || !GameCursorState.IsPlaying)
            {
                return;
            }

            if (sealWall == null)
            {
                Debug.LogError($"{name} 未指定封闭墙。", this);
                _sealed = true;
                return;
            }

            _sealed = true;
            sealWall.SetActive(true);
        }
    }
}
