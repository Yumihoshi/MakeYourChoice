using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Dialogue
{
    /// <summary>
    /// 让 3D 浮字绕过低分辨率画面：本组件的相机只渲染文字层，输出到与屏幕同尺寸的
    /// RenderTexture，再由全屏 RawImage 叠在低分辨率画面之上，使文字与描边保持清晰。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class Dialogue3DOverlay : MonoBehaviour
    {
        [SerializeField, LabelText("主相机")]
        private Camera mainCamera;

        [SerializeField, LabelText("显示图片")]
        private RawImage targetImage;

        private Camera overlayCamera;
        private RenderTexture renderTexture;

        private void Awake()
        {
            overlayCamera = GetComponent<Camera>();

            if (mainCamera == null || targetImage == null)
            {
                Debug.LogError("Dialogue3DOverlay 缺少主相机或显示图片引用，无法显示 3D 浮字", this);
                enabled = false;
                return;
            }

            if ((mainCamera.cullingMask & overlayCamera.cullingMask) != 0)
            {
                Debug.LogWarning("Dialogue3DOverlay 的文字层仍被主相机渲染，低分辨率画面里会出现重影", this);
            }
        }

        private void LateUpdate()
        {
            SyncCamera();

            if (renderTexture == null || renderTexture.width != Screen.width || renderTexture.height != Screen.height)
            {
                RebuildTarget();
            }
        }

        private void OnDestroy()
        {
            ReleaseTarget();
        }

        private void SyncCamera()
        {
            Transform source = mainCamera.transform;
            overlayCamera.transform.SetPositionAndRotation(source.position, source.rotation);

            overlayCamera.fieldOfView = mainCamera.fieldOfView;
            overlayCamera.nearClipPlane = mainCamera.nearClipPlane;
            overlayCamera.farClipPlane = mainCamera.farClipPlane;
            overlayCamera.orthographic = mainCamera.orthographic;
            overlayCamera.orthographicSize = mainCamera.orthographicSize;
            overlayCamera.aspect = mainCamera.aspect;
        }

        private void RebuildTarget()
        {
            ReleaseTarget();

            renderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32)
            {
                name = "Dialogue3DOverlay",
                filterMode = FilterMode.Bilinear,
                antiAliasing = 1,
            };
            renderTexture.Create();

            overlayCamera.targetTexture = renderTexture;
            targetImage.texture = renderTexture;
        }

        private void ReleaseTarget()
        {
            if (renderTexture == null)
            {
                return;
            }

            if (overlayCamera != null)
            {
                overlayCamera.targetTexture = null;
            }

            if (targetImage != null)
            {
                targetImage.texture = null;
            }

            renderTexture.Release();

            if (Application.isPlaying)
            {
                Destroy(renderTexture);
            }
            else
            {
                DestroyImmediate(renderTexture);
            }

            renderTexture = null;
        }
    }
}
