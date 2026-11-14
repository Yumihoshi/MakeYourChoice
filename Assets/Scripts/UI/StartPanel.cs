using System;
using UnityEngine;
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

		protected override void OnInit(IUIData uiData = null)
		{
			_data = uiData as StartPanelData ?? new StartPanelData();
			mData = _data;

			Btn_Start.onClick.AddListener(OnStartButtonClick);
			Btn_Exit.onClick.AddListener(OnExitButtonClick);
		}

		protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
			Btn_Start.onClick.RemoveListener(OnStartButtonClick);
			Btn_Exit.onClick.RemoveListener(OnExitButtonClick);
		}

		void OnStartButtonClick()
		{
			if (_data.StartRequested == null)
			{
				Debug.LogError("StartPanel 没有收到开始游戏回调，无法进入游戏", this);
				return;
			}

			Btn_Start.interactable = false;
			Btn_Exit.interactable = false;
			_data.StartRequested.Invoke();
		}

		void OnExitButtonClick()
		{
			GameApplication.Quit();
		}
	}
}
