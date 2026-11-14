using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Level
{
    /// <summary>
    /// 把指定相机渲染到低分辨率 RenderTexture，再交给全屏 RawImage 放大显示。
    /// 本组件接管指定相机的 targetTexture 输出，画面需由 targetImage 呈现。
    /// 注意：鼠标等屏幕坐标必须经 RawImage 的 RectTransformUtility 映射到 RenderTexture 坐标，
    /// 不能直接用于 Camera.ScreenPointToRay 之类的屏幕射线计算。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LowResDisplay : MonoBehaviour
    {
        [SerializeField, LabelText("渲染相机")] private Camera renderCamera;
        [SerializeField, LabelText("显示 RawImage")] private RawImage targetImage;
        [SerializeField, LabelText("横向像素"), MinValue(1)] private int width = 320;
        [SerializeField, LabelText("纵向像素"), MinValue(1)] private int height = 180;

        RenderTexture _renderTexture;

        void Awake()
        {
            if (renderCamera == null)
            {
                Debug.LogError("LowResDisplay 未指定渲染相机", this);
                enabled = false;
                return;
            }

            if (targetImage == null)
            {
                Debug.LogError("LowResDisplay 未指定显示 RawImage", this);
                enabled = false;
                return;
            }

            if (width <= 0 || height <= 0)
            {
                Debug.LogError("LowResDisplay 像素尺寸必须大于 0", this);
                enabled = false;
                return;
            }

            _renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                antiAliasing = 1,
                useMipMap = false,
                name = "LowResDisplay"
            };

            if (!_renderTexture.Create())
            {
                Debug.LogError("LowResDisplay 创建 RenderTexture 失败", this);
                enabled = false;
                return;
            }

            if (renderCamera.targetTexture != null)
            {
                Debug.LogWarning("LowResDisplay 接管了相机已有的 targetTexture", this);
            }

            renderCamera.targetTexture = _renderTexture;
            targetImage.texture = _renderTexture;
        }

        void OnDestroy()
        {
            // 场景重载时旧实例会晚于新实例销毁；只清理仍指向自己的引用。
            if (renderCamera != null && renderCamera.targetTexture == _renderTexture)
            {
                renderCamera.targetTexture = null;
            }

            if (targetImage != null && targetImage.texture == _renderTexture)
            {
                targetImage.texture = null;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
        }
    }
}
