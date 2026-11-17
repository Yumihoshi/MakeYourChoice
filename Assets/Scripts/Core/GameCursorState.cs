using UnityEngine;

namespace Game
{
    /// <summary>
    /// 光标的唯一写入口：菜单打开与到站演出期间解锁光标，其余时间锁定并隐藏，进入第一人称。
    /// 光标锁定同时被各触发区当作「游玩中」信号，所以这里只读地暴露 <see cref="IsPlaying"/>。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class GameCursorState : MonoBehaviour
    {
        /// <summary>玩家现在是否处于可操作状态（光标已锁定）。</summary>
        public static bool IsPlaying => Cursor.lockState == CursorLockMode.Locked;

        /// <summary>菜单是否打开（由 GameBootstrap 每帧告知）。</summary>
        public bool MenuOpen { get; set; }

        /// <summary>到站演出是否还在播：期间不显示光标，但也不能操作。</summary>
        public bool Settling { get; set; }

        // 放在 LateUpdate：保证本帧内所有读取方都先拿到上一帧已生效的状态，写入时机唯一且确定。
        void LateUpdate()
        {
            bool unlock = MenuOpen || Settling;

            if (unlock)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                }

                // 菜单里要用光标点按钮；演出期间没有可点的东西，藏起来免得鼠标在画面里乱跑。
                if (Cursor.visible != MenuOpen)
                {
                    Cursor.visible = MenuOpen;
                }
            }
            else
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }

                // 窗口失焦再切回来时，系统会把光标重新显示出来，而锁定状态可能已经恢复；这里每帧兜底隐藏。
                if (Cursor.visible)
                {
                    Cursor.visible = false;
                }
            }
        }
    }
}
