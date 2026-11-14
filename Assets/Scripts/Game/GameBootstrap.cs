using System.Collections;
using Game.Level;
using Game.UI;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game
{
	/// <summary>
	/// 游戏场景入口：进入场景后打开开始菜单，并驱动菜单背后的电梯下行与到站演出。
	/// </summary>
	public class GameBootstrap : MonoBehaviour
	{
		[LabelText("楼层显示"), Required, SerializeField] ElevatorFloorDisplay floorDisplay;
		[LabelText("电梯运行晃动"), Required, SerializeField] ElevatorRideMotion rideMotion;
		[LabelText("到站楼层"), SerializeField] int arrivalFloor = 1;
		[LabelText("到站后关闭菜单的延迟"), SerializeField] float closeDelay = 0.65f;

		bool _starting;

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
			if (floorDisplay == null || rideMotion == null)
			{
				Debug.LogError("GameBootstrap 缺少楼层显示或电梯晃动引用，开始菜单将跳过电梯到站演出", this);
				UIKit.OpenPanel<StartPanel>(new StartPanelData(() => UIKit.ClosePanel<StartPanel>()));
				return;
			}

			floorDisplay.BeginIdleDescent();
			rideMotion.Ride();
			UIKit.OpenPanel<StartPanel>(new StartPanelData(OnStartRequested));
		}

		void Update()
		{
			var input = PlayerInputRouter.Instance;
			if (input != null && input.Menu.WasPressedThisFrame())
			{
				TogglePausePanel();
			}

			UpdateCursorState();
		}

		/// <summary>玩家点击开始：让电梯从当前楼层减速停到目标楼层。</summary>
		void OnStartRequested()
		{
			if (_starting)
			{
				return;
			}

			_starting = true;
			if (!floorDisplay.ArriveAt(arrivalFloor, OnArrived))
			{
				_starting = false;
				Debug.LogError("电梯已经在到站过程中，忽略了重复的开始请求", this);
			}
		}

		/// <summary>电梯停稳：播放镜头下沉回弹，稍后关闭开始菜单把控制权交还玩家。</summary>
		void OnArrived()
		{
			rideMotion.Arrive();
			StartCoroutine(CloseStartPanelAfterArrival());
		}

		IEnumerator CloseStartPanelAfterArrival()
		{
			yield return new WaitForSeconds(closeDelay);
			_starting = false;
			UIKit.ClosePanel<StartPanel>();
		}

		/// <summary>
		/// 菜单打开时显示并解锁光标，游戏中锁定光标用于第一人称视角。
		/// </summary>
		static void UpdateCursorState()
		{
			var menuOpen = UIKit.GetPanel<StartPanel>() != null || UIKit.GetPanel<PausePanel>() != null;
			if (menuOpen)
			{
				if (Cursor.lockState != CursorLockMode.None)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}
			}
			else if (Cursor.lockState != CursorLockMode.Locked)
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
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
