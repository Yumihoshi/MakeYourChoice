using UnityEngine;
using QFramework;
using Game.UI;

namespace Game
{
	/// <summary>
	/// 游戏场景入口：进入场景后打开开始菜单。
	/// </summary>
	public class GameBootstrap : MonoBehaviour
	{
		/// <summary>
		/// 关闭 Domain Reload 后 UIKit 的静态状态会跨 Play 残留，进入 Play 前重置一次。
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void ResetUIKitState()
		{
			UIKit.Config = new UIKitConfig();
			UIKit.Config.PanelLoaderPool = new ResKitPanelLoaderPool();
			UIKit.Table.Clear();
		}

		void Start()
		{
			UIKit.OpenPanel<StartPanel>();
		}
	}
}
