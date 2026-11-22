using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
	/// <summary>艺术馆的收集进度：杯子计数、提交判定、提交时的 3D 飘字，以及提交后开启的门。</summary>
	public sealed class GalleryCupStation : MonoBehaviour
	{
		[SerializeField, Required, LabelText("3D 飘字")] WorldLineView worldLines;
		[SerializeField, LabelText("提交成功台词")] DialogueTrigger submitDialogue;
		[SerializeField, Required, LabelText("提交后开启的门")] AutoDoor paintingDoor;
		[SerializeField, LabelText("需要的杯子数"), MinValue(1)] int requiredCups = 3;
		[SerializeField, LabelText("桌上的杯子模型")] GameObject[] cupPrefabs;
		[SerializeField, LabelText("桌上摆放点")] Transform[] cupSlots;

		public int Collected { get; private set; }
		public bool Submitted { get; private set; }
		public int Remaining => Mathf.Max(0, requiredCups - Collected);

		/// <summary>是否已经捡到过至少一个杯子；提交区的进入台词据此分岔。</summary>
		public bool AnyCollected => Collected > 0;

		/// <summary>是否已经找齐需要的杯子数量。</summary>
		public bool AllCollected => Collected >= requiredCups;

		/// <summary>是否已经看过白鱼的画；豆袋交互以此为前提。</summary>
		public bool PaintingSeen { get; private set; }

		void Awake()
		{
			if (worldLines == null || paintingDoor == null)
			{
				Debug.LogError("GalleryCupStation 缺少飘字系统或门引用", this);
				enabled = false;
			}
		}

		/// <summary>拾取一个杯子后调用，返回累计数量；拾取台词由调用方按杯子固定的节点播放。</summary>
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
			PlaceCups();
			if (submitDialogue != null)
			{
				submitDialogue.StartDialogue();
			}

			paintingDoor.Open();
			return true;
		}

		/// <summary>
		/// 把三个杯子摆到提交台的摆放点上。摆件不再参与拾取，因此移除交互组件与碰撞。
		/// </summary>
		void PlaceCups()
		{
			if (cupPrefabs == null || cupSlots == null || cupPrefabs.Length == 0 || cupSlots.Length < cupPrefabs.Length)
			{
				Debug.LogError("GalleryCupStation 的杯子模型与摆放点没有配对", this);
				return;
			}

			for (int i = 0; i < cupPrefabs.Length; i++)
			{
				GameObject cup = Instantiate(cupPrefabs[i], cupSlots[i].position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), cupSlots[i]);
				foreach (CupPickup pickup in cup.GetComponentsInChildren<CupPickup>(true))
				{
					Destroy(pickup);
				}

				foreach (Collider collider in cup.GetComponentsInChildren<Collider>(true))
				{
					collider.enabled = false;
				}
			}
		}

		/// <summary>记录玩家已经正视过白鱼的画。</summary>
		public void MarkPaintingSeen()
		{
			PaintingSeen = true;
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
