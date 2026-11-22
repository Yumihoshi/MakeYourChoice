using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
	/// <summary>
	/// 提交三个杯子之后才打得开的门：没提交时飘一句提示并拒绝开门，
	/// 提交后行为与普通 <see cref="DoorInteract"/> 一致。
	/// </summary>
	public sealed class GalleryDoorInteract : DoorInteract
	{
		[SerializeField, Required, LabelText("杯子站")] GalleryCupStation station;
		[SerializeField, Required, LabelText("未解锁提示锚点")] Transform hintAnchor;

		protected override bool CanInteract()
		{
			if (station == null || hintAnchor == null)
			{
				return false;
			}

			return base.CanInteract();
		}

		protected override bool TryOpenDoor()
		{
			if (station == null || hintAnchor == null)
			{
				Debug.LogError("GalleryDoorInteract 缺少杯子站或提示锚点引用", this);
				return false;
			}

			if (!station.Submitted)
			{
				station.ShowLine("先把杯子交到提交区。", hintAnchor.position);
				return false;
			}

			return base.TryOpenDoor();
		}
	}
}
