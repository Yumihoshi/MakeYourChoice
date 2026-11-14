using Sirenix.OdinInspector;
using UnityEngine;
using Yarn.Unity;

namespace Game.Dialogue
{
    /// <summary>
    /// 持有当前行、逐字进度和显示方式；3D 字与对话框只按当前方式渲染。
    /// 显示方式只看 3D 字在玩家视角内是否看得清：看不见或太远看不清就改用对话框。
    /// </summary>
    public sealed class FloatingDialoguePresenter : DialoguePresenterBase
    {
        enum LineDisplay
        {
            World,
            Panel,
        }

        enum LinePhase
        {
            Idle,
            Revealing,
            Holding,
            Dismissing,
        }

        [SerializeField, Required, LabelText("对话框")] DialogueBoxView dialogueBox;
        [SerializeField, Required, LabelText("3D 字视图")] WorldLineView worldLines;
        [SerializeField, LabelText("每秒字数"), MinValue(1f)] float charsPerSecond = 25f;
        [SerializeField, LabelText("每句停留"), MinValue(0f)] float holdAfterLine = 1.2f;
        [SerializeField, LabelText("切换延迟"), MinValue(0f)] float displaySwitchDelay = 0.15f;
        [SerializeField, LabelText("可读距离"), MinValue(0.1f)] float readableDistance = 6f;

        Transform anchor;
        LineDisplay display;
        LinePhase phase;
        string bodyText = string.Empty;
        int revealed;
        bool isDialogueActive;
        bool lineVisible;
        float visibilityChangedAt;

        /// <summary>对话是否正在进行；用于阻止同一剧情点重复触发。</summary>
        public bool IsBusy => isDialogueActive;

        /// <summary>由 DialogueTrigger 在 StartDialogue 前调用；失败返回 false。</summary>
        public bool BeginStory(Transform storyAnchor)
        {
            if (storyAnchor == null)
            {
                Debug.LogError("FloatingDialoguePresenter 未指定锚点", this);
                return false;
            }

            if (dialogueBox == null)
            {
                Debug.LogError("FloatingDialoguePresenter 未指定对话框", this);
                return false;
            }

            if (worldLines == null)
            {
                Debug.LogError("FloatingDialoguePresenter 未指定 3D 字视图", this);
                return false;
            }

            anchor = storyAnchor;
            bodyText = string.Empty;
            revealed = 0;
            phase = LinePhase.Idle;
            isDialogueActive = false;

            lineVisible = worldLines.IsLineVisible(anchor.position, readableDistance);
            visibilityChangedAt = Time.time;
            display = lineVisible ? LineDisplay.World : LineDisplay.Panel;

            worldLines.HideAll();
            dialogueBox.Hide();
            return true;
        }

        public override YarnTask OnDialogueStartedAsync()
        {
            if (anchor == null)
            {
                Debug.LogError("FloatingDialoguePresenter 未调用 BeginStory", this);
                return YarnTask.CompletedTask;
            }

            if (dialogueBox == null || worldLines == null)
            {
                Debug.LogError("FloatingDialoguePresenter 缺少对话框或 3D 字视图", this);
                return YarnTask.CompletedTask;
            }

            isDialogueActive = true;
            phase = LinePhase.Idle;
            UpdateDisplayMode();
            return YarnTask.CompletedTask;
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (!isDialogueActive)
            {
                Debug.LogError("FloatingDialoguePresenter 未激活，无法显示台词", this);
                return;
            }

            bodyText = line.TextWithoutCharacterName.Text;
            revealed = 0;
            phase = LinePhase.Revealing;
            ApplyDisplay();

            int total = bodyText.Length;
            int delayMs = Mathf.Max(1, Mathf.RoundToInt(1000f / charsPerSecond));

            while (revealed < total)
            {
                if (token.IsNextContentRequested)
                {
                    CleanupLine();
                    return;
                }

                revealed = Mathf.Min(total, revealed + 1);
                ApplyReveal();

                await YarnTask.Delay(delayMs, token.NextContentToken).SuppressCancellationThrow();

                if (token.IsNextContentRequested)
                {
                    CleanupLine();
                    return;
                }
            }

            phase = LinePhase.Holding;
            int holdMs = Mathf.RoundToInt(holdAfterLine * 1000f);
            await YarnTask.Delay(holdMs, token.NextContentToken).SuppressCancellationThrow();

            if (token.IsNextContentRequested)
            {
                CleanupLine();
                return;
            }

            if (display == LineDisplay.World)
            {
                worldLines.DismissActive();
                phase = LinePhase.Dismissing;
            }
            else
            {
                dialogueBox.SetVisibleCount(total);
                phase = LinePhase.Idle;
            }
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            isDialogueActive = false;
            if (dialogueBox != null) dialogueBox.Hide();

            if (phase == LinePhase.Revealing || phase == LinePhase.Holding)
            {
                if (worldLines != null) worldLines.HideAll();
            }

            phase = LinePhase.Idle;
            bodyText = string.Empty;
            revealed = 0;
            return YarnTask.CompletedTask;
        }

