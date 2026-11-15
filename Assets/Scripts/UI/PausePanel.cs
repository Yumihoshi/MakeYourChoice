using UnityEngine;
using UnityEngine.EventSystems;
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
	/// 手柄十字键 / 摇杆在控件间移动焦点，B 键与 Esc 一样关闭面板。
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

			UiNavigation.ChainVertical(Slider_Bgm, Slider_Sfx, Btn_Continue, Btn_Exit);
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

			FocusFirstControl();
		}

		protected override void OnHide()
		{
			Time.timeScale = 1f;
		}

		void Update()
		{
			var input = PlayerInputRouter.Instance;
			if (input != null && input.Back.WasPressedThisFrame())
			{
				CloseSelf();
				return;
			}

			UiNavigation.RecoverFocusIfLost(Btn_Continue);
		}

		protected override void OnClose()
		{
			Time.timeScale = 1f;

			Slider_Bgm.onValueChanged.RemoveListener(OnBgmSliderChanged);
			Slider_Sfx.onValueChanged.RemoveListener(OnSfxSliderChanged);

			Btn_Continue.onClick.RemoveListener(OnContinueButtonClick);
			Btn_Exit.onClick.RemoveListener(OnExitButtonClick);

			UiNavigation.ClearFocus(transform);

			// AudioKit 的音量属性只会写入 PlayerPrefs 内存，这里落盘保证重启后仍然生效。
			PlayerPrefs.Save();
		}

		/// <summary>
		/// 打开面板时把焦点放到「继续游戏」，让手柄不用先推摇杆就能看到当前选中项。
		/// </summary>
		void FocusFirstControl()
		{
			var eventSystem = EventSystem.current;
			if (eventSystem == null)
			{
				Debug.LogError("PausePanel 找不到 EventSystem，手柄无法在面板里移动焦点", this);
				return;
			}

			eventSystem.SetSelectedGameObject(Btn_Continue.gameObject);
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
