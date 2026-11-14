using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 让物体绕竖直轴始终正对相机。用于把一张 2D 等距贴图（比如黄箱子）贴在 Quad 上，
    /// 从任何角度看都是同一张正面图，不会露出侧边。
    /// </summary>
    public class YBillboard : MonoBehaviour
    {
        [SerializeField, LabelText("面向的相机（留空用主相机）")]
        Camera targetCamera;

        void LateUpdate()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;

            Vector3 dir = transform.position - cam.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            // Quad 的正面朝 -Z，所以让 +Z 背对相机
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
