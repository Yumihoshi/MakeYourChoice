using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
	/// <summary>捡起一个杯子：上报进度、飘字，然后隐藏自己。</summary>
	public sealed class CupPickup : InteractableBase
	{
		[SerializeField, Required, LabelText("杯子站")] GalleryCupStation station;

		protected override bool CanInteract() => station != null && !station.Submitted;

		protected override void OnInteract()
		{
			if (station == null)
			{
				Debug.LogError("CupPickup 缺少杯子站引用", this);
				return;
			}

			station.ReportPickup(transform.position + Vector3.up * 0.6f);
			gameObject.SetActive(false);
		}
	}
}
