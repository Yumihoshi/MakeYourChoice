using QFramework;
using UnityEngine;

namespace Game
{
	/// <summary>
	/// 游戏音效入口：按名字从 Resources/Audio/Sfx 加载，交给 AudioKit 播放。
	/// 播放音量跟随暂停面板里的音效音量（AudioKit.Settings.SoundVolume）。
	/// 依赖 ResKit 已初始化：编辑器由 ResKit 自动初始化，真机由 <see cref="GameBootstrap.Awake"/> 初始化。
	/// </summary>
	public static class GameSfx
	{
		/// <summary>门外循环风声。</summary>
		public const string Wind = "wind";

		/// <summary>电梯到站。</summary>
		public const string ElevatorArrive = "elevator_to";

		/// <summary>电梯门打开。</summary>
		public const string ElevatorDoorOpen = "elevator-door-opening";

		const string Folder = "resources://Audio/Sfx/";

		/// <summary>播放一次音效。</summary>
		public static void Play(string clipName, float volume = 1f)
		{
			AudioKit.PlaySound(Folder + clipName, false, null, volume);
		}

		/// <summary>读取音效资源时长（秒），用于让演出等待音效播完；资源缺失时报错并返回 0。</summary>
		public static float ClipLength(string clipName)
		{
			var loader = ResLoader.Allocate();
			var clip = loader.LoadSync<AudioClip>(Folder + clipName);
			var duration = clip != null ? clip.length : 0f;
			loader.Recycle2Cache();

			if (clip == null)
			{
				Debug.LogError($"找不到音效资源：{Folder + clipName}");
			}

			return duration;
		}

		/// <summary>循环播放音效；返回的播放器用来停止播放。</summary>
		public static AudioPlayer PlayLoop(string clipName, float volume = 1f)
		{
			return AudioKit.PlaySound(Folder + clipName, true, null, volume);
		}
	}
}
