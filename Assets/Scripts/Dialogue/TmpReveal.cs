using TMPro;
using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>TMP 逐字显示：把已显示字数夹到文本实际字符范围内。</summary>
    public static class TmpReveal
    {
        /// <summary>只显示前 <paramref name="count"/> 个字符。</summary>
        public static void SetVisibleCount(TMP_Text text, int count)
        {
            if (text == null)
            {
                return;
            }

            text.maxVisibleCharacters = Mathf.Clamp(count, 0, text.textInfo.characterCount);
        }
    }
}
