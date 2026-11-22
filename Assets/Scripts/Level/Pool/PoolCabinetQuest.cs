using System.Collections.Generic;
using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
	/// <summary>
	/// 更衣室储物柜的开柜进度：统计拿到钥匙后打开过的不同柜门数量，第三次时播一段旁白。
	/// </summary>
	public sealed class PoolCabinetQuest : MonoBehaviour
	{
		[SerializeField, Required, LabelText("流程状态")] PoolFlowState state;
		[SerializeField, Required, LabelText("第三次开柜剧情")] DialogueTrigger thirdDialogue;

		readonly HashSet<int> _opened = new HashSet<int>();
		bool _fired;

		/// <summary>记录一扇柜门被打开；同一扇门重复开关只算一次，拿钥匙前的开柜不计入。</summary>
		public void ReportOpened(CabinetDoorInteract door)
		{
			if (state == null || thirdDialogue == null || door == null)
			{
				return;
			}

			if (!_opened.Add(door.GetInstanceID()))
			{
				return;
			}

			if (!state.HasKey)
			{
				return;
			}

			var count = state.RegisterCabinetOpened();
			if (!_fired && count >= 3)
			{
				_fired = true;
				thirdDialogue.StartDialogue();
			}
		}
	}
}
