using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Demo
{
    /// <summary>
    /// 伪 3D 演示入口：运行时自建相机与全屏画面、处理输入，并把 <see cref="DoomRaycaster"/>
    /// 输出的像素缓冲贴到屏幕。因为全部走 CPU，2D URP 工程里不需要任何额外管线配置。
    /// </summary>
    public sealed class DoomDemo : MonoBehaviour
    {
        private const int Width = 320;
        private const int Height = 200;

        [SerializeField, LabelText("移动速度")]
        private float moveSpeed = 3.2f;

        [SerializeField, LabelText("奔跑速度倍率")]
        private float runMultiplier = 1.7f;

        [SerializeField, LabelText("转向速度")]
        private float turnSpeed = 130f;

        [SerializeField, LabelText("鼠标灵敏度")]
        private float mouseSensitivity = 2.6f;

        [SerializeField, LabelText("起始位置")]
        private Vector2 startPosition = new Vector2(8.5f, 6.5f);

        [SerializeField, LabelText("起始朝向")]
        private float startYaw = 90f;

        [SerializeField, LabelText("画面")]
        private RawImage screen;

        private DoomDemoMap _map;
        private DoomRaycaster _raycaster;
        private Texture2D _frame;
        private Color32[] _upload;

        private Vector2 _position;
        private float _yaw;

        private void Awake()
        {
            _map = new DoomDemoMap();
            _raycaster = new DoomRaycaster(Width, Height);
            _upload = new Color32[Width * Height];
            _position = startPosition;
            _yaw = startYaw;

            Application.targetFrameRate = 60;

            CreateFrame();
        }

        private void Update()
        {
            HandleInput();
            RenderFrame();
        }

        private void HandleInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                // 没有键盘设备（无头运行等）时没有可读的输入，跳过本帧。
                return;
            }

            float deltaTime = Mathf.Min(Time.deltaTime, 0.05f);

            float forward = 0f;
            float strafe = 0f;
            float turn = 0f;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                forward += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                forward -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                strafe += 1f;
            }

            if (keyboard.aKey.isPressed)
            {
                strafe -= 1f;
            }

            if (keyboard.eKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                turn += 1f;
            }

            if (keyboard.qKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                turn -= 1f;
            }

            // 旧版 "Mouse X" 轴自带 0.1 的灵敏度缩放，新版 delta 是原始像素位移，乘回 0.1 保持同样手感。
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                turn += mouse.delta.ReadValue().x * 0.1f * mouseSensitivity;
            }

            _yaw -= turn * turnSpeed * deltaTime;

            float speed = moveSpeed * (keyboard.leftShiftKey.isPressed ? runMultiplier : 1f);
            if (speed <= 0f)
            {
                return;
            }

            float radians = _yaw * Mathf.Deg2Rad;
            float dirX = Mathf.Cos(radians);
            float dirY = Mathf.Sin(radians);
            float rightX = dirY;
            float rightY = -dirX;

            Vector2 direction = new Vector2(
                dirX * forward + rightX * strafe,
                dirY * forward + rightY * strafe);

            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            MoveWithCollision(direction * (speed * deltaTime));
        }

        private void MoveWithCollision(Vector2 delta)
        {
            // 分轴推进，贴墙时可以滑动而不是卡死。
            if (!Blocked(_position.x + delta.x, _position.y))
            {
                _position.x += delta.x;
            }

            if (!Blocked(_position.x, _position.y + delta.y))
            {
                _position.y += delta.y;
            }
        }

        private bool Blocked(float x, float y)
        {
            const float radius = 0.24f;
            return _map.IsWall(Mathf.FloorToInt(x - radius), Mathf.FloorToInt(y - radius))
                || _map.IsWall(Mathf.FloorToInt(x + radius), Mathf.FloorToInt(y - radius))
                || _map.IsWall(Mathf.FloorToInt(x - radius), Mathf.FloorToInt(y + radius))
                || _map.IsWall(Mathf.FloorToInt(x + radius), Mathf.FloorToInt(y + radius));
        }

        private void RenderFrame()
        {
            _raycaster.Render(_map, _position, _yaw);
            _raycaster.DrawMinimap(_map, _position, _yaw);

            // Texture2D 的行序是自下而上，这里翻一次。
            Color32[] source = _raycaster.Buffer;
            for (int y = 0; y < Height; y++)
            {
                System.Array.Copy(source, y * Width, _upload, (Height - 1 - y) * Width, Width);
            }

            _frame.SetPixels32(_upload);
            _frame.Apply(false);
        }

        /// <summary>
        /// 生成本次运行的画面纹理并挂到场景里的 RawImage 上；纹理只能在运行时创建，无法序列化。
        /// </summary>
        private void CreateFrame()
        {
            if (screen == null)
            {
                Debug.LogError("DoomDemo：场景里的 RawImage 引用没有连线，画面无法显示。", this);
                return;
            }

            _frame = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = "DoomDemoFrame",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            screen.texture = _frame;
        }

        private void OnGUI()
        {
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(14f, 12f, 900f, 24f),
                "WASD / arrows: move    Q E / arrows: turn    mouse X: turn    Shift: run");
        }
    }
}
