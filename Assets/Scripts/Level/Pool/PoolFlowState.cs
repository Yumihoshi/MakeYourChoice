using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 泳池房八步流程的只读状态。只由各交互点 / 区域触发器置位，不广播事件、不存档。
    /// </summary>
    public sealed class PoolFlowState : MonoBehaviour
    {
        public bool EnteredPoolRoom { get; private set; }
        public bool PumpHintHeard { get; private set; }
        public bool PoolNumberSeen { get; private set; }
        public bool WrenchNeedLearned { get; private set; }
        public bool HasWheel { get; private set; }
        public bool WheelInstalled { get; private set; }
        public bool LongPoolDrained { get; private set; }
        public bool HasKey { get; private set; }
        public bool HasWrench { get; private set; }
        public bool MainValveOpened { get; private set; }
        public bool CoreDrained { get; private set; }
        public bool NextLevelReached { get; private set; }

        public void MarkEnteredPoolRoom() => EnteredPoolRoom = true;
        public void MarkPumpHintHeard() => PumpHintHeard = true;
        public void MarkPoolNumberSeen() => PoolNumberSeen = true;
        public void MarkWrenchNeedLearned() => WrenchNeedLearned = true;
        public void TakeWheel() => HasWheel = true;
        public void MarkWheelInstalled() => WheelInstalled = true;
        public void MarkLongPoolDrained() => LongPoolDrained = true;
        public void TakeKey() => HasKey = true;
        public void TakeWrench() => HasWrench = true;
        public void MarkMainValveOpened() => MainValveOpened = true;
        public void MarkCoreDrained() => CoreDrained = true;
        public void MarkNextLevelReached() => NextLevelReached = true;
    }
}
