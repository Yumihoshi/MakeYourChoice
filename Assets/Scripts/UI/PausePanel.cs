using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace Game.UI
{
	public class PausePanelData : UIPanelData
	{
	}

	/// <summary>
	/// 暂停面板：暂停游戏，调节背景音乐与音效音量，并提供继续游戏与退出游戏操作。
	/// </summary>
	public partial class PausePanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as PausePanelData ?? new PausePanelData();

			Slider_Bgm.onValueChanged.AddListener(OnBgmSliderChanged);
			Slider_Sfx.onValueChanged.AddListener(OnSfxSliderChanged);

			Btn_Continue.onClick.AddListener(OnContinueButtonClick);
			Btn_Exit.onClick.AddListener(OnExitButtonClick);
		}

		protected override void OnOpen(IUIData uiData = null)
		{
		}

		protected override void OnShow()
		{
			Time.timeScale = 0f;

			var bgmVolume = AudioKit.Settings.MusicVolume.Value;
			Slider_Bgm.SetValueWithoutNotify(bgmVolume);
			SetVolumeLabel(TMP_BgmVal, bgmVolume);

			var sfxVolume = AudioKit.Settings.SoundVolume.Value;
			Slider_Sfx.SetValueWithoutNotify(sfxVolume);
			SetVolumeLabel(TMP_SfxVal, sfxVolume);
		}

		protected override void OnHide()
		{
			Time.timeScale = 1f;
		}

		protected override void OnClose()
		{
			Time.timeScale = 1f;

			Slider_Bgm.onValueChanged.RemoveListener(OnBgmSliderChanged);
			Slider_Sfx.onValueChanged.RemoveListener(OnSfxSliderChanged);

			Btn_Continue.onClick.RemoveListener(OnContinueButtonClick);
			Btn_Exit.onClick.RemoveListener(OnExitButtonClick);

			// AudioKit 的音量属性只会写入 PlayerPrefs 内存，这里落盘保证重启后仍然生效。
			PlayerPrefs.Save();
		}

		void OnBgmSliderChanged(float value)
		{
			AudioKit.Settings.MusicVolume.Value = value;
			SetVolumeLabel(TMP_BgmVal, value);
		}

		void OnSfxSliderChanged(float value)
		{
			AudioKit.Settings.SoundVolume.Value = value;
			SetVolumeLabel(TMP_SfxVal, value);
		}

		/// <summary>
		/// 把 0~1 的音量值显示为百分比文本。
		/// </summary>
		static void SetVolumeLabel(TMP_Text label, float value)
		{
			label.text = Mathf.RoundToInt(value * 100f) + "%";
		}

		void OnContinueButtonClick()
		{
			CloseSelf();
		}

		void OnExitButtonClick()
		{
			PlayerPrefs.Save();
			GameApplication.Quit();
		}
	}
}
