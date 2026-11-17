using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using Yarn.Unity;

namespace Game.Dialogue
{
    /// <summary>由 TriggerVolume 的进入事件调用；校验并启动指定 Yarn 节点。</summary>
    public sealed class DialogueTrigger : MonoBehaviour
    {
        /// <summary>全局串行标记：同一时刻只让一段 Yarn 剧情运行，其余剧情排队等它播完。</summary>
        static bool s_storyBusy;

        [SerializeField, Required, LabelText("对话运行器")] DialogueRunner runner;
        [SerializeField, Required, LabelText("飘字对话 Presenter")] FloatingDialoguePresenter presenter;
        [SerializeField, Required, LabelText("文字锚点")] Transform textAnchor;
        [SerializeField, LabelText("剧情节点")] string nodeName = "Start";

        bool _queued;

        /// <summary>对话是否正在播放；用于剧情点之间互斥，避免打断当前台词。</summary>
        public bool IsRunning => runner != null && (runner.IsDialogueRunning || (presenter != null && presenter.IsBusy));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_storyBusy = false;

        public void StartDialogue()
        {
            StartDialogue(nodeName);
        }

        /// <summary>
        /// 播放指定的 Yarn 剧情节点；结束时（含被校验拦下）发 <see cref="DialogueEndedEvent"/>。
        /// 如果已有剧情在播，本段会排队等它播完再开始，不会打断前面的台词。
        /// </summary>
        public async void StartDialogue(string node)
        {
            if (_queued)
            {
                Debug.LogWarning("DialogueTrigger 忽略重复触发：本段剧情已在排队或播放中", this);
                return;
            }

            _queued = true;
            var completed = false;
            var acquired = false;
            try
            {
                await WaitForEarlierStoryAsync();
                acquired = true;
                completed = await RunDialogueAsync(node);
            }
            catch (OperationCanceledException)
            {
                // 对象销毁导致排队被取消时静默退出。
            }
            finally
            {
                if (acquired)
                {
                    s_storyBusy = false;
                }

                _queued = false;
                TypeEventSystem.Global.Send(new DialogueEndedEvent(this, completed));
            }
        }

        /// <summary>排队等前面那一段剧情播完，然后占住全局串行标记。</summary>
        async Task WaitForEarlierStoryAsync()
        {
            while (s_storyBusy)
            {
                await UniTask.NextFrame(destroyCancellationToken);
            }

            s_storyBusy = true;
        }

        /// <summary>播放指定节点：完整播完返回 true，被校验拦下或抛异常返回 false。</summary>
        async Task<bool> RunDialogueAsync(string node)
        {
            if (runner == null)
            {
                Debug.LogError("DialogueTrigger 未指定 DialogueRunner", this);
                return false;
            }

            if (presenter == null)
            {
                Debug.LogError("DialogueTrigger 未指定 FloatingDialoguePresenter", this);
                return false;
            }

            if (textAnchor == null)
            {
                Debug.LogError("DialogueTrigger 未指定文字锚点", this);
                return false;
            }

            if (string.IsNullOrWhiteSpace(node))
            {
                Debug.LogError("DialogueTrigger 的剧情节点为空", this);
                return false;
            }

            if (runner.IsDialogueRunning)
            {
                Debug.LogWarning("DialogueTrigger 忽略重复触发：对话已在运行", this);
                return false;
            }

            if (presenter.IsBusy)
            {
                Debug.LogWarning("DialogueTrigger 忽略重复触发：Presenter 正忙", this);
                return false;
            }

            if (runner.YarnProject == null)
            {
                Debug.LogError("DialogueTrigger 的 DialogueRunner 没有 Yarn Project", this);
                return false;
            }

            if (runner.YarnProject.Program == null)
            {
                Debug.LogError("DialogueTrigger 的 Yarn Project 没有有效 Program", this);
                return false;
            }

            if (!presenter.BeginStory(textAnchor))
            {
                return false;
            }

            try
            {
                // Yarn 的 StartDialogue 只等到各 presenter 的 OnDialogueStartedAsync 完成就返回，
                // 并不等整段剧情播完；必须再等 DialogueTask，否则剧情刚开场就会被当成已经结束。
                await runner.StartDialogue(node);
                await runner.DialogueTask;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
                presenter.CancelStory();
                return false;
            }
        }
    }
}
