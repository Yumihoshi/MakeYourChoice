using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Game
{
    /// <summary>
    /// 把 DOTween 补间/序列变成可 await 的等待。补间被 Kill 或 token 取消时立即返回，
    /// 不抛取消异常——调用方按「已经停下」继续收尾即可。
    /// </summary>
    public static class TweenAwaiter
    {
        /// <summary>等待补间播完；<paramref name="tween"/> 已结束或为空时立即返回。</summary>
        public static async UniTask WaitForCompletionAsync(this Tween tween, CancellationToken cancellationToken = default)
        {
            if (tween == null || !tween.IsActive())
            {
                return;
            }

            var done = new UniTaskCompletionSource();
            tween.OnComplete(() => done.TrySetResult());
            tween.OnKill(() => done.TrySetResult());

            using (cancellationToken.Register(() =>
                   {
                       if (tween.IsActive())
                       {
                           tween.Kill();
                       }
                   }))
            {
                await done.Task;
            }
        }
    }
}
