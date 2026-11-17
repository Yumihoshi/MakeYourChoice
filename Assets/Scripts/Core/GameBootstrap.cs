using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
	[RequireComponent(typeof(GameCursorState))]
	public class GameBootstrap : MonoBehaviour
	{
		[LabelText("楼层显示"), Required, SerializeField] ElevatorFloorDisplay floorDisplay;
		[LabelText("电梯运行晃动"), Required, SerializeField] ElevatorRideMotion rideMotion;
		[LabelText("到站楼层"), SerializeField] int arrivalFloor = 1;
		[LabelText("到站音效提前量"), SerializeField] float arrivalSfxLead = 1.5f;

		bool _starting;

		GameCursorState _cursor;

		/// <summary>
		/// 关闭 Domain Reload 后 UIKit 的静态状态会跨 Play 残留，进入 Play 前重置一次。
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void ResetUIKitState()
		{
			UIKit.Config = new UIKitConfig();
			UIKit.Config.PanelLoaderPool = new ResKitPanelLoaderPool();
			UIKit.Table.Clear();
			GameProgress.Reset();
		}

		void Awake()
		{
			_cursor = GetComponent<GameCursorState>();

			// 面板与音效都通过 ResKit 按名字加载。编辑器下 ResKit 会在场景加载后按模拟模式自动初始化，
			// 真机没有这层自动初始化，所以只在真机上显式初始化。
#if !UNITY_EDITOR
			ResKit.Init();
#endif
		}

		void Start()
		{
			if (floorDisplay == null || rideMotion == null)
			{
				Debug.LogError("GameBootstrap 缺少楼层显示或电梯晃动引用，开始菜单将跳过电梯到站演出", this);
				UIKit.OpenPanel<StartPanel>(new StartPanelData(() =>
				{
					UIKit.ClosePanel<StartPanel>();
					return true;
				}));
			}
			else
			{
				floorDisplay.BeginIdleDescent();
				rideMotion.Ride();
				UIKit.OpenPanel<StartPanel>(new StartPanelData(OnStartRequested));
			}

			// UIRoot 要等第一次打开面板时才创建，它自带的旧版事件系统只能在这之后清理。
			UiNavigation.EnsureSingleEventSystem();
		}

		void Update()
		{
			var input = PlayerInputRouter.Instance;
			if (input != null && input.Menu.WasPressedThisFrame())
			{
				TogglePausePanel();
			}

			_cursor.MenuOpen = UIKit.GetPanel<StartPanel>() != null || UIKit.GetPanel<PausePanel>() != null;
		}

		/// <summary>
		/// 玩家点击开始：立刻收起开始菜单，再让电梯从当前楼层减速停到目标楼层。
		/// </summary>
		/// <returns>是否已接受本次开始请求；返回 false 时调用方会恢复开始菜单的按钮。</returns>
		bool OnStartRequested()
		{
			if (_starting)
			{
				return false;
			}

			_starting = true;
			if (!floorDisplay.ArriveAt(arrivalFloor, OnArrived))
			{
				_starting = false;
				Debug.LogError("电梯已经在到站过程中，忽略了重复的开始请求", this);
				return false;
			}

			_cursor.Settling = true;
			UIKit.ClosePanel<StartPanel>();
			PlayArrivalSfx(floorDisplay.ArrivalPresentationSeconds - arrivalSfxLead, destroyCancellationToken).Forget();
			return true;
		}

		/// <summary>到站音效提前于电梯停稳播放，让声音的渐入与到站动作对齐。</summary>
		async UniTaskVoid PlayArrivalSfx(float delaySeconds, CancellationToken cancellationToken)
		{
			try
			{
				if (delaySeconds > 0f)
				{
					await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), DelayType.DeltaTime, cancellationToken: cancellationToken);
				}
			}
			catch (OperationCanceledException)
			{
				return;
			}

			GameSfx.Play(GameSfx.ElevatorArrive);
		}

		/// <summary>电梯停稳：镜头下沉回弹，等音效播完再结束演出。</summary>
		void OnArrived()
		{
			_starting = false;
			rideMotion.Arrive();
			ReleaseControlAfterSfx(destroyCancellationToken).Forget();
		}

		/// <summary>
		/// 到站音效播完后才锁定光标：开场剧情点只在光标锁定时开播，这样台词不会盖住楼号动画与到站声音。
		/// </summary>
		async UniTaskVoid ReleaseControlAfterSfx(CancellationToken cancellationToken)
		{
			// 音效在到站前 arrivalSfxLead 秒开始，所以到站后还要等它剩下的部分。
			var sfxStart = Mathf.Max(0f, floorDisplay.ArrivalPresentationSeconds - arrivalSfxLead);
			var remaining = sfxStart + GameSfx.ClipLength(GameSfx.ElevatorArrive) - floorDisplay.ArrivalPresentationSeconds;
			if (remaining > 0f)
			{
				try
				{
					await UniTask.Delay(TimeSpan.FromSeconds(remaining), DelayType.DeltaTime, cancellationToken: cancellationToken);
				}
				catch (OperationCanceledException)
				{
					return;
				}
			}

			_cursor.Settling = false;
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
