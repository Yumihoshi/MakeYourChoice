using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 玩家处于该区域内的任意位置时，临时替换 CharacterController 的半径与台阶高度；离开后恢复原值。
    /// 用于长廊口假柜与更衣室暗门这类「柜内空腔只有 0.412 m 宽、净高 1.75 m」的侧身通道：
    /// 常驻半径 0.15 m 时中线左右余量只剩 ±4.5 cm，玩家会被门框挡住走不过去。
    /// 台阶高度必须一起调小：CharacterController 遇到小台阶时会先向上抬 stepOffset 再前进，
    /// 抬升 0.45 m 会让胶囊顶部顶到 1.8 m 高的门楣，抬升判定失败后整个前进被当成撞墙而卡死。
    /// 区域用同物体上的 BoxCollider 定义，按玩家位置逐帧判断，不依赖碰撞事件（传送瞬移不会漏掉恢复）。
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PlayerCapsuleZone : MonoBehaviour
    {
        [LabelText("区域内胶囊半径"), SerializeField] float radius = 0.12f;
        [LabelText("区域内台阶高度"), SerializeField] float stepOffset = 0.2f;
        [LabelText("玩家标签"), SerializeField] string playerTag = "Player";

        BoxCollider _box;
        CharacterController _controller;
        float _defaultRadius;
        float _defaultStepOffset;
        bool _inside;

        void Awake()
        {
            _box = GetComponent<BoxCollider>();

            if (!_box.isTrigger)
            {
                Debug.LogError($"{name} 的 Collider 没有勾选 Is Trigger，请改成触发体。", this);
                enabled = false;
            }
        }

        void Update()
        {
            if (_controller == null && !TryResolvePlayer())
            {
                return;
            }

            if (_box.bounds.Contains(_controller.transform.position))
            {
                Enter();
            }
            else
            {
                Exit();
            }
        }

        /// <summary>玩家销毁（换场景、重开）之后重新找一次。</summary>
        bool TryResolvePlayer()
        {
            var player = GameObject.FindWithTag(playerTag);
            if (player == null)
            {
                return false;
            }

            _controller = player.GetComponent<CharacterController>();
            return _controller != null;
        }

        void Enter()
        {
            if (_inside)
            {
                return;
            }

            _defaultRadius = _controller.radius;
            _defaultStepOffset = _controller.stepOffset;
            _controller.radius = radius;
            _controller.stepOffset = stepOffset;
            _inside = true;
        }

        void Exit()
        {
            if (!_inside)
            {
                return;
            }

            if (_controller != null)
            {
                _controller.radius = _defaultRadius;
                _controller.stepOffset = _defaultStepOffset;
            }

            _inside = false;
        }

        void OnDisable()
        {
            Exit();
        }

        void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            var previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = previous;
        }
    }
}
