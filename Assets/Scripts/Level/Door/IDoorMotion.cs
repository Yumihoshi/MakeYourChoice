namespace Game.Level
{
    /// <summary>门的一种开合方式：自己管自己的门扇位移或物理，AutoDoor 只负责开关状态与触发。</summary>
    public interface IDoorMotion
    {
        /// <summary>0 为完全关闭，接近 1 为完全打开；旋转门受电机死区影响可能略小于 1，消费侧用 AutoDoor.IsFullyOpen 判断。</summary>
        float Progress { get; }

        /// <summary>按场景配置记录关闭位、设置铰链限位；配置不全时打印错误并返回 false。</summary>
        bool TryConfigure();

        void Open();

        void Close();

        /// <summary>FixedUpdate 驱动（目前只有铰链门需要）。</summary>
        void FixedTick();

        /// <summary>释放补间等运行时句柄。</summary>
        void Dispose();
    }
}
