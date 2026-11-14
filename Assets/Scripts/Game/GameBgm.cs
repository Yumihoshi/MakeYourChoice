using System.Collections;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game
{
	/// <summary>
	/// 背景音乐：循环播放同一首曲子，并在每一轮的首尾做渐入渐出，掩盖循环接缝。
	/// 音量跟随暂停面板里的背景音乐音量（AudioKit.Settings.MusicVolume）。
	/// </summary>
	[RequireComponent(typeof(AudioSource))]
	public class GameBgm : MonoBehaviour
	{
		[LabelText("背景音乐"), Required, SerializeField] AudioClip clip;
		[LabelText("淡入时长（秒）"), MinValue(0), SerializeField] float fadeIn = 2f;
		[LabelText("淡出时长（秒）"), MinValue(0), SerializeField] float fadeOut = 2f;
		[LabelText("基础音量"), Range(0f, 1f), SerializeField] float volume = 1f;

		/// <summary>渐入渐出系数：0 为静音、1 为满音量；实际输出还要乘上全局音乐音量。初值为 0，让第一轮从淡入开始。</summary>
		float _fade;

		AudioSource _source;

		void Awake()
		{
			_source = GetComponent<AudioSource>();
		}

		void Start()
		{
			if (clip == null)
			{
				Debug.LogError("GameBgm 没有指定背景音乐，背景音乐不会播放", this);
				enabled = false;
				return;
			}

			_source.playOnAwake = false;
			_source.loop = false; // 接缝由协程淡出再淡入处理，不能用 AudioSource 自带的循环
			_source.spatialBlend = 0f;

			StartCoroutine(PlayLoop());
		}

		void Update()
		{
			var musicOn = AudioKit.Settings.IsMusicOn.Value ? 1f : 0f;
			_source.volume = _fade * volume * AudioKit.Settings.MusicVolume.Value * musicOn;
		}

		/// <summary>
		/// 播放一轮：开头淡入，接近结尾淡出，然后立刻重播进入下一轮，接缝处音量降到 0 再升起。
		/// </summary>
		IEnumerator PlayLoop()
		{
			var length = clip.length;
			// 淡入淡出各不超过曲子长度的四成，避免短曲子时两段互相覆盖。
			var fadeInSeconds = Mathf.Min(fadeIn, length * 0.4f);
			var fadeOutSeconds = Mathf.Min(fadeOut, length * 0.4f);

			while (true)
			{
				_source.clip = clip;
				_source.Play();

				yield return FadeTo(1f, fadeInSeconds);

				while (_source.isPlaying && _source.time < length - fadeOutSeconds)
				{
					yield return null;
				}

				yield return FadeTo(0f, fadeOutSeconds);

				_source.Stop();
			}
		}

		/// <summary>把渐入渐出系数从当前值线性过渡到目标值；用非缩放时间，暂停菜单打开时也能正常过渡。</summary>
		IEnumerator FadeTo(float target, float duration)
		{
			if (duration <= 0f)
			{
				_fade = target;
				yield break;
			}

			var start = _fade;
			var elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				_fade = Mathf.Lerp(start, target, elapsed / duration);
				yield return null;
			}

			_fade = target;
		}
	}
}
