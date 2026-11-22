using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
	/// <summary>
	/// 提交区入口的旁白：玩家第一次走进提交区时，按是否已经捡到过杯子播不同的一句。
	/// </summary>
	public sealed class GallerySubmitRoomTrigger : MonoBehaviour
	{
		[SerializeField, Required, LabelText("杯子站")] GalleryCupStation station;
		[SerializeField, Required, LabelText("空手进入台词")] DialogueTrigger emptyDialogue;
		[SerializeField, Required, LabelText("带杯子进入台词")] DialogueTrigger withCupsDialogue;
		[SerializeField, LabelText("玩家")] Transform player;

		bool _firedEmpty;
		bool _firedWithCups;

		void Awake()
		{
			if (station == null || emptyDialogue == null || withCupsDialogue == null)
			{
				Debug.LogError("GallerySubmitRoomTrigger 缺少杯子站或入口台词引用", this);
				enabled = false;
			}
		}

		void OnTriggerEnter(Collider other)
		{
			if (player != null)
			{
				if (other.transform != player && !other.transform.IsChildOf(player))
				{
					return;
				}
			}
			else if (!other.CompareTag("Player"))
			{
				return;
			}

			if (!GameCursorState.IsPlaying)
			{
				return;
			}

			if (station.AnyCollected)
			{
				if (_firedWithCups || withCupsDialogue.IsRunning)
				{
					return;
				}

				_firedWithCups = true;
				withCupsDialogue.StartDialogue();
				return;
			}

			if (_firedEmpty || emptyDialogue.IsRunning)
			{
				return;
			}

			_firedEmpty = true;
			emptyDialogue.StartDialogue();
		}
	}
}
