using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 文件柜 A 第三层夹层里的真门禁卡：跟随抽屉升起显示，露出后玩家可以瞄准它按交互键收走。
    /// 第一次拉开（触发 21）仍是反光，卡不露出；第二次拉开（触发 22）拿到卡后卡片从底板下升起，
    /// 抽屉拉开期间一直可见，把抽屉推回去或拾取后收起。
    /// </summary>
    public sealed class FilingCabinetCard : InteractableBase
    {
        [SerializeField, Required, LabelText("跟随的抽屉节点")]
        Transform drawerMount;

        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, LabelText("抽屉内本地位置（未露出）")]
        Vector3 hiddenLocalPositionInDrawer = new Vector3(0f, -0.155f, 0.3f);

        [SerializeField, LabelText("抽屉内本地位置（露出时）")]
        Vector3 revealedLocalPositionInDrawer = new Vector3(0f, 0f, 0.3f);

        [SerializeField, LabelText("抽屉内本地旋转")]
        Vector3 localEulerAnglesInDrawer = new Vector3(90f, 180f, 0f);

        [SerializeField, LabelText("升起耗时")]
        float riseSeconds = 0.25f;

        FilingCabinetDrawer _drawer;
        Renderer[] _renderers;
        Collider[] _colliders;
        float _revealStart = -1f;
        bool _visible;
        bool _pickedUp;
        bool _errorReported;

        /// <summary>卡片当前是否露出且还没被拾取（只有这时才允许交互）。</summary>
        public bool IsRevealed => _visible && !_pickedUp;

        protected override void Awake()
        {
            base.Awake();

            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponents<Collider>();
            if (drawerMount != null)
            {
                _drawer = drawerMount.GetComponent<FilingCabinetDrawer>();
            }

            // 没露出、被推回或已拾取时，卡片不能参与交互射线检测。
            SetVisible(false);
        }

        void LateUpdate()
        {
            if (drawerMount == null || state == null || _drawer == null)
            {
                if (!_errorReported)
                {
                    _errorReported = true;
                    Debug.LogError("FilingCabinetCard 缺少抽屉节点、关卡状态或抽屉组件引用", this);
                }

                return;
            }

            // 还没拿到真卡、已被拾取，或抽屉已被推回去，卡片都藏在底板下面。
            if (_pickedUp || !state.GotRealCard || !_drawer.IsOpen)
            {
                _revealStart = -1f;
                SetVisible(false);
                MoveTo(hiddenLocalPositionInDrawer);
                return;
            }

            if (_revealStart < 0f)
            {
                _revealStart = Time.time;
            }

            float elapsed = Time.time - _revealStart;
            float t = riseSeconds > 0f ? Mathf.Clamp01(elapsed / riseSeconds) : 1f;
            SetVisible(true);
            MoveTo(Vector3.Lerp(hiddenLocalPositionInDrawer, revealedLocalPositionInDrawer, t));
        }

        /// <summary>卡片露出且未被拾取时才允许收走。</summary>
        protected override bool CanInteract()
        {
            return state != null && IsRevealed;
        }

        /// <summary>把卡片收走：隐藏并永久退出交互，拿到真卡的状态由柜子 A 在第二次拉开时记录。</summary>
        protected override void OnInteract()
        {
            _pickedUp = true;
            SetVisible(false);
        }

        void MoveTo(Vector3 localPosition)
        {
            transform.SetPositionAndRotation(
                drawerMount.TransformPoint(localPosition),
                drawerMount.rotation * Quaternion.Euler(localEulerAnglesInDrawer));
        }

        void SetVisible(bool visible)
        {
            _visible = visible;
            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }

            foreach (Collider collider in _colliders)
            {
                if (collider != null)
                {
                    collider.enabled = visible;
                }
            }
        }
    }
}
