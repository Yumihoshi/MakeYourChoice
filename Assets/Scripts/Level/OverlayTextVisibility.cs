using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 让 Dialogue3D 叠层里的世界空间文字只在相机真正看得见时显示：
    /// 超出可见距离、转到文字背面、或视线被场景挡住时隐藏文字。
    /// 叠层相机不做深度测试，没有这层判断时文字会从墙后透出来，从背面还会看到镜像字。
    /// 文字正面是自身的 -forward（UGUI Canvas 平面朝 +forward，字要反过来看才正）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OverlayTextVisibility : MonoBehaviour
    {
        [SerializeField, LabelText("目标文字")] private TMP_Text text;
        [SerializeField, LabelText("判断用相机")] private Camera viewCamera;
        [SerializeField, LabelText("最大可见距离"), MinValue(0f)] private float maxDistance = 12f;
        [SerializeField, LabelText("遮挡层")] private LayerMask blockMask = ~0;
        [SerializeField, LabelText("最小正面夹角"), Range(0f, 1f)] private float minFacingDot = 0.15f;

        void Awake()
        {
            if (text == null)
            {
                text = GetComponent<TMP_Text>();
            }

            if (text == null)
            {
                Debug.LogError("OverlayTextVisibility 未指定目标文字", this);
                enabled = false;
                return;
            }

            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            if (viewCamera == null)
            {
                Debug.LogError("OverlayTextVisibility 未指定判断用相机，场景里也找不到 MainCamera", this);
                enabled = false;
            }
        }

        void LateUpdate()
        {
            Transform plane = text.transform;
            Vector3 toCamera = viewCamera.transform.position - plane.position;
            bool facing = Vector3.Dot(-plane.forward, toCamera.normalized) > minFacingDot;
            bool visible = facing && ScreenVisibility.IsOnScreen(viewCamera, plane.position, maxDistance, blockMask);
            text.enabled = visible;
        }
    }
}
