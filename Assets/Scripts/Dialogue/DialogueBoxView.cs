using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>
    /// 底部对话框。只负责显示正文、按字数截断和淡入淡出；不处理输入，也不显示角色名。
    /// </summary>
    public sealed class DialogueBoxView : MonoBehaviour
    {
        [SerializeField, Required, LabelText("画布组")] CanvasGroup canvasGroup;
        [SerializeField, Required, LabelText("正文")] TMP_Text bodyText;
        [SerializeField, LabelText("淡入时间"), MinValue(0f)] float fadeInDuration = 0.15f;
        [SerializeField, LabelText("淡出时间"), MinValue(0f)] float fadeOutDuration = 0.12f;

        void Awake()
        {
            if (canvasGroup == null)
            {
                Debug.LogError("DialogueBoxView 未指定 CanvasGroup", this);
                enabled = false;
                return;
            }

            if (bodyText == null)
            {
                Debug.LogError("DialogueBoxView 未指定正文", this);
                enabled = false;
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            bodyText.raycastTarget = false;
        }

        /// <summary>设置一句台词，并按 visibleCount 截断正文。</summary>
        public void SetLine(string body, int visibleCount)
        {
            if (bodyText == null)
            {
                Debug.LogError("DialogueBoxView 未指定正文", this);
                return;
            }

            bodyText.text = body ?? string.Empty;
            bodyText.ForceMeshUpdate();
            SetVisibleCount(visibleCount);
        }

        public void SetVisibleCount(int count)
        {
            if (bodyText == null) return;
            bodyText.maxVisibleCharacters = Mathf.Clamp(count, 0, bodyText.textInfo.characterCount);
        }

        public void Show()
        {
            if (canvasGroup == null) return;

            canvasGroup.DOKill();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            if (canvasGroup.alpha >= 0.999f) return;
            canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);
        }

        public void Hide()
        {
            if (canvasGroup == null) return;

            canvasGroup.DOKill();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            if (canvasGroup.alpha <= 0.001f) return;
            canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad);
        }

        void OnDestroy()
        {
            if (canvasGroup != null) canvasGroup.DOKill();
        }
    }
}
