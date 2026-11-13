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

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				TogglePausePanel();
			}
		}

		/// <summary>
		/// ESC 开关暂停面板；开始菜单打开时忽略，避免在菜单界面弹出暂停面板。
		/// </summary>
		static void TogglePausePanel()
		{
			if (UIKit.GetPanel<PausePanel>())
			{
				UIKit.ClosePanel<PausePanel>();
				return;
			}

			if (UIKit.GetPanel<StartPanel>())
			{
				return;
			}

			UIKit.OpenPanel<PausePanel>();
		}
	}
}
