using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.UI
{
	/// <summary>
	/// 面板间的焦点导航：键盘、十字键或摇杆在控件组之间移动焦点时的顺序约定。
	/// </summary>
	public static class UiNavigation
	{
		/// <summary>
		/// 把一组控件按传入顺序自上而下串起来，上下键首尾循环，左右键留给滑块自身调节。
		/// </summary>
		public static void ChainVertical(params Selectable[] items)
		{
			if (items == null || items.Length == 0)
			{
				Debug.LogError("UiNavigation.ChainVertical 没有收到任何控件");
				return;
			}

			for (var i = 0; i < items.Length; i++)
			{
				if (items[i] == null)
				{
					Debug.LogError("UiNavigation.ChainVertical 的控件列表里有空引用");
					return;
				}
			}

			for (var i = 0; i < items.Length; i++)
			{
				var navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnUp = items[(i - 1 + items.Length) % items.Length],
					selectOnDown = items[(i + 1) % items.Length],
				};
				items[i].navigation = navigation;
			}
		}

		/// <summary>
		/// 面板关闭时清掉仍停留在面板内的焦点，否则 EventSystem 会记住已销毁的控件。
		/// </summary>
		public static void ClearFocus(Transform panel)
		{
			var eventSystem = EventSystem.current;
			if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
			{
				return;
			}

			if (eventSystem.currentSelectedGameObject.transform.IsChildOf(panel))
			{
				eventSystem.SetSelectedGameObject(null);
			}
		}

		/// <summary>
		/// QFramework 的 UIRoot 自带一个使用旧版输入模块的 EventSystem，和场景里配置好的事件系统重复：
		/// 指针事件会被两个模块各处理一次，旧模块还会抢走摇杆轴。只保留带 InputSystemUIInputModule 的那个，
		/// 并关掉「点击空白处取消选中」——否则鼠标点一下面板背景，手柄的十字键就没有起点可导航了。
		/// </summary>
		public static void EnsureSingleEventSystem()
		{
			var systems = Object.FindObjectsOfType<EventSystem>(true);
			if (systems.Length == 0)
			{
				Debug.LogError("场景里没有 EventSystem，UI 无法接收输入");
				return;
			}

			EventSystem keep = null;
			foreach (var system in systems)
			{
				if (system.GetComponent<InputSystemUIInputModule>() != null)
				{
					keep = system;
					break;
				}
			}

			if (keep == null)
			{
				keep = systems[0];
			}

			foreach (var system in systems)
			{
				if (system == keep)
				{
					continue;
				}

				Debug.LogWarning($"移除了重复的事件系统 {system.name}，统一使用 {keep.name}", system);
				if (Application.isPlaying)
				{
					Object.Destroy(system.gameObject);
				}
				else
				{
					Object.DestroyImmediate(system.gameObject);
				}
			}

			var module = keep.GetComponent<InputSystemUIInputModule>();
			if (module != null)
			{
				module.deselectOnBackgroundClick = false;
			}
			else
			{
				Debug.LogError($"{keep.name} 上没有 InputSystemUIInputModule，手柄导航不会生效", keep);
			}
		}

		/// <summary>
		/// 焦点被清空、或落在已经不可用的控件上时，把焦点拉回默认控件，保证手柄随时可以继续导航。
		/// 默认控件自己也不可用时，退回到同一层级里第一个可用的控件。
		/// </summary>
		public static void RecoverFocusIfLost(Selectable fallback)
		{
			var eventSystem = EventSystem.current;
			if (eventSystem == null || fallback == null)
			{
				return;
			}

			var current = eventSystem.currentSelectedGameObject;
			var currentSelectable = current != null ? current.GetComponent<Selectable>() : null;
			var lost = current == null || (currentSelectable != null && !currentSelectable.IsInteractable());
			if (!lost)
			{
				return;
			}

			var target = fallback.IsInteractable() ? fallback : FindFirstInteractable(fallback.transform.parent);
			if (target == null)
			{
				return;
			}

			eventSystem.SetSelectedGameObject(target.gameObject);
		}

		static Selectable FindFirstInteractable(Transform parent)
		{
			if (parent == null)
			{
				return null;
			}

			foreach (var selectable in parent.GetComponentsInChildren<Selectable>(true))
			{
				if (selectable.gameObject.activeInHierarchy && selectable.IsInteractable())
				{
					return selectable;
				}
			}

			return null;
		}
	}
}
