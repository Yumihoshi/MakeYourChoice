# 非欧几何「空间叠加」实现计划

> **规格：** `docs/superpowers/specs/2026-11-19-space-stacking-design.md`

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 在 Level1 安全门处实现「门前面向 +z 看见并走入 1000 m 外的 TestRoom 走廊、背面朝 -z 只看到墙、可原路返回」的空间叠加效果。

**架构：** 安全门处一张朝南的传送面 P1、走廊入口一张朝北的传送面 P2，组成一对单向传送门；每侧用一台 320×180 的传送相机 + 斜近裁剪渲染对侧，玩家靠近时按配对旋转/平移瞬移；TestRoom 做成 Prefab，在 Level1 的 `= 关卡/口袋空间`（y=-1000）放一个实例。

**技术栈：** Unity 2022.3.62f3c1、URP 14.0.12、Cinemachine、Odin Inspector、ProBuilder 5.2.4、Unity MCP。

**项目约束（必须遵守）：** 按 `AGENTS.md` 只允许查询类 git 操作，**本计划不包含 `git add/commit`**；场景/预制体用 Unity 编辑器或 Unity MCP 修改，不手写 YAML；`Assets/Plugins` 与 `Assets/QFramework` 只读；异步用 UniTask、不写协程；序列化字段加 Odin `[LabelText("中文")]`；视觉/动态效果由用户手测。本项目没有自动化测试，验证步骤是「Unity 编译 + Console 检查 + 编辑器检查 + 用户 Play 手测」。

---

## 文件结构

**创建：**

- `Assets/Scripts/Level/Portal/PortalSide.cs` — 传送门单侧数据（quad / camera / clipPlane / material / 运行时 RT）。
- `Assets/Scripts/Level/Portal/PortalPair.cs` — 传送门管理器（相机位姿、斜裁剪、瞬移、RT 生命周期）。
- `Assets/Shaders/PortalSurface.shader` — 屏幕空间 UV 采样 RT 的 URP Unlit shader。
- `Assets/Materials/Level/PortalSurface_Hall.mat`、`PortalSurface_Room.mat` — 两张传送面的材质。
- `Assets/Prefabs/Level/TestRoom.prefab` — 1.44×2.4×13.3 的测试走廊，含 P2 quad / 相机 / 裁剪面 / 三盏点光。

**修改：**

- `Assets/Scripts/Level/Door/IDoorMotion.cs` — 增加 `Progress`。
- `Assets/Scripts/Level/Door/RiseDoorMotion.cs`、`SlideApartDoorMotion.cs`、`HingeDoorMotion.cs` — 实现 `Progress`。
- `Assets/Scripts/Level/Door/AutoDoor.cs` — 增加 `OpenProgress` 与 `IsFullyOpen`。
- `Assets/Scenes/Level2.unity` — 用 TestRoom Prefab 实例替换旧 TestRoom 三个 Cube。
- `Assets/Scenes/Level1.unity` — 中间墙开洞 + 补面；新增 `= 关卡/口袋空间`（Prefab 实例）与 `= 关卡/大厅/空间叠加`（P1 quad / 相机 / 裁剪面 + `PortalPair`）；`SafeDoorInteract.exitOpened` 接线。

---

## 任务 1：门的开合进度

**文件：**

- 修改：`Assets/Scripts/Level/Door/IDoorMotion.cs`
- 修改：`Assets/Scripts/Level/Door/RiseDoorMotion.cs`
- 修改：`Assets/Scripts/Level/Door/SlideApartDoorMotion.cs`
- 修改：`Assets/Scripts/Level/Door/HingeDoorMotion.cs`
- 修改：`Assets/Scripts/Level/Door/AutoDoor.cs`

- [ ] **步骤 1：给 `IDoorMotion` 增加 `Progress`**

把 `Assets/Scripts/Level/Door/IDoorMotion.cs` 改成：