        /// <summary>StartDialogue 抛异常或进入异常状态时的兜底清理。</summary>
        public void CancelStory()
        {
            isDialogueActive = false;
            phase = LinePhase.Idle;
            bodyText = string.Empty;
            revealed = 0;

            if (worldLines != null) worldLines.HideAll();
            if (dialogueBox != null) dialogueBox.Hide();
        }

        void Update()
        {
            if (!isDialogueActive) return;
            UpdateDisplayMode();
        }

        /// <summary>按 3D 字在视角内是否看得清决定显示方式；状态持续超过切换延迟才真正切换，避免边缘抖动。</summary>
        void UpdateDisplayMode()
        {
            if (anchor == null) return;

            bool visible = worldLines.IsLineVisible(anchor.position, readableDistance);
            if (visible != lineVisible)
            {
                lineVisible = visible;
                visibilityChangedAt = Time.time;
            }

            LineDisplay next = lineVisible ? LineDisplay.World : LineDisplay.Panel;
            if (next == display) return;
            if (Time.time - visibilityChangedAt < displaySwitchDelay) return;

            display = next;
            ApplyDisplay();
        }

        void ApplyDisplay()
        {
            if (dialogueBox == null || worldLines == null) return;

            if (display == LineDisplay.World)
            {
                dialogueBox.Hide();

                if (phase == LinePhase.Revealing || phase == LinePhase.Holding)
                {
                    if (worldLines.HasActive)
                    {
                        worldLines.SetVisibleCount(revealed);
                    }
                    else
                    {
                        worldLines.ShowLine(bodyText, revealed, anchor.position);
                    }
                }
                else if (phase == LinePhase.Idle && worldLines.HasActive)
                {
                    worldLines.DismissActive();
                }

                return;
            }

            worldLines.HideAll();

            if (phase == LinePhase.Dismissing)
            {
                dialogueBox.Hide();
                phase = LinePhase.Idle;
                return;
            }

            if (phase == LinePhase.Revealing || phase == LinePhase.Holding)
            {
                dialogueBox.SetLine(bodyText, revealed);
                dialogueBox.Show();
                return;
            }

            dialogueBox.Hide();
        }

        void ApplyReveal()
        {
            if (display == LineDisplay.World)
            {
                if (worldLines != null) worldLines.SetVisibleCount(revealed);
            }
            else
            {
                if (dialogueBox != null) dialogueBox.SetVisibleCount(revealed);
            }
        }

        void CleanupLine()
        {
            if (display == LineDisplay.World)
            {
                if (worldLines != null) worldLines.HideAll();
            }
            else
            {
                if (dialogueBox != null) dialogueBox.Hide();
            }

            phase = LinePhase.Idle;
            revealed = 0;
        }

        void OnValidate()
        {
            if (charsPerSecond <= 0f) charsPerSecond = 25f;
            if (holdAfterLine < 0f) holdAfterLine = 0f;
            if (displaySwitchDelay < 0f) displaySwitchDelay = 0f;
            if (readableDistance < 0.1f) readableDistance = 0.1f;
        }
    }
}
