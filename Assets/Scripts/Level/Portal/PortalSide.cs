using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>传送门的一侧：传送面、负责渲染对侧画面的相机、该侧墙体的裁剪面与材质。</summary>
    [Serializable]
    public sealed class PortalSide
    {
        [LabelText("传送面")] public Transform quad;
        [LabelText("传送相机")] public Camera camera;
        [LabelText("裁剪面")] public Transform clipPlane;
        [LabelText("材质")] public Material material;

        [NonSerialized] public RenderTexture renderTexture;

        /// <summary>传送面可见一侧的法线；Unity Quad 的可见面法线是 -forward。</summary>
        public Vector3 FrontNormal => quad != null ? -quad.forward : Vector3.zero;
    }
}
