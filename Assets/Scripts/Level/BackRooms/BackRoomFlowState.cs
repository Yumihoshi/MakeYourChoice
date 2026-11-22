using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 办公室区域（大厅北侧、前室、储藏室、打印室）的剧情状态：记录每个触发点是否已经播过。
    /// 触发编号对应 docs/Back D Room (3).xlsx 的「办公室区域」工作表（7–24）。
    /// </summary>
    public sealed class BackRoomFlowState : MonoBehaviour
    {
        /// <summary>玩家是否已经交互过“禁止入内”招牌（触发 9）。</summary>
        public bool OpenedNoEntrySign { get; private set; }

        /// <summary>玩家是否已经第一次走进前室（触发 10）。</summary>
        public bool EnteredPrepRoom { get; private set; }

        /// <summary>玩家是否已经被关在储藏室与前室之间（触发 11）。</summary>
        public bool StorageTrapped { get; private set; }

        /// <summary>关门的这一分钟里玩家是否打开过储藏室的柜子（决定触发 12 还是 13）。</summary>
        public bool OpenedCabinetDuringLockdown { get; private set; }

        /// <summary>玩家是否已经第一次交互假门（触发 14）。</summary>
        public bool FakeDoorInteracted { get; private set; }

        /// <summary>假门是否已经打开（触发 15）。</summary>
        public bool FakeDoorOpened { get; private set; }

        /// <summary>玩家是否已经走进打印室（触发 16）。</summary>
        public bool EnteredPrintRoom { get; private set; }

        /// <summary>玩家是否已经在打印室打开过任意柜子（触发 20）。</summary>
        public bool OpenedPrintCabinet { get; private set; }

        /// <summary>玩家是否已经从复印机拿到临时门禁卡（触发 18）。</summary>
        public bool GotTempCard { get; private set; }

        /// <summary>玩家是否已经拿临时门禁卡试过安全门（触发 19）。</summary>
        public bool TriedSafeDoorWithTempCard { get; private set; }

        /// <summary>玩家是否已经第一次拉开柜子 A 的第三层（触发 21）。</summary>
        public bool CheckedCabinetA { get; private set; }

        /// <summary>玩家是否已经从柜子 A 的夹层拿到真门禁卡（触发 22）。</summary>
        public bool GotRealCard { get; private set; }

        /// <summary>安全门是否已经用真卡打开（触发 23）。</summary>
        public bool SafeDoorOpened { get; private set; }

        /// <summary>玩家是否已经看过储藏室柜子里的维护记录（触发 24）。</summary>
        public bool OpenedCabinetB { get; private set; }

        /// <summary>储藏室/前室的门是否正锁着（触发 11–12/13 之间）。</summary>
        public bool IsLockdownActive { get; private set; }

        /// <summary>储藏室的维护记录以“已经进入储藏室”为前提，这里作为触发 11 的别名。</summary>
        public bool EnteredStorage => StorageTrapped;

        /// <summary>记录玩家交互过“禁止入内”招牌。</summary>
        public void MarkOpenedNoEntrySign()
        {
            OpenedNoEntrySign = true;
        }

        /// <summary>记录玩家第一次走进前室。</summary>
        public void MarkEnteredPrepRoom()
        {
            EnteredPrepRoom = true;
        }

        /// <summary>记录玩家走进储藏室、被关在里面，并开始一分钟倒计时。</summary>
        public void MarkStorageTrapped()
        {
            StorageTrapped = true;
            IsLockdownActive = true;
        }

        /// <summary>记录关门的这一分钟里玩家打开过储藏室的柜子。</summary>
        public void MarkCabinetOpenedDuringLockdown()
        {
            if (IsLockdownActive)
            {
                OpenedCabinetDuringLockdown = true;
            }
        }

        /// <summary>一分钟结束、门重新打开。</summary>
        public void EndLockdown()
        {
            IsLockdownActive = false;
        }

        /// <summary>记录玩家第一次交互假门。</summary>
        public void MarkFakeDoorInteracted()
        {
            FakeDoorInteracted = true;
        }

        /// <summary>记录假门已经打开。</summary>
        public void MarkFakeDoorOpened()
        {
            FakeDoorOpened = true;
        }

        /// <summary>记录玩家走进了打印室。</summary>
        public void MarkEnteredPrintRoom()
        {
            EnteredPrintRoom = true;
        }

        /// <summary>记录玩家在打印室打开过柜子。</summary>
        public void MarkOpenedPrintCabinet()
        {
            OpenedPrintCabinet = true;
        }

        /// <summary>记录玩家拿到临时门禁卡。</summary>
        public void TakeTempCard()
        {
            GotTempCard = true;
        }

        /// <summary>记录玩家用临时卡试过安全门。</summary>
        public void MarkTriedSafeDoorWithTempCard()
        {
            TriedSafeDoorWithTempCard = true;
        }

        /// <summary>记录玩家第一次拉开柜子 A 的第三层。</summary>
        public void MarkCheckedCabinetA()
        {
            CheckedCabinetA = true;
        }

        /// <summary>记录玩家从柜子 A 的夹层拿到真门禁卡。</summary>
        public void TakeRealCard()
        {
            GotRealCard = true;
        }

        /// <summary>记录安全门已被真卡打开。</summary>
        public void MarkSafeDoorOpened()
        {
            SafeDoorOpened = true;
        }

        /// <summary>记录玩家看过储藏室柜子里的维护记录。</summary>
        public void MarkOpenedCabinetB()
        {
            OpenedCabinetB = true;
        }
    }
}
