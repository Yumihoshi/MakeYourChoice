using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡二（打印室 / 储藏室）的剧情状态：记录每个触发点是否已经播过。
    /// 触发编号对应 docs/后室游戏交互节点_1-2.xlsx 的「节点2」工作表（8、10–16）。
    /// </summary>
    public sealed class BackRoomFlowState : MonoBehaviour
    {
        /// <summary>玩家是否已经跟着蓝箭头走进打印室（触发 8）。</summary>
        public bool EnteredPrintRoom { get; private set; }

        /// <summary>玩家是否已经反着走、进了储藏室（触发 10）。</summary>
        public bool EnteredStorage { get; private set; }

        /// <summary>玩家是否已经打开储藏室里的柜子 B 看到维护记录（触发 11）。</summary>
        public bool OpenedCabinetB { get; private set; }

        /// <summary>玩家是否已经从复印机拿到临时门禁卡（触发 12）。</summary>
        public bool GotTempCard { get; private set; }

        /// <summary>
        /// 柜子当前是否被锁住不能交互：拿到临时门禁卡时锁上（“锁定柜子无法交互”），
        /// 玩家拿临时卡试过安全门之后才重新放开。
        /// </summary>
        public bool CabinetsLocked { get; private set; }

        /// <summary>玩家是否已经拿临时门禁卡试过安全门（触发 13）。</summary>
        public bool TriedSafeDoorWithTempCard { get; private set; }

        /// <summary>玩家是否已经第一次交互柜子 A（触发 14）。</summary>
        public bool CheckedCabinetA { get; private set; }

        /// <summary>玩家是否已经从柜子 A 的夹层拿到真门禁卡（触发 15）。</summary>
        public bool GotRealCard { get; private set; }

        /// <summary>安全门是否已经用真卡打开（触发 16，进入下一节点）。</summary>
        public bool SafeDoorOpened { get; private set; }

        /// <summary>记录玩家走进了打印室。</summary>
        public void MarkEnteredPrintRoom()
        {
            EnteredPrintRoom = true;
        }

        /// <summary>记录玩家走进了储藏室；返回是否首次进入（首次时调用方要把储藏室门锁上）。</summary>
        /// <returns>本次是否是第一次进入储藏室。</returns>
        public bool EnterStorage()
        {
            if (EnteredStorage)
            {
                return false;
            }

            EnteredStorage = true;
            return true;
        }

        /// <summary>记录玩家看过储藏室柜子 B 里的维护记录。</summary>
        public void MarkOpenedCabinetB()
        {
            OpenedCabinetB = true;
        }

        /// <summary>记录玩家拿到临时门禁卡，同时锁住柜子。</summary>
        public void TakeTempCard()
        {
            GotTempCard = true;
            CabinetsLocked = true;
        }

        /// <summary>记录玩家用临时卡试过安全门；柜子随之重新可以交互。</summary>
        public void MarkTriedSafeDoorWithTempCard()
        {
            TriedSafeDoorWithTempCard = true;
            CabinetsLocked = false;
        }

        /// <summary>记录玩家第一次交互柜子 A。</summary>
        public void MarkCheckedCabinetA()
        {
            CheckedCabinetA = true;
        }

        /// <summary>记录玩家从柜子 A 的夹层拿到真门禁卡。</summary>
        public void TakeRealCard()
        {
            GotRealCard = true;
        }

        /// <summary>记录安全门已被真卡打开，关卡二的出口已经打开。</summary>
        public void MarkSafeDoorOpened()
        {
            SafeDoorOpened = true;
        }
    }
}
