using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using QFramework;

namespace Game.UI
{
	public class StartPanelData : UIPanelData
	{
		/// <summary>玩家点击开始按钮后的处理；由场景入口注入，返回是否接受了本次开始请求（接受时入口会立即关闭本面板）。</summary>
		public Func<bool> StartRequested { get; }

		public StartPanelData(Func<bool> startRequested = null)
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
			SetButtonsInteractable(true);

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

			if (_startRequested)
			{
				return;
			}

			// 先置灰防止连点：回调成功后场景入口会立即关闭本面板，回调返回 false 时才需要恢复。
			_startRequested = true;
			SetButtonsInteractable(false);

			if (_data.StartRequested.Invoke())
			{
				return;
			}

			_startRequested = false;
			SetButtonsInteractable(true);
		}

		/// <summary>
		/// 设置开始与退出按钮是否可用：不可用时按钮变灰，既不响应点击也不参与手柄导航。
		/// </summary>
		void SetButtonsInteractable(bool interactable)
		{
			SetInteractable(Btn_Start, interactable);
			SetInteractable(Btn_Exit, interactable);

			if (!interactable)
			{
				UiNavigation.ClearFocus(transform);
			}
		}

		static void SetInteractable(Selectable button, bool interactable)
		{
			button.interactable = interactable;

			// 按钮的过渡方式为 None，选中与禁用配色都由 SelectableHighlight 负责，这里催它立刻重绘。
			var highlight = button.GetComponent<SelectableHighlight>();
			if (highlight != null)
			{
				highlight.Refresh();
			}
		}

		void OnExitButtonClick()
		{
			GameApplication.Quit();
		}
	}
}
