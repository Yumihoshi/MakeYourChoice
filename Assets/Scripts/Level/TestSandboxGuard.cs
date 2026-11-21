using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 测试沙盒守卫：场景里已经存在别的第一人称玩家时销毁自身容器，
    /// 让"为方便单独运行关卡而放进去的测试玩家"不会和正式流程的玩家重复。
    /// 挂在测试容器根节点上，容器内通常只有一个玩家预制体实例。
    /// </summary>
    public sealed class TestSandboxGuard : MonoBehaviour
    {
        void Start()
        {
            var players = FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None);
            if (players.Length <= 1) return;

            Debug.Log("[测试沙盒] 场景里已有 " + players.Length + " 个玩家，移除测试容器 " + name + "。", this);
            Destroy(gameObject);
        }
    }
}
