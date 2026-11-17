using QFramework;

namespace Game
{
    /// <summary>
    /// 跨关卡剧情的只读进度。用 <see cref="BindableProperty{T}"/> 同时提供「读」与「变更通知」：
    /// 置位方直接写 <c>.Value</c>，消费方读 <c>.Value</c> 或 <c>Register</c> 订阅，
    /// 所以它既不持有 <see cref="Game.Level.HallFlowState"/>，也不需要额外的事件类型与中间缓存。
    /// </summary>
    public static class GameProgress
    {
        /// <summary>电梯门是否已经打开（关卡一触发 2）。</summary>
        public static readonly BindableProperty<bool> ElevatorDoorOpened = new BindableProperty<bool>(false);

        /// <summary>玩家是否已经第一次走进大厅（中间墙两侧的缝隙共用）。</summary>
        public static readonly BindableProperty<bool> EnteredLobby = new BindableProperty<bool>(false);

        /// <summary>把两个进度复位。关掉 Domain Reload 后静态值会跨 Play 残留，入口必须调用。</summary>
        public static void Reset()
        {
            ElevatorDoorOpened.SetValueWithoutEvent(false);
            EnteredLobby.SetValueWithoutEvent(false);
        }
    }
}
