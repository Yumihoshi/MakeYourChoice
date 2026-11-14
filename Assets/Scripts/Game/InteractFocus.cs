using UnityEngine;

namespace Game
{
    /// <summary>
    /// 准心提示的当前交互目标：可交互物在"玩家正瞄着它"时把自己登记进来，
    /// 准心 HUD 据此决定是否显示交互按键图标。
    /// </summary>
    public static class InteractFocus
    {
        /// <summary>当前可以被交互的目标，没有则为 null。</summary>
        public static Component Current { get; private set; }

        /// <summary>登记交互目标。</summary>
        public static void Set(Component source) => Current = source;

        /// <summary>取消登记，只有当前目标是自己时才清空。</summary>
        public static void Clear(Component source)
        {
            if (Current == source)
            {
                Current = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current = null;
        }
    }
}
