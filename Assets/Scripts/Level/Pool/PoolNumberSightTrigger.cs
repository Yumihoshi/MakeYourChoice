using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 池底数字的视线判定：玩家在观察区内、距离够近、数字在视口内且水平角对准时触发一次。
    /// 刻意不用 ScreenVisibility：池边空气墙的 Collider 会挡住它的 Physics.Linecast。
    /// </summary>
    public sealed class PoolNumberSightTrigger : MonoBehaviour
    {
        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, LabelText("视线起点（玩家眼睛）")]
        Transform eye;

        [SerializeField, LabelText("判定相机")]
        Camera viewCamera;

        [SerializeField, Required, LabelText("池底数字")]
        Transform target;

        [SerializeField, Required, LabelText("观察区")]
        BoxCollider observationArea;

        [SerializeField, Required, LabelText("剧情")]
        DialogueTrigger dialogue;

        [SerializeField, LabelText("要解锁的柜门")]
        CabinetDoorInteract[] unlockDoors;

        [SerializeField, LabelText("可视距离")]
        float maxDistance = 15f;

        [SerializeField, LabelText("对准角度")]
        float maxAngle = 20f;

        readonly RaycastHit[] _sightHits = new RaycastHit[16];

        bool _fired;

        void Awake()
        {
            if (state == null || target == null || observationArea == null || dialogue == null)
            {
                Debug.LogError("PoolNumberSightTrigger 缺少流程状态 / 池底数字 / 观察区 / 剧情引用", this);
                enabled = false;
                return;
            }

            var mainCamera = Camera.main;
            if (eye == null)
            {
                if (mainCamera == null)
                {
                    Debug.LogError("PoolNumberSightTrigger 的视线起点为空，且场景里没有 Main Camera", this);
                    enabled = false;
                    return;
                }

                eye = mainCamera.transform;
            }

            if (viewCamera == null)
            {
                viewCamera = mainCamera;
                if (viewCamera == null)
                {
                    Debug.LogError("PoolNumberSightTrigger 的判定相机为空，且场景里没有 Main Camera", this);
                    enabled = false;
                    return;
                }
            }

            if (unlockDoors == null || unlockDoors.Length == 0)
            {
                Debug.LogError("PoolNumberSightTrigger 没有配置要解锁的暗门", this);
            }
        }

        void Update()
        {
            if (_fired || state == null || target == null || observationArea == null || dialogue == null)
            {
                return;
            }

            if (state.PoolNumberSeen || dialogue.IsRunning || !GameCursorState.IsPlaying)
            {
                return;
            }

            if (!observationArea.bounds.Contains(eye.position))
            {
                return;
            }

            if (Vector3.Distance(eye.position, target.position) > maxDistance)
            {
                return;
            }

            var viewport = viewCamera.WorldToViewportPoint(target.position);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
            {
                return;
            }

            var toTarget = target.position - eye.position;
            var flatForward = eye.forward;
            toTarget.y = 0f;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.0001f || toTarget.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (Vector3.Angle(flatForward, toTarget) > maxAngle)
            {
                return;
            }

            if (!HasLineOfSight())
            {
                return;
            }

            _fired = true;
            state.MarkPoolNumberSeen();

            if (unlockDoors != null)
            {
                foreach (var door in unlockDoors)
                {
                    if (door != null)
                    {
                        door.Unlock();
                        door.Open();
                    }
                }
            }

            dialogue.StartDialogue();
        }

        /// <summary>
        /// 眼睛到数字之间是否没有可见几何遮挡。空气墙只有碰撞体、没有渲染器，忽略；
        /// 地板、池壁等有渲染器的实体视为遮挡。
        /// </summary>
        bool HasLineOfSight()
        {
            var origin = eye.position;
            var toTarget = target.position - origin;
            var distance = toTarget.magnitude;
            if (distance <= 0.0001f)
            {
                return true;
            }

            var count = Physics.RaycastNonAlloc(
                origin,
                toTarget / distance,
                _sightHits,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var hit = _sightHits[i];
                var hitTransform = hit.collider.transform;
                if (hitTransform == target || hitTransform.IsChildOf(target) || target.IsChildOf(hitTransform))
                {
                    continue;
                }

                if (HasVisibleRenderer(hit.collider))
                {
                    return false;
                }
            }

            return true;
        }

        static bool HasVisibleRenderer(Collider collider)
        {
            var renderer = collider.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = collider.GetComponentInChildren<Renderer>();
            }

            return renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy;
        }
    }
}