```csharp
namespace Game.Level
{
    /// <summary>门的一种开合方式：自己管自己的门扇位移或物理，AutoDoor 只负责开关状态与触发。</summary>
    public interface IDoorMotion
    {
        /// <summary>0 为完全关闭，1 为完全打开；传送门在开门完成后才允许通过。</summary>
        float Progress { get; }

        /// <summary>按场景配置记录关闭位、设置铰链限位；配置不全时打印错误并返回 false。</summary>
        bool TryConfigure();

        void Open();

        void Close();

        /// <summary>FixedUpdate 驱动（目前只有铰链门需要）。</summary>
        void FixedTick();

        /// <summary>释放补间等运行时句柄。</summary>
        void Dispose();
    }
}
```

- [ ] **步骤 2：`RiseDoorMotion` 实现 `Progress`**

在 `Assets/Scripts/Level/Door/RiseDoorMotion.cs` 的 `Dispose` 之前加入：

```csharp
        public float Progress
        {
            get
            {
                if (_doorPanel == null || _openHeight <= 0f)
                {
                    return 0f;
                }

                return Mathf.Clamp01(
                    (_doorPanel.localPosition.y - _closedPosition.y) / _openHeight);
            }
        }
```

- [ ] **步骤 3：`SlideApartDoorMotion` 实现 `Progress`**

在 `Assets/Scripts/Level/Door/SlideApartDoorMotion.cs` 的 `Dispose` 之前加入：

```csharp
        public float Progress
        {
            get
            {
                if (_leftLeaf == null || _rightLeaf == null || _slideDistance <= 0f)
                {
                    return 0f;
                }

                float left = (_closedLeftPosition.x - _leftLeaf.localPosition.x) / _slideDistance;
                float right = (_rightLeaf.localPosition.x - _closedRightPosition.x) / _slideDistance;
                return Mathf.Clamp01((left + right) * 0.5f);
            }
        }
```

- [ ] **步骤 4：`HingeDoorMotion` 实现 `Progress`**

在 `Assets/Scripts/Level/Door/HingeDoorMotion.cs` 的 `Dispose` 之前加入：

```csharp
        public float Progress
        {
            get
            {
                if (_hinge == null || Mathf.Approximately(_openAngle, _closedAngle))
                {
                    return 0f;
                }

                return Mathf.Clamp01(Mathf.InverseLerp(_closedAngle, _openAngle, _hinge.angle));
            }
        }
```

- [ ] **步骤 5：`AutoDoor` 暴露 `OpenProgress` 与 `IsFullyOpen`**

在 `Assets/Scripts/Level/Door/AutoDoor.cs` 的 `IsOpen` 属性之后加入：

```csharp
        /// <summary>门扇开合进度：0 完全关闭，接近 1 完全打开；旋转门可能略小于 1，判断完全打开请用 IsFullyOpen。</summary>
        public float OpenProgress => _motion != null ? _motion.Progress : 0f;

        /// <summary>门扇是否已基本到位。旋转门的电机会在剩余角度小于 0.5° 时停转，OpenProgress 可能停在略小于 1 的值，因此带容差判断。</summary>
        public bool IsFullyOpen(float epsilon = 0.99f) => OpenProgress >= epsilon;
```

- [ ] **步骤 6：编译与回归检查**

在 Unity 里等待编译完成，Console 无报错。打开安全门、电梯门、关卡二北门，确认开关动作与之前一致（用户手测）；`IsFullyOpen()` 在门完全打开后为 true（旋转门 `OpenProgress` 约 0.995，属正常）。需要临时验证时可在 Play 里 `Debug.Log`，验证后删除日志。

---

## 任务 2：PortalSurface shader、材质与 PortalSide

**文件：**

- 创建：`Assets/Shaders/PortalSurface.shader`
- 创建：`Assets/Materials/Level/PortalSurface_Hall.mat`、`Assets/Materials/Level/PortalSurface_Room.mat`
- 创建：`Assets/Scripts/Level/Portal/PortalSide.cs`

- [ ] **步骤 1：创建 shader**

新建 `Assets/Shaders/PortalSurface.shader`：

```hlsl
Shader "Game/PortalSurface"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "PortalSurface"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.screenPos.xy / input.screenPos.w;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            }
            ENDHLSL
        }
    }
}
```

- [ ] **步骤 2：创建两个材质**

在 Unity 的 Project 窗口打开 `Assets/Materials/Level`：

