using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 准心与交互按键提示：游戏中显示准心；玩家瞄着可交互物时，
    /// 在准心右侧显示最近使用设备的交互按键图标（键鼠 / 手柄各一张）。
    /// </summary>
    public sealed class InteractPromptView : MonoBehaviour
    {
        [SerializeField, Required, LabelText("准心")]
        Image crosshair;

        [SerializeField, Required, LabelText("交互按键图标")]
        Image keyIcon;

        [SerializeField, Required, LabelText("键鼠交互键")]
        Sprite keyboardIcon;

        [SerializeField, Required, LabelText("手柄交互键")]
        Sprite gamepadIcon;

        void LateUpdate()
        {
            bool inGame = Cursor.lockState == CursorLockMode.Locked;
            SetGraphicEnabled(crosshair, inGame);

            bool showKey = inGame && InteractFocus.Current != null;
            SetGraphicEnabled(keyIcon, showKey);
            if (!showKey)
            {
                return;
            }

            var router = PlayerInputRouter.Instance;
            var kind = router != null ? router.CurrentDevice : InputDeviceKind.KeyboardMouse;
            keyIcon.sprite = kind == InputDeviceKind.Gamepad ? gamepadIcon : keyboardIcon;
            keyIcon.preserveAspect = true;
        }

        static void SetGraphicEnabled(Graphic graphic, bool enabled)
        {
            if (graphic != null && graphic.enabled != enabled)
            {
                graphic.enabled = enabled;
            }
        }
    }
}
