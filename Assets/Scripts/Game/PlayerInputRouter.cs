using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    /// <summary>最近一次输入来自哪类设备，用于切换准心旁的按键图标。</summary>
    public enum InputDeviceKind
    {
        KeyboardMouse,
        Gamepad,
    }

    /// <summary>
    /// 玩家输入的统一切口：按策划案 3C 键位表配置新版 Input System 的键盘 / 鼠标 / 手柄绑定，
    /// 并记录最近使用的设备。键位集中在这里，场景脚本只读动作，不直接判断按键。
    /// </summary>
    public sealed class PlayerInputRouter : MonoBehaviour
    {
        public static PlayerInputRouter Instance { get; private set; }

        [SerializeField, LabelText("手柄视角速度"), Tooltip("右摇杆推满时每秒转动的角度")]
        float gamepadLookDegreesPerSecond = 180f;

        /// <summary>移动输入，WASD 或左摇杆。</summary>
        public InputAction Move { get; private set; }

        /// <summary>视角输入，鼠标位移或右摇杆。</summary>
        public InputAction Look { get; private set; }

        /// <summary>交互，鼠标左键或手柄 A。</summary>
        public InputAction Interact { get; private set; }

        /// <summary>加速走，Shift 或手柄扳机。</summary>
        public InputAction Sprint { get; private set; }

        /// <summary>菜单，Esc 或手柄 Start。</summary>
        public InputAction Menu { get; private set; }

        /// <summary>最近一次产生输入的设备类型。</summary>
        public InputDeviceKind CurrentDevice { get; private set; } = InputDeviceKind.KeyboardMouse;

        /// <summary>设备切换时触发，参数是新设备类型。</summary>
        public event Action<InputDeviceKind> DeviceChanged;

        InputActionMap _map;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        void Awake()
        {
            Instance = this;
            BuildActions();
        }

        void OnEnable() => _map?.Enable();

        void OnDisable() => _map?.Disable();

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            _map?.Dispose();
            _map = null;
        }

        void Update() => RefreshDevice();

        /// <summary>
        /// 手柄视角的角速度（度/秒）：右摇杆推量 × 灵敏度。
        /// 鼠标视角是位移量，不需要再乘时间，所以由调用方按设备区分。
        /// </summary>
        public float ReadGamepadLookDegreesPerSecond() =>
            Look.ReadValue<Vector2>().x * gamepadLookDegreesPerSecond;

        void BuildActions()
        {
            _map = new InputActionMap("Player");

            Move = _map.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");

            Look = _map.AddAction("Look", InputActionType.Value);
            Look.AddBinding("<Mouse>/delta");
            Look.AddBinding("<Gamepad>/rightStick");

            Interact = _map.AddAction("Interact", InputActionType.Button);
            Interact.AddBinding("<Mouse>/leftButton");
            Interact.AddBinding("<Gamepad>/buttonSouth");

            Sprint = _map.AddAction("Sprint", InputActionType.Button);
            Sprint.AddBinding("<Keyboard>/leftShift");
            Sprint.AddBinding("<Keyboard>/rightShift");
            Sprint.AddBinding("<Gamepad>/leftTrigger");
            Sprint.AddBinding("<Gamepad>/rightTrigger");

            Menu = _map.AddAction("Menu", InputActionType.Button);
            Menu.AddBinding("<Keyboard>/escape");
            Menu.AddBinding("<Gamepad>/start");

            _map.Enable();
        }

        void RefreshDevice()
        {
            var gamepad = Gamepad.current;
            if (gamepad != null && IsGamepadActive(gamepad))
            {
                SetDevice(InputDeviceKind.Gamepad);
                return;
            }

            var mouse = Mouse.current;
            if (mouse != null &&
                (mouse.delta.ReadValue().sqrMagnitude > 1f || mouse.leftButton.isPressed || mouse.rightButton.isPressed))
            {
                SetDevice(InputDeviceKind.KeyboardMouse);
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.isPressed)
            {
                SetDevice(InputDeviceKind.KeyboardMouse);
            }
        }

        static bool IsGamepadActive(Gamepad gamepad) =>
            gamepad.leftStick.ReadValue().sqrMagnitude > 0.04f
            || gamepad.rightStick.ReadValue().sqrMagnitude > 0.04f
            || gamepad.dpad.ReadValue().sqrMagnitude > 0.04f
            || gamepad.buttonSouth.isPressed || gamepad.buttonEast.isPressed
            || gamepad.buttonWest.isPressed || gamepad.buttonNorth.isPressed
            || gamepad.startButton.isPressed
            || gamepad.leftTrigger.ReadValue() > 0.2f || gamepad.rightTrigger.ReadValue() > 0.2f;

        void SetDevice(InputDeviceKind kind)
        {
            if (CurrentDevice == kind)
            {
                return;
            }

            CurrentDevice = kind;
            DeviceChanged?.Invoke(kind);
        }
    }
}