1. `Assets > Create > Material`，命名 `PortalSurface_Hall`；Shader 选 `Game/PortalSurface`；`_MainTex` 保持默认 black；保存。
2. 复制一份命名 `PortalSurface_Room`，同样使用 `Game/PortalSurface`。

两张材质先不指定贴图；运行时由 `PortalPair` 把 RT 赋给 `_MainTex`。

- [ ] **步骤 3：创建 `PortalSide`**

新建 `Assets/Scripts/Level/Portal/PortalSide.cs`：

```csharp
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
```

- [ ] **步骤 4：编译检查**

等待 Unity 编译，Console 无报错。此时 shader/材质/`PortalSide` 还没有被场景引用，属正常。

---

## 任务 3：PortalPair

**文件：**

- 创建：`Assets/Scripts/Level/Portal/PortalPair.cs`

- [ ] **步骤 1：创建 `PortalPair`**

新建 `Assets/Scripts/Level/Portal/PortalPair.cs`：

```csharp
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
        [LabelText("分辨率"), SerializeField] Vector2Int resolution = new Vector2Int(320, 180);

        bool _active;
        bool _valid;
        Vector3 _lastPlayerPosition;
        readonly Plane[] _frustumPlanes = new Plane[6];

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
            var normal = -clipPlane.forward;
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
```

- [ ] **步骤 2：编译检查**

等待 Unity 编译，Console 无报错。此时 `PortalPair` 还没有挂到场景里，属正常。

---

## 任务 4：重建 TestRoom 并做成 Prefab

**文件：**

- 修改：`Assets/Scenes/Level2.unity`
- 创建：`Assets/Prefabs/Level/TestRoom.prefab`

- [ ] **步骤 1：清理旧 TestRoom 几何**

打开 `Assets/Scenes/Level2.unity`。在 `TestRoom` 根节点下删除旧的 `Ground`、`LeftWall`、`RightWall` 三个 Cube（保留 `TestRoom` 根节点，位置 (0,0,0)）。

- [ ] **步骤 2：建走廊主体**

在 `TestRoom` 下用 `Cube` 建以下物体（Transform 的 position/scale 如下，rotation 全 0；全部保留 BoxCollider）：

| 名称 | Position | Scale |
| --- | --- | --- |
| `地板` | (0, -0.065, 6.65) | (1.70, 0.13, 13.3) |
| `左墙` | (0.785, 1.2, 6.65) | (0.13, 2.4, 13.3) |
| `右墙` | (-0.785, 1.2, 6.65) | (0.13, 2.4, 13.3) |
| `天花板` | (0, 2.465, 6.65) | (1.70, 0.13, 13.3) |
| `尽头封墙` | (0, 1.2, 13.365) | (1.70, 2.4, 0.13) |
| `入口侧壁_左` | (0.785, 1.2, -0.065) | (0.13, 2.4, 0.13) |
| `入口侧壁_右` | (-0.785, 1.2, -0.065) | (0.13, 2.4, 0.13) |

材质先沿用 TestRoom 现有材质；若要贴合大厅，可换成 `Assets/Materials/Level` 下的墙面材质，不影响逻辑。

- [ ] **步骤 3：加三盏点光**

在 `TestRoom` 下建三个 `Point Light`（`点光_1/2/3`），位置分别为 (0,2.2,2.5)、(0,2.2,7.0)、(0,2.2,11.5)，Range 6，Intensity 1.5，颜色暖白；阴影先关掉或只留一盏，按画面调。

- [ ] **步骤 4：建 P2 传送面**

在 `TestRoom` 下建 `Quad`，命名 `返回传送面`：

- Position (0, 1.2, 0)，Rotation (0, 180, 0)，Scale (1.44, 2.4, 1)；
- MeshRenderer 的 Material 用 `PortalSurface_Room`；
- 删除 `MeshCollider`（Quad 默认带），确保没有 Collider；
- 先保持 Active = false。

- [ ] **步骤 5：建 P2 相机**

在 `TestRoom` 下建 `Camera`，命名 `返回传送相机`：

- Position (0, 1.2, -0.2)，Rotation (0, 0, 0)；
- 添加 `UniversalAdditionalCameraData`（URP 项目里 Camera 通常已带；没有就 Add Component）；
- Depth 先设 -10，Target Texture 留空，Audio Listener 不要加；
- `Camera` 组件先保持 Enabled = false（`PortalPair` 运行时接管）。

