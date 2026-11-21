using System;
using Cysharp.Threading.Tasks;
using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 一次退水：水面本地 y 在 duration 内降到 lowerY，随后禁用；同一帧关空气墙。
    /// 长池和池核各调用一次，互不影响。
    /// </summary>
    public sealed class PoolWaterDrain : MonoBehaviour
    {
        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, LabelText("长池水面")]
        Transform[] longPoolSurfaces;

        [SerializeField, LabelText("长池空气墙")]
        Collider[] longPoolBlockers;

        [SerializeField, LabelText("池核水面")]
        Transform[] coreSurfaces;

        [SerializeField, LabelText("池核空气墙")]
        Collider[] coreBlockers;

        [SerializeField, LabelText("退水时长")]
        float duration = 2f;

        [SerializeField, LabelText("退水后水面高度")]
        float lowerY = -1.55f;

        [SerializeField, LabelText("长池退水剧情")]
        DialogueTrigger longPoolDialogue;

        [SerializeField, LabelText("池核退水剧情")]
        DialogueTrigger coreDialogue;

        bool _longDraining;
        bool _coreDraining;

        public void DrainLongPool()
        {
            if (state == null)
            {
                Debug.LogError("PoolWaterDrain 缺少流程状态引用", this);
                return;
            }

            if (_longDraining || state.LongPoolDrained)
            {
                return;
            }

            _longDraining = true;
            DrainAsync(true).Forget();
        }

        public void DrainCorePool()
        {
            if (state == null)
            {
                Debug.LogError("PoolWaterDrain 缺少流程状态引用", this);
                return;
            }

            if (_coreDraining || state.CoreDrained)
            {
                return;
            }

            _coreDraining = true;
            DrainAsync(false).Forget();
        }

        async UniTaskVoid DrainAsync(bool longPool)
        {
            var surfaces = longPool ? longPoolSurfaces : coreSurfaces;
            var blockers = longPool ? longPoolBlockers : coreBlockers;
            var dialogue = longPool ? longPoolDialogue : coreDialogue;

            if (surfaces == null || surfaces.Length == 0)
            {
                Debug.LogError("PoolWaterDrain 没有配置水面", this);
                return;
            }

            if (blockers == null || blockers.Length == 0)
            {
                Debug.LogError("PoolWaterDrain 没有配置空气墙", this);
            }

            try
            {
                var starts = new float[surfaces.Length];
                for (var i = 0; i < surfaces.Length; i++)
                {
                    starts[i] = surfaces[i] != null ? surfaces[i].localPosition.y : lowerY;
                }

                var d = Mathf.Max(0.01f, duration);
                for (var elapsed = 0f; elapsed < d; elapsed += Time.deltaTime)
                {
                    var k = Mathf.Clamp01(elapsed / d);
                    for (var i = 0; i < surfaces.Length; i++)
                    {
                        if (surfaces[i] == null)
                        {
                            continue;
                        }

                        var p = surfaces[i].localPosition;
                        p.y = Mathf.Lerp(starts[i], lowerY, k);
                        surfaces[i].localPosition = p;
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken);
                }

                foreach (var surface in surfaces)
                {
                    if (surface == null)
                    {
                        continue;
                    }

                    var p = surface.localPosition;
                    p.y = lowerY;
                    surface.localPosition = p;
                    surface.gameObject.SetActive(false);
                }

                if (blockers != null)
                {
                    foreach (var blocker in blockers)
                    {
                        if (blocker != null)
                        {
                            blocker.gameObject.SetActive(false);
                        }
                    }
                }

                if (longPool)
                {
                    state.MarkLongPoolDrained();
                }
                else
                {
                    state.MarkCoreDrained();
                }

                if (dialogue != null)
                {
                    dialogue.StartDialogue();
                }
            }
            catch (OperationCanceledException)
            {
                // 场景卸载时静默退出。
            }
        }
    }
}
