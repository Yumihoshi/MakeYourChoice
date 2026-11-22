using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
	/// <summary>艺术馆的收集进度：杯子计数、提交判定、提交时的 3D 飘字，以及提交后开启的门。</summary>
	public sealed class GalleryCupStation : MonoBehaviour
	{
		[SerializeField, Required, LabelText("3D 飘字")] WorldLineView worldLines;
		[SerializeField, Required, LabelText("提交后开启的门")] AutoDoor paintingDoor;
		[SerializeField, LabelText("需要的杯子数"), MinValue(1)] int requiredCups = 3;

		public int Collected { get; private set; }
		public bool Submitted { get; private set; }
		public int Remaining => Mathf.Max(0, requiredCups - Collected);

		void Awake()
		{
			if (worldLines == null || paintingDoor == null)
			{
				Debug.LogError("GalleryCupStation 缺少飘字系统或门引用", this);
				enabled = false;
			}
		}

		/// <summary>拾取一个杯子后调用，返回累计数量；拾取台词由调用方按这个数量播放。</summary>
		public int ReportPickup()
		{
			Collected = Mathf.Min(Collected + 1, requiredCups);
			return Collected;
		}

		/// <summary>在提交台交互：数量不足只提示，够了就提交并开门。</summary>
		public bool TrySubmit(Vector3 worldPosition)
		{
			if (Submitted)
			{
				ShowLine("已经交过了。", worldPosition);
				return false;
			}

			if (Collected < requiredCups)
			{
				ShowLine($"还差 {Remaining} 个杯子。", worldPosition);
				return false;
			}

			Submitted = true;
			ShowLine("三个杯子都放上去了。", worldPosition);
			paintingDoor.Open();
			return true;
		}

		/// <summary>飘一条 3D 字；已有飘字时先收掉旧的，避免被 WorldLineView 拒绝。</summary>
		public void ShowLine(string text, Vector3 worldPosition)
		{
			if (worldLines == null)
			{
				Debug.LogError("GalleryCupStation 缺少 3D 飘字引用", this);
				return;
			}

			worldLines.DismissActive();
			worldLines.ShowLine(text, 999, worldPosition);
		}
	}
}