- [ ] **步骤 6：建 P2 裁剪面标记**

在 `TestRoom` 下建 `Create Empty`，命名 `返回裁剪面`：

- Position (0, 1.2, 0)，Rotation (0, 0, 0)（forward = +z，指向走廊内）；
- 用 Unity MCP 的 `execute_code` 调 `EditorGUIUtility.SetIconForObject(go, EditorGUIUtility.IconContent("sv_icon_dot11_pix16_gizmo").image as Texture2D)` 设内置圆点图标；
- 保持 Active。

- [ ] **步骤 7：做成 Prefab**

把 `TestRoom` 根节点拖到 `Assets/Prefabs/Level/TestRoom.prefab`（`Level` 文件夹不存在就先建）。场景里的 `TestRoom` 变成 Prefab 实例。保存 `Level2.unity`。

- [ ] **步骤 8：检查**

确认 Prefab 和 Level2 实例里：走廊净空 1.44×2.4、长 13.3；P2 quad/camera 默认禁用；裁剪面 Active；三盏点光在走廊内；Console 无报错。Level2 的 Main Camera / Directional Light 可保留作测试用；若点光效果被方向光压住，把方向光强度调低或禁用后再看。

---

## 任务 5：Level1 中间墙开洞与补面

**文件：**

- 修改：`Assets/Scenes/Level1.unity`

- [ ] **步骤 1：准备中间墙**

打开 `Assets/Scenes/Level1.unity`，选中 `= 关卡/大厅/建筑/中间墙`。如果它不是 ProBuilder 网格，先用 `ProBuilderize` 转成 ProBuilder 网格；确认它是 12.8×3×0.3、中心 (0,1.5,1.5)。

- [ ] **步骤 2：布尔减出门洞**

用 ProBuilder 建一个临时 Cube：Position (0,1.2,1.5)、Scale (1.44,2.4,0.3)。选中 `中间墙`，用 ProBuilder 的 `Geometry > Boolean > Subtract`（或同版本的布尔工具）减去这个 Cube，得到贯穿墙厚的 1.44×2.4 门洞。删除临时 Cube。保留前表面 UV，不要整体重新展开。

- [ ] **步骤 3：补北侧开口**

把视角转到大厅北侧，确认门洞背面是空的。用 ProBuilder 的 `Fill Hole` / `Bridge` 在门洞北侧开口补一个面，让它与墙北面共面（z=1.65）；在 UV 编辑器里把补面 UV 对齐到墙北面原贴图区域，使从背面看贴图连续。材质保持 `Wall_Mid`。

如果补面 UV 无法对齐：退回一块 `Cube`，命名 `安全门背板`，Position (0,1.2,1.67)、Scale (1.5,2.46,0.04)，材质 `Wall_Mid`，作为门洞北侧的遮挡；它同时承担背面碰撞。

- [ ] **步骤 4：碰撞**

给开洞后的 `中间墙` 加/保留 `MeshCollider`（Convex 关闭），让洞与补面参与碰撞。若 CharacterController 在洞口边缘卡顿，改成左右/门楣三个不可见 `BoxCollider` + 补面 `BoxCollider`，并移除 `MeshCollider`。

- [ ] **步骤 5：检查**

保存场景；Console 无报错。在 Scene 视图里确认：门洞 1.44×2.4；从北侧看补面/背板是墙；从南侧看洞内能看到 P1 将要放的位置；前表面贴图没有三块拼接的接缝。

---

## 任务 6：Level1 口袋空间、P1/P2 与接线

**文件：**

- 修改：`Assets/Scenes/Level1.unity`

- [ ] **步骤 1：建口袋空间**

在 `= 关卡` 下建空物体 `口袋空间`，Position (0, -1000, 0)，Rotation 0。把 `Assets/Prefabs/Level/TestRoom.prefab` 拖进去作为子级（Prefab 实例的 localPosition 归零）。确认大厅的相机/灯光/碰撞都碰不到 y=-1000 的走廊。

- [ ] **步骤 2：建 P1 传送面**

