using UnityEngine;

namespace Game
{
    /// <summary>项目里按名字取层掩码的地方集中在这里，避免各脚本各写一份。</summary>
    public static class GameLayers
    {
        /// <summary>除玩家层以外的所有层：射线/视线检测默认打这些。</summary>
        public static int EverythingButPlayer => ~LayerMask.GetMask("Player");
    }
}
