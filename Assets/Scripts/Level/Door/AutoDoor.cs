using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>门的打开方式。</summary>
    public enum AutoDoorMode
    {
        [LabelText("整体向上")] Rise,
        [LabelText("两扇向两侧滑开")] SlideApart,
        [LabelText("绕铰链旋转")] Hinge,
    }

    /// <summary>
    /// 玩家靠近时打开的门；触发区挂在根节点，门扇作为子物体按 <see cref="mode"/> 开合。
    /// 开合动作本身交给 <see cref="IDoorMotion"/>，这里只负责锁定、开关状态、玩家触发与开门音效。
    /// </summary>
    public sealed class AutoDoor : MonoBehaviour
    {
        [LabelText("开门方式"), SerializeField] AutoDoorMode mode = AutoDoorMode.Rise;

        [LabelText("门扇（整体向上时使用；旋转门用于校验）"), SerializeField] Transform doorPanel;
        [LabelText("升起高度"), SerializeField] float openHeight = 2.5f;
        [LabelText("单侧滑动距离（两扇向两侧时使用）"), SerializeField] float slideDistance = 1.5f;

        [LabelText("左门扇"), SerializeField] Transform leftLeaf;
        [LabelText("右门扇"), SerializeField] Transform rightLeaf;

        [LabelText("铰链关节（旋转门使用）"), SerializeField] HingeJoint hinge;
        [LabelText("开门角度（旋转门使用，负值反向转）"), SerializeField] float hingeOpenAngle = -95f;
        [LabelText("最大角速度（旋转门使用，度/秒）"), SerializeField] float hingeSpeed = 150f;
        [LabelText("角度跟随刚度（旋转门使用）"), SerializeField] float hingeGain = 4f;
        [LabelText("电机力矩上限（旋转门使用）"), SerializeField] float hingeMotorForce = 600f;

        [LabelText("开关门时长"), SerializeField] float duration = 0.6f;
        [LabelText("自动关门"), SerializeField] bool autoClose;
        [LabelText("关门延迟"), SerializeField] float closeDelay = 2f;

        IDoorMotion _motion;

        /// <summary>锁住时玩家靠近也不开门、调用 <see cref="Open"/> 也无效；由剧情脚本控制。</summary>
        public bool Locked { get; set; }

        /// <summary>开门指令是否已下达（开始打开即置 true，不等门到位）；门扇是否到位用 <see cref="OpenProgress"/> / <see cref="IsFullyOpen"/>。</summary>
        public bool IsOpen { get; private set; }

        /// <summary>门扇开合进度：0 完全关闭，接近 1 完全打开；旋转门受电机死区影响可能略小于 1，判断完全打开请用 <see cref="IsFullyOpen"/>。</summary>
        public float OpenProgress => _motion != null ? _motion.Progress : 0f;

        /// <summary>门扇是否已基本到位。旋转门的电机会在剩余角度小于 0.5° 时停转，<see cref="OpenProgress"/> 可能停在略小于 1 的值，因此带容差判断。</summary>
        /// <param name="epsilon">进度阈值（0..1），默认 0.99 以覆盖旋转门电机死区。</param>
        public bool IsFullyOpen(float epsilon = 0.99f) => OpenProgress >= epsilon;

        void Awake()
        {
            // 铰链电机转的是铰链节点，门扇必须挂在它下面，否则只会空转、门板纹丝不动。
            if (mode == AutoDoorMode.Hinge && hinge != null
                && (hinge.transform.childCount == 0
                    || (doorPanel != null && !doorPanel.IsChildOf(hinge.transform))))
            {
                Debug.LogError("旋转门的门扇没有挂在铰链节点下，开门时门板不会跟着转动", this);
                enabled = false;
                return;
            }

            _motion = mode switch
            {
                AutoDoorMode.Rise => new RiseDoorMotion(
                    this, doorPanel, openHeight, duration, autoClose, closeDelay),
                AutoDoorMode.SlideApart => new SlideApartDoorMotion(
                    this, leftLeaf, rightLeaf, slideDistance, duration, autoClose, closeDelay),
                _ => new HingeDoorMotion(
                    this, hinge, hingeOpenAngle, hingeSpeed, hingeGain, hingeMotorForce, autoClose, closeDelay),
            };

            if (!_motion.TryConfigure())
            {
                enabled = false;
            }
        }

        void FixedUpdate() => _motion?.FixedTick();

        void OnDestroy() => _motion?.Dispose();

        void OnTriggerEnter(Collider other)
        {
            if (Locked || !other.CompareTag("Player"))
            {
                return;
            }

            Open(playSound: false);
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player") || !autoClose)
            {
                return;
            }

            Close();
        }

        /// <summary>打开门；锁住、已经打开或配置有误时不响应。</summary>
        /// <param name="playSound">是否播开门音效：玩家交互开门要响，触发式自动开与定时锁自动重开不响。</param>
        public void Open(bool playSound = true)
        {
            if (Locked || IsOpen || _motion == null)
            {
                return;
            }

            IsOpen = true;
            if (playSound)
            {
                GameSfx.Play(GameSfx.ElevatorDoorOpen);
            }

            _motion.Open();
        }

        /// <summary>把门放回关闭位置。</summary>
        public void Close()
        {
            if (!IsOpen || _motion == null)
            {
                return;
            }

            IsOpen = false;
            _motion.Close();
        }
    }
}