在 `= 关卡/大厅` 下建空物体 `空间叠加`（Position 0），再在其下建 `Quad`，命名 `入口传送面`：

- Position (0, 1.2, 1.40)，Rotation (0, 0, 0)，Scale (1.44, 2.4, 1)；
- Material 用 `PortalSurface_Hall`；
- 删除 `MeshCollider`，确保没有 Collider；
- 先保持 Active（`PortalPair` 在 Awake 里会先禁用，开门后再启用）。

- [ ] **步骤 3：建 P1 相机**

在 `空间叠加` 下建 `Camera`，命名 `入口传送相机`：

- Position (0, 1.2, 1.22)，Rotation (0, 0, 0)；
- 添加 `UniversalAdditionalCameraData`；
- Depth -10，Target Texture 留空，不加 Audio Listener；
- `Camera` 先 Enabled = false。

- [ ] **步骤 4：建 P1 裁剪面标记**

在 `空间叠加` 下建 `Create Empty`，命名 `入口裁剪面`：

- Position (0, 1.2, 1.35)，Rotation (0, 180, 0)（forward = -z，指向大厅内）；
- 设内置圆点图标；保持 Active。

- [ ] **步骤 5：挂 PortalPair 并填引用**

给 `空间叠加` 加 `PortalPair` 组件，填：

- 玩家：`= 玩家`；玩家控制器：`= 玩家` 上的 `CharacterController`；
- 主相机：`= 相机/主相机`；Cinemachine Brain：`= 相机/主相机` 上的 `CinemachineBrain`；
- 安全门：`= 关卡/大厅/门/安全门` 上的 `AutoDoor`；
- 大厅侧：传送面 `入口传送面`、传送相机 `入口传送相机`、裁剪面 `入口裁剪面`、材质 `PortalSurface_Hall`；
- 走廊侧：传送面 `= 关卡/口袋空间/TestRoom/返回传送面`、传送相机 `…/返回传送相机`、裁剪面 `…/返回裁剪面`、材质 `PortalSurface_Room`；
- 触发距离 0.18、落点前移 0.25、分辨率 (320,180)。

- [ ] **步骤 6：接线 exitOpened**

选中 `= 关卡/大厅/门/安全门`，在 `SafeDoorInteract` 的 `exitOpened` UnityEvent 里点 `+`，把 `空间叠加` 拖进去，方法选 `PortalPair.Activate`。不要改 `SafeDoorInteract` 代码。

- [ ] **步骤 7：检查**

保存场景；Console 无报错。确认：

- `空间叠加` 下有 `入口传送面`、`入口传送相机`、`入口裁剪面`；
- `口袋空间` 实例下有 `返回传送面`、`返回传送相机`、`返回裁剪面`；
- P2 quad/camera 在 Prefab 里是禁用的（`PortalPair` 开门后启用）；
- 两张传送相机的 `UniversalAdditionalCameraData` 存在；
- 两张 quad 没有 Collider。

---

## 任务 7：用户手测与验收

**文件：** 无（Play 测试）

- [ ] **步骤 1：开门前**

Play `Level1`。安全门关着；从大厅北侧看中间墙只有墙，走不过去；看不到走廊；Console 无报错。

- [ ] **步骤 2：开门后**

拿真门禁卡，对准安全门交互开门。门升起时走廊透过门洞逐渐露出；左右移动有正确视差；中间墙前表面贴图连续，没有三块拼接的接缝。

- [ ] **步骤 3：走进走廊**

朝门走，在离传送面约 0.18 m 处无缝进入走廊；相机不飞、无黑帧；走廊 1.44×2.4、有尽头封墙与局部照明。

- [ ] **步骤 4：返回**

在走廊里转身，入口显示大厅南侧（不是墙）；走回去落在大厅安全门南侧约 0.25 m，面朝南，门在背后；不会立刻被弹回去。

- [ ] **步骤 5：斜看与背面**

斜着看传送面：画面正确、边缘与门框对齐、无黑缝；大厅光照不变、走廊灯光不漏进大厅。开门后再从大厅北侧看：仍然是墙，走不过去。

- [ ] **步骤 6：问题修复**

