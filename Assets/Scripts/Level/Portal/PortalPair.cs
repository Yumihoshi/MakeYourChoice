using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Level
{
    /// <summary>
    /// 把大厅安全门与 1000 m 外的测试走廊连成一对单向传送面：
    /// 负责两张传送面的相机、斜近裁剪、玩家瞬移与 RT 生命周期。
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class PortalPair : MonoBehaviour
    {
        [LabelText("玩家"), SerializeField] Transform player;
        [LabelText("玩家控制器"), SerializeField] CharacterController controller;
        [LabelText("主相机"), SerializeField] Camera mainCamera;
        [LabelText("Cinemachine Brain"), SerializeField] CinemachineBrain brain;
        [LabelText("安全门"), SerializeField] AutoDoor safeDoor;
        [LabelText("大厅侧"), SerializeField] PortalSide hallSide = new PortalSide();
        [LabelText("走廊侧"), SerializeField] PortalSide roomSide = new PortalSide();

        [LabelText("触发距离"), SerializeField] float triggerDistance = 0.18f;
        [LabelText("落点前移"), SerializeField] float landingOffset = 0.25f;
        [LabelText("空间高度容差"), Tooltip("玩家与传送面的高度差超过该值即视为不在同一空间；口袋空间与大厅水平重叠、仅竖直相隔 1000 m")]
        [SerializeField] float spaceHeightTolerance = 3f;
        [LabelText("分辨率"), SerializeField] Vector2Int resolution = new Vector2Int(320, 180);
        [LabelText("显示辅助线"), SerializeField] bool showGuideGizmos = true;

        bool _active;
        bool _valid;
        Vector3 _lastPlayerPosition;
        readonly Plane[] _frustumPlanes = new Plane[6];

        static readonly Color QuadGizmoColor = new Color(1f, 1f, 1f, 0.45f);
        static readonly Color TriggerGizmoColor = new Color(1f, 0.62f, 0.16f);
        static readonly Color LandingGizmoColor = new Color(0.3f, 1f, 0.45f);
        static readonly Vector3[] QuadCornerOffsets =
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
        };

        void Awake()
        {
            _valid = Validate();
            if (!_valid)
            {
                enabled = false;
                return;
            }

            ConfigureCamera(hallSide);
            ConfigureCamera(roomSide);
            if (!_valid)
            {
                enabled = false;
                return;
            }

            SetSideActive(hallSide, false);
            SetSideActive(roomSide, false);
            _lastPlayerPosition = player.position;
        }

        /// <summary>由安全门的 exitOpened 调用；开门后启用传送面、相机与 RT。</summary>
        public void Activate()
        {
            if (!_valid || _active)
            {
                return;
            }

            _active = true;
            CreateRenderTexture(hallSide, "大厅侧");
            CreateRenderTexture(roomSide, "走廊侧");
            SetSideActive(hallSide, true);
            SetSideActive(roomSide, true);
            _lastPlayerPosition = player.position;
        }

        void LateUpdate()
        {
            if (!_active)
            {
                return;
            }

            UpdateSideCamera(hallSide, roomSide);
            UpdateSideCamera(roomSide, hallSide);

            if (!safeDoor.IsFullyOpen())
            {
                _lastPlayerPosition = player.position;
                return;
            }

            TryTeleport(hallSide, roomSide);
            TryTeleport(roomSide, hallSide);
            _lastPlayerPosition = player.position;
        }

        void OnDestroy()
        {
            ReleaseRenderTexture(hallSide);
            ReleaseRenderTexture(roomSide);
        }

        void OnDrawGizmos()
        {
            if (!showGuideGizmos)
            {
                return;
            }

            DrawSideGuides(hallSide);
            DrawSideGuides(roomSide);
        }

        /// <summary>
        /// 画出单侧的传送面（白）、触发面（橙）与落点面（绿）：
        /// 橙色平面是玩家跨过后瞬移的位置，绿色平面是从对侧过来后玩家出现的位置。
        /// </summary>
        void DrawSideGuides(PortalSide side)
        {
            if (side == null || side.quad == null)
            {
                return;
            }

            var quad = side.quad;
            var normal = side.FrontNormal;
            var quadCorners = new Vector3[QuadCornerOffsets.Length];
            for (int i = 0; i < QuadCornerOffsets.Length; i++)
            {
                quadCorners[i] = quad.TransformPoint(QuadCornerOffsets[i]);
            }

            var triggerCorners = OffsetCorners(quadCorners, normal * triggerDistance);
            var landingCorners = OffsetCorners(quadCorners, normal * landingOffset);

            DrawFrame(quadCorners, QuadGizmoColor);
            DrawFrame(triggerCorners, TriggerGizmoColor);
            DrawFrame(landingCorners, LandingGizmoColor);

            for (int i = 0; i < quadCorners.Length; i++)
            {
                Gizmos.color = TriggerGizmoColor;
                Gizmos.DrawLine(quadCorners[i], triggerCorners[i]);
                Gizmos.color = LandingGizmoColor;
                Gizmos.DrawLine(quadCorners[i], landingCorners[i]);
            }

#if UNITY_EDITOR
            var right = quad.right.normalized * (Mathf.Abs(quad.lossyScale.x) * 0.5f + 0.08f);
            var up = quad.up.normalized * (Mathf.Abs(quad.lossyScale.y) * 0.25f);
            Handles.Label(quad.position + normal * triggerDistance + right + up,
                $"触发距离 {triggerDistance:0.##} m");
            Handles.Label(quad.position + normal * landingOffset + right - up,
                $"落点前移 {landingOffset:0.##} m");
#endif
        }

        static Vector3[] OffsetCorners(Vector3[] corners, Vector3 offset)
        {
            var result = new Vector3[corners.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                result[i] = corners[i] + offset;
            }

            return result;
        }

        static void DrawFrame(Vector3[] corners, Color color)
        {
            Gizmos.color = color;
            for (int i = 0; i < corners.Length; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % corners.Length]);
            }
        }

        bool Validate()
        {
            bool ok = true;
            ok &= Require(player != null, "玩家");
            ok &= Require(controller != null, "玩家控制器");
            ok &= Require(mainCamera != null, "主相机");
            ok &= Require(brain != null, "Cinemachine Brain");
            ok &= Require(safeDoor != null, "安全门");
            ok &= ValidateSide(hallSide, "大厅侧");
            ok &= ValidateSide(roomSide, "走廊侧");
            return ok;
        }

        bool ValidateSide(PortalSide side, string label)
        {
            if (side == null)
            {
                Debug.LogError($"{name}: 缺少「{label}」配置", this);
                return false;
            }

            bool ok = true;
            ok &= Require(side.quad != null, $"{label}/传送面");
            ok &= Require(side.camera != null, $"{label}/传送相机");
            ok &= Require(side.clipPlane != null, $"{label}/裁剪面");
            ok &= Require(side.material != null, $"{label}/材质");
            return ok;
        }

        bool Require(bool condition, string label)
        {
            if (condition)
            {
                return true;
            }

            Debug.LogError($"{name}: 缺少「{label}」引用", this);
            return false;
        }

        void ConfigureCamera(PortalSide side)
        {
            var camera = side.camera;
            var mainData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (mainData == null || data == null)
            {
                Debug.LogError($"{name}: 主相机或传送相机缺少 UniversalAdditionalCameraData", this);
                _valid = false;
                return;
            }

            camera.enabled = false;
            camera.cullingMask = mainCamera.cullingMask;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = mainCamera.nearClipPlane;
            camera.farClipPlane = mainCamera.farClipPlane;
            camera.allowHDR = mainCamera.allowHDR;
            camera.allowMSAA = mainCamera.allowMSAA;
            camera.depth = -10f;
            camera.targetTexture = null;

            data.renderType = CameraRenderType.Base;
            data.renderPostProcessing = false;
            data.antialiasing = mainData.antialiasing;
            data.renderShadows = mainData.renderShadows;
            data.requiresDepthTexture = mainData.requiresDepthTexture;
            data.requiresColorTexture = mainData.requiresColorTexture;
        }

        void CreateRenderTexture(PortalSide side, string label)
        {
            if (side.renderTexture != null)
            {
                return;
            }

            var rt = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32)
            {
                name = $"{name}_{label}_RT",
                filterMode = FilterMode.Point,
                antiAliasing = 1,
                useMipMap = false,
                wrapMode = TextureWrapMode.Clamp,
            };

            if (!rt.Create())
            {
                Debug.LogError($"{name}: 创建「{label}」传送 RT 失败", this);
                enabled = false;
                return;
            }

            side.renderTexture = rt;
            side.camera.targetTexture = rt;
            side.material.mainTexture = rt;
        }

        void ReleaseRenderTexture(PortalSide side)
        {
            if (side == null)
            {
                return;
            }

            if (side.camera != null)
            {
                side.camera.targetTexture = null;
            }

            if (side.material != null)
            {
                side.material.mainTexture = null;
            }

            if (side.renderTexture != null)
            {
                side.renderTexture.Release();
                Destroy(side.renderTexture);
                side.renderTexture = null;
            }
        }

        void SetSideActive(PortalSide side, bool active)
        {
            side.quad.gameObject.SetActive(active);
            side.camera.enabled = active;
        }

        void UpdateSideCamera(PortalSide visible, PortalSide other)
        {
            var rotation = GetRotation(visible, other);
            var camera = visible.camera;
            var main = mainCamera.transform;
            camera.transform.SetPositionAndRotation(
                other.quad.position + rotation * (main.position - visible.quad.position),
                rotation * main.rotation);

            if (!IsQuadVisible(visible.quad))
            {
                camera.enabled = false;
                return;
            }

            camera.enabled = true;
            camera.projectionMatrix = GetObliqueProjection(mainCamera, camera, other.clipPlane);
        }

        void TryTeleport(PortalSide source, PortalSide destination)
        {
            var position = player.position;
            var offset = position - source.quad.position;
            var normal = source.FrontNormal;

            // 两个空间在水平面上完全重叠、只隔了 1000 m 竖直距离，单看「到传送面的距离」两侧都会成立；
            // 因此先确认玩家与源传送面处于同一空间，否则在走廊里走到 z≈1.4 会被大厅侧传送面吸走。
            if (Mathf.Abs(position.y - source.quad.position.y) > spaceHeightTolerance)
            {
                return;
            }

            var distance = Vector3.Dot(offset, normal);
            if (distance <= 0f || distance > triggerDistance)
            {
                return;
            }

            if (Vector3.Dot(position - _lastPlayerPosition, normal) >= 0f)
            {
                return;
            }

            if (Mathf.Abs(Vector3.Dot(offset, source.quad.right)) > 0.72f)
            {
                return;
            }

            var rotation = GetRotation(source, destination);
            var lateral = offset - distance * normal;
            var target = destination.quad.position + rotation * lateral
                + landingOffset * destination.FrontNormal;

            controller.enabled = false;
            player.position = target;
            controller.enabled = true;
            CinemachineCore.Instance.OnTargetObjectWarped(player, target - position);
            _lastPlayerPosition = target;
        }

        bool IsQuadVisible(Transform quad)
        {
            if (!quad.TryGetComponent<Renderer>(out var quadRenderer) || quadRenderer == null)
            {
                return false;
            }

            GeometryUtility.CalculateFrustumPlanes(mainCamera, _frustumPlanes);
            return GeometryUtility.TestPlanesAABB(_frustumPlanes, quadRenderer.bounds);
        }

        static Quaternion GetRotation(PortalSide visible, PortalSide other)
        {
            return other.quad.rotation * Quaternion.Euler(0f, 180f, 0f)
                * Quaternion.Inverse(visible.quad.rotation);
        }

        static Matrix4x4 GetObliqueProjection(Camera source, Camera target, Transform clipPlane)
        {
            var normal = clipPlane.forward;
            var worldToCamera = target.worldToCameraMatrix;
            var cameraSpacePoint = worldToCamera.MultiplyPoint(clipPlane.position);
            var cameraSpaceNormal = worldToCamera.MultiplyVector(normal).normalized;
            var cameraSpacePlane = new Vector4(
                cameraSpaceNormal.x,
                cameraSpaceNormal.y,
                cameraSpaceNormal.z,
                -Vector3.Dot(cameraSpacePoint, cameraSpaceNormal));

            target.projectionMatrix = source.projectionMatrix;
            return target.CalculateObliqueMatrix(cameraSpacePlane);
        }
    }
}
