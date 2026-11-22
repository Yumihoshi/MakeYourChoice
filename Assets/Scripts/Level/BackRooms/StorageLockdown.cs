using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 触发 11：玩家走进储藏室后，把前室门（大厅红门）与储藏室门关上并锁一分钟；
    /// 时间到后重新打开，并按这一分钟里有没有开过储藏室柜子播放触发 12 或 13。
    /// </summary>
    public sealed class StorageLockdown : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("前室门（大厅红门）")]
        AutoDoor prepRoomDoor;

        [SerializeField, Required, LabelText("储藏室门")]
        AutoDoor storageDoor;

        [SerializeField, Required, LabelText("没开柜子的旁白（触发 12）")]
        DialogueTrigger idleDialogue;

        [SerializeField, Required, LabelText("开过柜子的旁白（触发 13）")]
        DialogueTrigger openedDialogue;

        [SerializeField, LabelText("锁定时长（秒）")]
        float lockSeconds = 60f;

        CancellationTokenSource _countdown;
        bool _running;

        /// <summary>关门并开始一分钟倒计时；重复调用不会重新计时。</summary>
        public void BeginLockdown()
        {
            if (state == null || prepRoomDoor == null || storageDoor == null)
            {
                Debug.LogError($"{name} 缺少关卡状态或门引用，储藏室无法关人。", this);
                return;
            }

            if (_running)
            {
                return;
            }

            _running = true;
            CancelCountdown();
            _countdown = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            LockAndClose(prepRoomDoor);
            LockAndClose(storageDoor);
            CountdownAsync(_countdown.Token).Forget();
        }

        /// <summary>等满 <see cref="lockSeconds"/> 秒后开门并播结果旁白；被打断时直接退出。</summary>
        async UniTaskVoid CountdownAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(lockSeconds), DelayType.DeltaTime, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _running = false;
            CancelCountdown();
            state.EndLockdown();
            UnlockAndOpen(prepRoomDoor);
            UnlockAndOpen(storageDoor);

            DialogueTrigger result = state.OpenedCabinetDuringLockdown ? openedDialogue : idleDialogue;
            result.StartDialogue();
        }

        static void LockAndClose(AutoDoor door)
        {
            door.Locked = true;
            door.Close();
        }

        static void UnlockAndOpen(AutoDoor door)
        {
            door.Locked = false;
            door.Open(playSound: false);
        }

        void CancelCountdown()
        {
            if (_countdown == null)
            {
                return;
            }

            _countdown.Cancel();
            _countdown.Dispose();
            _countdown = null;
        }

        void OnDestroy()
        {
            CancelCountdown();
        }
    }
}