把 Console 报错、画面 pop、门框黑缝、落点位置、返回朝向等问题反馈给实现者；按规格的「风险与待手测」逐项修复。不要在没有手测结果的情况下宣称完成。

---

## 补充（2026-11-19）：传送距离的 Scene 辅助线

`PortalPair` 增加 `showGuideGizmos`（显示辅助线，默认开）与 `OnDrawGizmos()`：

- 每侧画三张面：白 = 传送面本体，橙 = 触发面（沿 `FrontNormal` 偏移 `triggerDistance`），绿 = 落点面（沿 `FrontNormal` 偏移 `landingOffset`）；三张面之间用同色竖线连起来。
- 右侧用 `Handles.Label` 标注 `触发距离 x m` / `落点前移 x m`，改数值时标签同步刷新。
- 只改 `Assets/Scripts/Level/Portal/PortalPair.cs`，不动场景与运行时逻辑；`Handles` 放在 `#if UNITY_EDITOR` 里，打包后不参与编译。

手测：选中 `空间叠加`，Scene 视图应看到门洞前两张贴近的橙/绿面（大厅侧 z≈1.22 / z≈1.15，走廊侧 z≈0.18 / z≈0.25），标签数值与 Inspector 一致。

---

## 补充（2026-11-19）：修复走廊内误触发大厅侧传送

**现象：** 进入口袋空间后走一两步（走廊 z≈1.22..1.4）被"传送"到 y≈-3000，编辑器里停住时玩家还在自由落体。

**根因：** `TryTeleport` 只检查玩家到传送面平面的距离/行进方向/横向范围；大厅侧 P1 在 z=1.4，而走廊世界 z 从 0 到 13.3，两者水平完全重叠。玩家在走廊里走到 z≈1.22..1.4 且朝 +z 移动时 P1 条件同样成立，落点 `target.y = roomQuad.y + (player.y - hallQuad.y)` 每次把玩家再往下挪 1000 m（-1000 → -2000 → -3000）。

**修复：** `PortalPair` 增加 `spaceHeightTolerance`（默认 3 m）；`TryTeleport` 先要求 `|player.position.y - source.quad.position.y| <= spaceHeightTolerance`，不满足直接返回。玩家脚底与门中心的正常高度差约 1.2 m，两个空间相隔 1000 m，该判定既能放行正常过门，也能排除对侧空间。

**验证（Play 中反射调用 `TryTeleport`）：**

- 走廊内 (0,-1000,1.3) 朝 +z：不传送（修复前会被传到 y=-2000）。
- 大厅 (0,0,1.25) 朝 +z：传送到 (0,-1000,0.25)。
- 走廊 (0,-1000,0.10) 朝 -z：返回 (0,0,1.15)。

仍需用户手测完整流程（开门 → 走进走廊 → 往返）。

---

## 自检

- **规格覆盖：** 中间墙开洞/补面（任务 5）；P1/P2 与裁剪面（任务 4/6）；PortalPair/PortalSide/shader/AutoDoor/事件（任务 1/2/3/6）；传送数学与瞬移（任务 3 的 `UpdateSideCamera`/`TryTeleport`/`GetObliqueProjection`）；RT/生命周期/错误处理（任务 3）；验收（任务 7）。
- **占位符：** 没有「待定/TODO/后续实现」；点光强度、材质替换属明确的美术调整步骤；补面 UV 与 MeshCollider 有明确的兜底方案。
- **类型一致性：** `PortalSide.renderTexture` 在 `PortalPair` 的 `CreateRenderTexture`/`ReleaseRenderTexture` 中使用；`IDoorMotion.Progress` 由三个 motion 实现、`AutoDoor.OpenProgress` 读取；`GetRotation` 的 `visible/other` 与 `UpdateSideCamera`/`TryTeleport` 的调用顺序一致；`GetObliqueProjection` 的 `source` 是主相机、`target` 是传送相机、`clipPlane` 是目的地侧。

## 执行交接

计划已保存。两种执行方式：

1. **子代理驱动（推荐）** — 每个任务调度一个新的子代理，任务间进行审查，快速迭代。
2. **内联执行** — 在当前会话中使用 `executing-plans` 逐任务执行，批量推进并设检查点。

选哪种方式？
