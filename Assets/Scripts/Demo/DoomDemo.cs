using UnityEngine;
using UnityEngine.SceneManagement;
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

        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float runMultiplier = 1.7f;
        [SerializeField] private float turnSpeed = 130f;
        [SerializeField] private float mouseSensitivity = 2.6f;
        [SerializeField] private Vector2 startPosition = new Vector2(8.5f, 6.5f);
        [SerializeField] private float startYaw = 90f;

        private DoomDemoMap _map;
        private DoomRaycaster _raycaster;
        private Texture2D _frame;
        private Color32[] _upload;

        private Vector2 _position;
        private float _yaw;

        /// <summary>DoomDemo 场景是空场景，靠这里自动把自己挂起来。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (SceneManager.GetActiveScene().name != "DoomDemo")
            {
                return;
            }

            if (FindObjectOfType<DoomDemo>() != null)
            {
                return;
            }

            new GameObject("DoomDemo").AddComponent<DoomDemo>();
        }

        private void Awake()
        {
            _map = new DoomDemoMap();
            _raycaster = new DoomRaycaster(Width, Height);
            _upload = new Color32[Width * Height];
            _position = startPosition;
            _yaw = startYaw;

            Application.targetFrameRate = 60;

            EnsureCamera();
            CreateScreen();
        }

        private void Update()
        {
            HandleInput();
            RenderFrame();
        }

        private void HandleInput()
        {
            float deltaTime = Mathf.Min(Time.deltaTime, 0.05f);

            float forward = 0f;
            float strafe = 0f;
            float turn = 0f;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                forward += 1f;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                forward -= 1f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                strafe += 1f;
            }

            if (Input.GetKey(KeyCode.A))
            {
                strafe -= 1f;
            }

            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.RightArrow))
            {
                turn += 1f;
            }

            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow))
            {
                turn -= 1f;
            }

            turn += Input.GetAxisRaw("Mouse X") * mouseSensitivity;

            _yaw -= turn * turnSpeed * deltaTime;

            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? runMultiplier : 1f);
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

        private void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            GameObject cameraObject = new GameObject("DoomDemoCamera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private void CreateScreen()
        {
            _frame = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = "DoomDemoFrame",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            GameObject canvasObject = new GameObject("DoomDemoCanvas", typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            GameObject screenObject = new GameObject("Screen",
                typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            screenObject.transform.SetParent(canvasObject.transform, false);

            RectTransform rect = (RectTransform)screenObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            AspectRatioFitter fitter = screenObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)Width / Height;

            RawImage image = screenObject.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.texture = _frame;
        }

        private void OnGUI()
        {
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(14f, 12f, 900f, 24f),
                "WASD / arrows: move    Q E / arrows: turn    mouse X: turn    Shift: run");
        }
    }
}
