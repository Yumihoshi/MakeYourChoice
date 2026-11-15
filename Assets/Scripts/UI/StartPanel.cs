using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using QFramework;

namespace Game.UI
{
	public class StartPanelData : UIPanelData
	{
		/// <summary>玩家点击开始按钮后的处理；由场景入口注入电梯到站流程。</summary>
		public Action StartRequested { get; }

		public StartPanelData(Action startRequested = null)
		{
			StartRequested = startRequested;
		}
	}

	public partial class StartPanel : UIPanel
	{
		StartPanelData _data;
		bool _startRequested;

		protected override void OnInit(IUIData uiData = null)
		{
			_data = uiData as StartPanelData ?? new StartPanelData();
			mData = _data;

			Btn_Start.onClick.AddListener(OnStartButtonClick);
			Btn_Exit.onClick.AddListener(OnExitButtonClick);

			UiNavigation.ChainVertical(Btn_Start, Btn_Exit);
		}

		protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		protected override void OnShow()
		{
			_startRequested = false;

			var eventSystem = EventSystem.current;
			if (eventSystem == null)
			{
				Debug.LogError("StartPanel 找不到 EventSystem，手柄无法在菜单里移动焦点", this);
				return;
			}

			eventSystem.SetSelectedGameObject(Btn_Start.gameObject);
		}

		void Update()
		{
			UiNavigation.RecoverFocusIfLost(Btn_Start);
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
			Btn_Start.onClick.RemoveListener(OnStartButtonClick);
			Btn_Exit.onClick.RemoveListener(OnExitButtonClick);

			UiNavigation.ClearFocus(transform);
		}

		void OnStartButtonClick()
		{
			if (_data.StartRequested == null)
			{
				Debug.LogError("StartPanel 没有收到开始游戏回调，无法进入游戏", this);
				return;
			}

			// 电梯到站前面板要一直开着：按钮禁用会让焦点失效、十字键动不了，
			// 所以只挡住重复触发，按钮本身保持可用。
			if (_startRequested)
			{
				return;
			}

			_startRequested = true;
			_data.StartRequested.Invoke();
		}

		void OnExitButtonClick()
		{
			GameApplication.Quit();
		}
	}
}
