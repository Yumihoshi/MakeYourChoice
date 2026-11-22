using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
	/// <summary>
	/// 播放模式下把「= 玩家」挪到艺术馆的几个测试点（三只杯子、提交区门口），方便逐个验证拾取与提交流程。
	/// 只在 Play 模式可用（编辑模式下菜单是灰的）：挪动只发生在本次运行里，停止播放即复原，不会影响场景里的出生点。
	/// </summary>
	public static class GalleryTeleportTool
	{
		const string PlayerPath = "= 玩家";
		const string GalleryPath = "= 关卡/艺术馆";
		const string EntryFallPath = "= 关卡/艺术馆/流程/入场坠落";
		const string SubmitDoorPanelPath = "门/门_提交区/铰链/门板";

		/// <summary>红毯中心，杯子按「从大厅走过来」的方向挑站位。</summary>
		static readonly Vector3 CarpetCenter = new Vector3(46f, 0f, 46f);

		[MenuItem("Tools/艺术馆/传送/杯子 A", true)]
		static bool CanTeleport() => Application.isPlaying;

		[MenuItem("Tools/艺术馆/传送/杯子 A", false, 20)]
		static void TeleportToCupA()
		{
			TeleportLookingAt("道具/杯子_A", "杯子 A", null);
		}

		[MenuItem("Tools/艺术馆/传送/杯子 B", true)]
		static bool CanTeleportToCupB() => Application.isPlaying;

		[MenuItem("Tools/艺术馆/传送/杯子 B", false, 21)]
		static void TeleportToCupB()
		{
			TeleportLookingAt("道具/杯子_B", "杯子 B", null);
		}

		[MenuItem("Tools/艺术馆/传送/杯子 C", true)]
		static bool CanTeleportToCupC() => Application.isPlaying;

		[MenuItem("Tools/艺术馆/传送/杯子 C", false, 22)]
		static void TeleportToCupC()
		{
			TeleportLookingAt("道具/杯子_C", "杯子 C", null);
		}

		[MenuItem("Tools/艺术馆/传送/提交区门口", true)]
		static bool CanTeleportToSubmitDoor() => Application.isPlaying;

		[MenuItem("Tools/艺术馆/传送/提交区门口", false, 23)]
		static void TeleportToSubmitDoor()
		{
			// 提交区的门开在 x = 46 的墙上，门外是 x 更小的走廊。
			TeleportLookingAt(SubmitDoorPanelPath, "提交区门口", Vector3.left);
		}

		/// <summary>在目标四周找一个能站、且直视目标的落脚点，把玩家摆过去并转向目标。</summary>
		/// <param name="relativePath">相对「= 关卡/艺术馆」的目标路径。</param>
		/// <param name="label">日志里用的中文名。</param>
		/// <param name="preferredDirection">优先从哪个方向靠近；传 null 表示从红毯中心方向绕过去。</param>
		static void TeleportLookingAt(string relativePath, string label, Vector3? preferredDirection)
		{
			if (!Application.isPlaying)
			{
				Debug.LogWarning("传送工具只在播放模式下可用：先点 Play 再传送，否则会改到场景里的出生点");
				return;
			}

			var gallery = GameObject.Find(GalleryPath);
			if (gallery == null)
			{
				Debug.LogError($"场景里没有 {GalleryPath}，请打开 Assets/Scenes/Gallery.unity");
				return;
			}

			var target = gallery.transform.Find(relativePath);
			if (target == null)
			{
				Debug.LogError($"找不到 {GalleryPath}/{relativePath}");
				return;
			}

			var player = GameObject.Find(PlayerPath);
			if (player == null)
			{
				Debug.LogError($"场景里没有 {PlayerPath}");
				return;
			}

			var aimPoint = AimPoint(target);
			var preferred = preferredDirection ?? (aimPoint - CarpetCenter);
			preferred.y = 0f;

			foreach (var candidate in Candidates(aimPoint, preferred))
			{
				if (!TryFindGround(candidate, out var groundY))
				{
					continue;
				}

				if (!HasLineOfSight(new Vector3(candidate.x, groundY + 1.6f, candidate.z), aimPoint, target))
				{
					continue;
				}

				EndEntryFall(player);

				var look = aimPoint - candidate;
				look.y = 0f;
				player.transform.position = new Vector3(candidate.x, groundY + 0.03f, candidate.z);

				if (look.sqrMagnitude > 0.0001f)
				{
					var yaw = Quaternion.LookRotation(look.normalized, Vector3.up).eulerAngles.y;
					var firstPerson = player.GetComponent<Game.Level.FirstPersonController>();
					if (firstPerson != null)
					{
						// 第一人称控制每帧用内部 yaw 写回自身旋转，直接改 transform.rotation 会被覆盖。
						firstPerson.SnapLook(yaw, 0f);
					}
					else
					{
						player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
					}
				}

				Debug.Log($"玩家已传送到{label}附近：{player.transform.position:F2}，面向{label}");
				return;
			}

			Debug.LogError($"没找到能直视{label}的落脚点，场景几何可能已经变了");
		}

		/// <summary>
		/// 入场坠落还在跑时会把玩家一直往下拉，传送立刻被覆盖；这里直接结束坠落演出（雾恢复、交还控制），
		/// 让玩家稳稳站在传送点上。没在坠落就什么都不做。
		/// </summary>
		static void EndEntryFall(GameObject player)
		{
			var entryFall = GameObject.Find(EntryFallPath)?.GetComponent<Game.Level.GalleryEntryFall>();
			if (entryFall == null || !entryFall.enabled)
			{
				return;
			}

			entryFall.enabled = false;
			RenderSettings.fogStartDistance = 4f;
			RenderSettings.fogEndDistance = 30f;

			var firstPerson = player.GetComponent<Game.Level.FirstPersonController>();
			if (firstPerson != null)
			{
				firstPerson.enabled = true;
			}

			Debug.Log("传送前先结束了入场坠落演出（雾已恢复、控制已交还）");
		}

		/// <summary>按「离目标 1.6 / 2.2 / 2.8 m、绕 8 个方向」依次给出落脚点，优先靠向 <paramref name="preferredDirection"/>。</summary>
		static IEnumerable<Vector3> Candidates(Vector3 aimPoint, Vector3 preferredDirection)
		{
			var directions = new List<Vector3>();
			for (var i = 0; i < 8; i++)
			{
				directions.Add(Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward);
			}

			if (preferredDirection.sqrMagnitude < 0.0001f)
			{
				preferredDirection = Vector3.forward;
			}

			preferredDirection.Normalize();
			directions.Sort((a, b) => Vector3.Dot(b, preferredDirection).CompareTo(Vector3.Dot(a, preferredDirection)));

			foreach (var distance in new[] { 1.6f, 2.2f, 2.8f })
			{
				foreach (var direction in directions)
				{
					yield return aimPoint + direction * distance;
				}
			}
		}

		/// <summary>从候选点上方往下打一条射线，取最上面的一层水平地面；返回 false 表示脚下没有可站的地板。</summary>
		static bool TryFindGround(Vector3 point, out float groundY)
		{
			groundY = 0f;
			var origin = new Vector3(point.x, point.y + 1.5f, point.z);
			var nearest = float.MaxValue;
			var found = false;

			foreach (var hit in Physics.RaycastAll(origin, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
			{
				if (hit.normal.y < 0.9f || hit.distance >= nearest)
				{
					continue;
				}

				nearest = hit.distance;
				groundY = hit.point.y;
				found = true;
			}

			return found;
		}

		/// <summary>视线射线打到的第一个碰撞体是不是目标本身（准心要求命中自身或子物体）。</summary>
		static bool HasLineOfSight(Vector3 eye, Vector3 aimPoint, Transform target)
		{
			var direction = aimPoint - eye;
			var distance = direction.magnitude;
			if (distance < 0.01f)
			{
				return false;
			}

			return Physics.Raycast(eye, direction / distance, out var hit, distance + 0.1f, ~0, QueryTriggerInteraction.Ignore)
				&& hit.collider.transform.IsChildOf(target);
		}

		/// <summary>目标的可视中心：优先用碰撞体包围盒，没有碰撞体再退到渲染器。</summary>
		static Vector3 AimPoint(Transform target)
		{
			var collider = target.GetComponentInChildren<Collider>();
			if (collider != null)
			{
				return collider.bounds.center;
			}

			var renderer = target.GetComponentInChildren<Renderer>();
			if (renderer != null)
			{
				return renderer.bounds.center;
			}

			return target.position + Vector3.up * 0.5f;
		}
	}
}
