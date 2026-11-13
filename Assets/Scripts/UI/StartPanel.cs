using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace Game.UI
{
	public class StartPanelData : UIPanelData
	{
	}
	public partial class StartPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as StartPanelData ?? new StartPanelData();

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
			CloseSelf();
		}

		void OnExitButtonClick()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
	}
}
