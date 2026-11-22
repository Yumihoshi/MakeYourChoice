using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
	/// <summary>
	/// 迷宫超时：提交杯子前在艺术馆里累计停留到时限后，播一句旁白并让迷宫墙与天花板碎裂。
	/// 对话播放与菜单期间暂停计时。
	/// </summary>
	public sealed class GalleryMazeTimeout : MonoBehaviour
	{
		[SerializeField, Required, LabelText("杯子站")] GalleryCupStation station;
		[SerializeField, Required, LabelText("超时旁白")] DialogueTrigger dialogue;
		[SerializeField, Required, LabelText("碎裂效果")] GalleryShatter shatter;
		[SerializeField, LabelText("超时秒数")] float limitSeconds = 300f;

		float _elapsed;
		bool _fired;

		void Awake()
		{
			if (station == null || dialogue == null || shatter == null)
			{
				Debug.LogError("GalleryMazeTimeout 缺少杯子站、超时旁白或碎裂效果引用", this);
				enabled = false;
			}
		}

		void Update()
		{
			if (_fired || station == null || dialogue == null || shatter == null)
			{
				return;
			}

			if (station.Submitted || station.AllCollected || !GameCursorState.IsPlaying || dialogue.IsRunning)
			{
				return;
			}

			_elapsed += Time.deltaTime;
			if (_elapsed < limitSeconds)
			{
				return;
			}

			_fired = true;
			shatter.Shatter();
			dialogue.StartDialogue();
		}
	}
}
