using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace Game.UI
{
	// Generate Id:515a0367-ec6a-4a99-add3-4aae312d990a
	public partial class PausePanel
	{
		public const string Name = "PausePanel";
		
		[SerializeField]
		public UnityEngine.UI.Slider Slider_Bgm;
		[SerializeField]
		public TMPro.TextMeshProUGUI TMP_BgmVal;
		[SerializeField]
		public UnityEngine.UI.Slider Slider_Sfx;
		[SerializeField]
		public TMPro.TextMeshProUGUI TMP_SfxVal;
		[SerializeField]
		public UnityEngine.UI.Button Btn_Continue;
		[SerializeField]
		public UnityEngine.UI.Button Btn_Exit;
		
		private PausePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Slider_Bgm = null;
			TMP_BgmVal = null;
			Slider_Sfx = null;
			TMP_SfxVal = null;
			Btn_Continue = null;
			Btn_Exit = null;
			
			mData = null;
		}
		
		public PausePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		PausePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new PausePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
