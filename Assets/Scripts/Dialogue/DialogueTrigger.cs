using System;
using System.Threading.Tasks;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using Yarn.Unity;

namespace Game.Dialogue
{
    /// <summary>由 TriggerVolume 的进入事件调用；校验并启动指定 Yarn 节点。</summary>
    public sealed class DialogueTrigger : MonoBehaviour
    {
        [SerializeField, Required, LabelText("对话运行器")] DialogueRunner runner;
        [SerializeField, Required, LabelText("飘字对话 Presenter")] FloatingDialoguePresenter presenter;
        [SerializeField, Required, LabelText("文字锚点")] Transform textAnchor;
        [SerializeField, LabelText("剧情节点")] string nodeName = "Start";

        /// <summary>对话是否正在播放；用于剧情点之间互斥，避免打断当前台词。</summary>
        public bool IsRunning => runner != null && (runner.IsDialogueRunning || (presenter != null && presenter.IsBusy));

        public void StartDialogue()
        {
            StartDialogue(nodeName);
        }

        /// <summary>播放指定的 Yarn 剧情节点；结束时（含被校验拦下）发 <see cref="DialogueEndedEvent"/>。</summary>
        public async void StartDialogue(string node)
        {
            var completed = false;
            try
            {
                completed = await RunDialogueAsync(node);
            }
            finally
            {
                TypeEventSystem.Global.Send(new DialogueEndedEvent(this, completed));
            }
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
