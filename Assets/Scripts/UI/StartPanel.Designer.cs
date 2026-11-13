using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace Game.UI
{
	// Generate Id:9153fc24-82a5-4e1e-b890-bc7649ecc449
	public partial class StartPanel
	{
		public const string Name = "StartPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Btn_Start;
		[SerializeField]
		public UnityEngine.UI.Button Btn_Exit;
		
		private StartPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Btn_Start = null;
			Btn_Exit = null;
			
			mData = null;
		}
		
		public StartPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		StartPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new StartPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
