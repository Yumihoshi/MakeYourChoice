using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
	/// <summary>捡起一个杯子：上报进度、按当前进度播 3D 台词，然后隐藏自己。</summary>
	public sealed class CupPickup : InteractableBase
	{
		[SerializeField, Required, LabelText("杯子站")] GalleryCupStation station;
		[SerializeField, Required, LabelText("拾取台词")] DialogueTrigger pickupDialogue;

		protected override void Awake()
		{
			base.Awake();
			if (!enabled) return;

			if (station == null || pickupDialogue == null)
			{
				Debug.LogError("CupPickup 缺少杯子站或拾取台词引用", this);
				enabled = false;
			}
		}

		protected override bool CanInteract() => station != null && !station.Submitted;

		protected override void OnInteract()
		{
			var collected = station.ReportPickup();
			pickupDialogue.StartDialogue($"Gallery_杯子_{collected}");
			gameObject.SetActive(false);
		}
	}
}
