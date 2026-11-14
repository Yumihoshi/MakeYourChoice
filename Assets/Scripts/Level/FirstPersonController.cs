using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Level
{
    /// <summary>
    /// 第一人称移动：CharacterController + 新版 Input System，只允许左右转。
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
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            HandleLook();
            HandleMove();
        }

        void HandleLook()
        {
            if (Mouse.current == null)
            {
                return;
            }

            _yaw += Mouse.current.delta.ReadValue().x * lookSensitivity;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        void HandleMove()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            var input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            var speed = moveSpeed;
            if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
            {
                speed *= sprintMultiplier;
            }

            var move = (transform.right * input.x + transform.forward * input.y) * speed;
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
