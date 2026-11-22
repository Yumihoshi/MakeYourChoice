using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
	/// <summary>提交区的台面：把杯子交上去，够了就开「白鱼的画」房间的门。</summary>
	public sealed class SubmitDeskInteract : InteractableBase
	{
		[SerializeField, Required, LabelText("杯子站")] GalleryCupStation station;
		[SerializeField, Required, LabelText("飘字锚点")] Transform textAnchor;

		protected override bool CanInteract() => station != null && textAnchor != null && !station.Submitted;

		protected override void OnInteract()
		{
			if (station == null || textAnchor == null)
			{
				Debug.LogError("SubmitDeskInteract 缺少杯子站或飘字锚点引用", this);
				return;
			}

			station.TrySubmit(textAnchor.position);
		}
	}
}
