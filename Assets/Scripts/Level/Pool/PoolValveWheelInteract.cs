using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 长池阀：先左键把轮装上，再按住左键累计转到 3 圈，触发长池退水。
    /// </summary>
    public sealed class PoolValveWheelInteract : InteractableBase
    {
        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, Required, LabelText("已安装的阀轮")]
        Transform wheel;

        [SerializeField, Required, LabelText("旋转轴")]
        Transform rotateAxis;

        [SerializeField, Required, LabelText("拾取物视觉")]
        GameObject pickupVisual;

        [SerializeField, Required, LabelText("退水控制")]
        PoolWaterDrain drain;

        [SerializeField, Required, LabelText("缺轮提示剧情")]
        DialogueTrigger noWheelDialogue;

        [SerializeField, LabelText("安装剧情")]
        DialogueTrigger installDialogue;

        [SerializeField, LabelText("旋转速度（度/秒）")]
        float degreesPerSecond = 240f;

        [SerializeField, LabelText("需要旋转角度")]
        float requiredDegrees = 1080f;

        float _degrees;
        bool _draining;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            if (state == null || wheel == null || rotateAxis == null || pickupVisual == null || drain == null
                || noWheelDialogue == null)
            {
                Debug.LogError("PoolValveWheelInteract 缺少流程状态 / 轮 / 旋转轴 / 拾取物 / 退水控制 / 缺轮剧情引用", this);
                enabled = false;
            }
        }

        protected override bool CanInteract()
        {
            return state != null && !state.LongPoolDrained && !_draining;
        }

        protected override bool IsHoldInteraction
        {
            get { return state != null && state.WheelInstalled && !state.LongPoolDrained && !_draining; }
        }

        protected override void OnInteract()
        {
            if (state == null || state.WheelInstalled || state.LongPoolDrained)
            {
                return;
            }

            if (!state.HasWheel)
            {
                noWheelDialogue.StartDialogue();
                return;
            }

            if (wheel == null || pickupVisual == null)
            {
                Debug.LogError("PoolValveWheelInteract 缺少轮 / 拾取物引用", this);
                return;
            }

            state.MarkWheelInstalled();
            pickupVisual.SetActive(false);
            wheel.gameObject.SetActive(true);

            if (installDialogue != null)
            {
                installDialogue.StartDialogue();
            }
        }

        protected override void OnInteractHeld(float deltaTime)
        {
            if (state == null || wheel == null || rotateAxis == null || drain == null)
            {
                return;
            }

            var step = degreesPerSecond * deltaTime;
            wheel.Rotate(rotateAxis.forward, step, Space.World);
            _degrees += step;

            if (_degrees < requiredDegrees)
            {
                return;
            }

            _draining = true;
            drain.DrainLongPool();
        }
    }
}
