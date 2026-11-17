using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>把门关上并锁住一段时间，时间一到自动解锁并重新打开。</summary>
    public sealed class TimedDoorLock : MonoBehaviour
    {
        [SerializeField, Required, LabelText("门")]
        AutoDoor door;

        [SerializeField, LabelText("锁定时长（秒）")]
        float lockSeconds = 60f;

        CancellationTokenSource _countdown;

        /// <summary>关门并锁定；重复调用会重新计时。</summary>
        public void LockForAWhile()
        {
            if (door == null)
            {
                Debug.LogError($"{name} 没有指定门，锁定不会生效。", this);
                return;
            }

            CancelCountdown();
            _countdown = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            door.Locked = true;
            door.Close();
            UnlockAfterDelay(_countdown.Token).Forget();
        }

        /// <summary>等满 <see cref="lockSeconds"/> 秒再解锁；被打断（重新计时或物体销毁）时直接退出。</summary>
        async UniTaskVoid UnlockAfterDelay(CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(lockSeconds), DelayType.DeltaTime, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            CancelCountdown();
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
