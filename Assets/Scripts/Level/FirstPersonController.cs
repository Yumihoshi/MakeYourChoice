using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 第一人称移动：CharacterController + 新版 Input System 的动作输入，只允许左右转。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [LabelText("移动速度"), SerializeField] float moveSpeed = 4f;
        [LabelText("奔跑倍率"), SerializeField] float sprintMultiplier = 1.8f;
        [LabelText("鼠标灵敏度"), SerializeField] float lookSensitivity = 0.12f;
        [LabelText("重力"), SerializeField] float gravity = -20f;

        CharacterController _controller;
        float _yaw;
        float _verticalVelocity;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _yaw = transform.eulerAngles.y;
        }

        void Update()
        {
            var input = PlayerInputRouter.Instance;
            if (input == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            HandleLook(input);
            HandleMove(input);
        }

        void HandleLook(PlayerInputRouter input)
        {
            // 鼠标是位移量、手柄摇杆是推量，两者换算方式不同。
            float degreesPerFrame = input.CurrentDevice == InputDeviceKind.Gamepad
                ? input.ReadGamepadLookDegreesPerSecond() * Time.deltaTime
                : input.Look.ReadValue<Vector2>().x * lookSensitivity;

            _yaw += degreesPerFrame;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        void HandleMove(PlayerInputRouter input)
        {
            var axis = Vector2.ClampMagnitude(input.Move.ReadValue<Vector2>(), 1f);

            var speed = moveSpeed;
            if (input.Sprint.IsPressed())
            {
                speed *= sprintMultiplier;
            }

            var move = (transform.right * axis.x + transform.forward * axis.y) * speed;
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += gravity * Time.deltaTime;
            move.y = _verticalVelocity;
            _controller.Move(move * Time.deltaTime);
        }
    }
}
