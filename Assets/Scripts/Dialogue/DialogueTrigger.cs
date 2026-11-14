using System;
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

        public async void StartDialogue()
        {
            if (runner == null)
            {
                Debug.LogError("DialogueTrigger 未指定 DialogueRunner", this);
                return;
            }

            if (presenter == null)
            {
                Debug.LogError("DialogueTrigger 未指定 FloatingDialoguePresenter", this);
                return;
            }

            if (textAnchor == null)
            {
                Debug.LogError("DialogueTrigger 未指定文字锚点", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(nodeName))
            {
                Debug.LogError("DialogueTrigger 的剧情节点为空", this);
                return;
            }

            if (runner.IsDialogueRunning)
            {
                Debug.LogWarning("DialogueTrigger 忽略重复触发：对话已在运行", this);
                return;
            }

            if (presenter.IsBusy)
            {
                Debug.LogWarning("DialogueTrigger 忽略重复触发：Presenter 正忙", this);
                return;
            }

            if (runner.YarnProject == null)
            {
                Debug.LogError("DialogueTrigger 的 DialogueRunner 没有 Yarn Project", this);
                return;
            }

            if (runner.YarnProject.Program == null)
            {
                Debug.LogError("DialogueTrigger 的 Yarn Project 没有有效 Program", this);
                return;
            }

            if (!presenter.BeginStory(textAnchor)) return;

            try
            {
                await runner.StartDialogue(nodeName);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
                presenter.CancelStory();
            }
        }
    }
}
