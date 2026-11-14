using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 让一块贴图持续呼吸闪烁，用来标记关卡二里“会闪光的东西”（比如闪紫光的复印机）。
    /// 只改渲染器的 _BaseColor，材质本身和别的物体不受影响。
    /// </summary>
    public sealed class Level2Blink : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Required, LabelText("闪烁的渲染器")]
        Renderer target;

        [SerializeField, LabelText("一次闪烁的时长（秒）")]
        float period = 1.6f;

        [SerializeField, LabelText("暗色")]
        Color dimColor = new Color(0.16f, 0.05f, 0.28f, 1f);

        [SerializeField, LabelText("亮色")]
        Color brightColor = new Color(0.78f, 0.42f, 1f, 1f);

        MaterialPropertyBlock _block;

        void Update()
        {
            if (target == null)
            {
                return;
            }

            _block ??= new MaterialPropertyBlock();
            float t = (Mathf.Sin(Time.time / Mathf.Max(period, 0.01f) * Mathf.PI * 2f) + 1f) * 0.5f;
            target.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, Color.Lerp(dimColor, brightColor, t));
            target.SetPropertyBlock(_block);
        }
    }
}
