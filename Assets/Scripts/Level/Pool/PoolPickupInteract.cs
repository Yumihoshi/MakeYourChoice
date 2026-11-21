using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>轮 / 钥匙 / 扳手共用的拾取交互；按流程状态决定能不能点。</summary>
    public sealed class PoolPickupInteract : InteractableBase
    {
        public enum Kind
        {
            Wheel,
            Key,
            Wrench
        }

        [SerializeField, LabelText("拾取物")]
        Kind kind;

        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, Required, LabelText("拾取剧情")]
        DialogueTrigger dialogue;

        [SerializeField, LabelText("拾取后解锁的柜门")]
        CabinetDoorInteract[] unlockDoors;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            if (state == null)
            {
                Debug.LogError("PoolPickupInteract 缺少流程状态引用", this);
                enabled = false;
                return;
            }

            if (dialogue == null)
            {
                Debug.LogError("PoolPickupInteract 缺少拾取剧情", this);
                enabled = false;
            }
        }

        protected override bool CanInteract()
        {
            if (state == null)
            {
                return false;
            }

            return kind switch
            {
                Kind.Wheel => state.WrenchNeedLearned && !state.HasWheel && !state.WheelInstalled,
                Kind.Key => state.LongPoolDrained && !state.HasKey,
                Kind.Wrench => state.HasKey && !state.HasWrench,
                _ => false
            };
        }

        protected override void OnInteract()
        {
            if (state == null || dialogue == null)
            {
                return;
            }

            switch (kind)
            {
                case Kind.Wheel:
                    state.TakeWheel();
                    break;
                case Kind.Key:
                    state.TakeKey();
                    if (unlockDoors != null)
                    {
                        foreach (var door in unlockDoors)
                        {
                            if (door != null)
                            {
                                door.Unlock();
                            }
                        }
                    }

                    break;
                case Kind.Wrench:
                    state.TakeWrench();
                    break;
            }

            dialogue.StartDialogue();
            gameObject.SetActive(false);
        }
    }
}
