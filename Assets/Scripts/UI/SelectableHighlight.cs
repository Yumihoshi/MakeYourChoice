using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
	/// <summary>
	/// 选择焦点的视觉反馈：鼠标悬停、键盘或手柄导航选中时，同步切换底色、描边、文字与左侧指示条，
	/// 并轻微放大，让当前焦点在静止画面里也能一眼看出。挂载节点必须带 Selectable。
	/// </summary>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Selectable))]
	public sealed class SelectableHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler,
		IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
	{
		[Title("图形")]
		[SerializeField, LabelText("底色图形"), Tooltip("选中时换色的整块底色；留空时取本物体的 Graphic")]
		Graphic plate;

		[SerializeField, LabelText("描边"), Tooltip("随焦点改色的 Outline，可留空")]
		Outline border;

		[SerializeField, LabelText("文字"), Tooltip("选中时换色的文字")]
		TMP_Text label;

		[SerializeField, LabelText("强调图形"), Tooltip("滑块把手之类需要单独点亮的图形，可留空")]
		Graphic accent;

		[SerializeField, LabelText("焦点指示条"), Tooltip("选中时显示在按钮左侧的竖条，可留空")]
		RectTransform indicator;

		[Title("配色")]
		[SerializeField, LabelText("常态底色")]
		Color plateNormal = new Color(0.102f, 0.118f, 0.149f, 0.92f);

		[SerializeField, LabelText("选中底色")]
		Color plateFocus = new Color(0.949f, 0.757f, 0.306f, 1f);

		[SerializeField, LabelText("按下底色")]
		Color platePressed = new Color(0.839f, 0.647f, 0.216f, 1f);

		[SerializeField, LabelText("常态描边")]
		Color borderNormal = new Color(0.949f, 0.757f, 0.306f, 0.35f);

		[SerializeField, LabelText("选中描边")]
		Color borderFocus = new Color(1f, 0.953f, 0.816f, 0.95f);

		[SerializeField, LabelText("常态文字")]
		Color labelNormal = new Color(0.929f, 0.910f, 0.863f, 1f);

		[SerializeField, LabelText("选中文字")]
		Color labelFocus = new Color(0.078f, 0.086f, 0.106f, 1f);

		[SerializeField, LabelText("常态强调色")]
		Color accentNormal = new Color(0.929f, 0.910f, 0.863f, 1f);

		[SerializeField, LabelText("选中强调色")]
		Color accentFocus = new Color(0.949f, 0.757f, 0.306f, 1f);

		[Title("动效")]
		[SerializeField, LabelText("选中放大"), Min(0f)]
		float focusScale = 1.04f;

		[SerializeField, LabelText("过渡时长（秒）"), Min(0f)]
		float transitionSeconds = 0.1f;

		Selectable _selectable;
		Vector3 _baseScale;
		float _blend;
		bool _selected;
		bool _pointerInside;
		bool _pressed;
		bool _indicatorVisible;

		void Awake()
		{
			_selectable = GetComponent<Selectable>();

			if (plate == null)
			{
				plate = GetComponent<Graphic>();
			}

			if (plate == null)
			{
				Debug.LogError("SelectableHighlight 没有可换色的底色图形", this);
				enabled = false;
				return;
			}

			if (label == null)
			{
				Debug.LogError("SelectableHighlight 没有指定文字，无法显示焦点反馈", this);
				enabled = false;
				return;
			}

			if (_selectable.transition != Selectable.Transition.None)
			{
				Debug.LogError("SelectableHighlight 需要把 Selectable 的 Transition 设为 None，否则状态过渡会覆盖高亮颜色", this);
				enabled = false;
				return;
			}

			_baseScale = transform.localScale;
			Apply(0f);
		}

		void OnEnable()
		{
			var eventSystem = EventSystem.current;
			_selected = eventSystem != null && eventSystem.currentSelectedGameObject == gameObject;
			_pointerInside = false;
			_pressed = false;
			_blend = _selected ? 1f : 0f;
			Apply(_blend);
		}

		void OnDisable()
		{
			_selected = false;
			_pointerInside = false;
			_pressed = false;
			_blend = 0f;
			_indicatorVisible = false;
			transform.localScale = _baseScale;
		}

		void Update()
		{
			var target = _selected || _pointerInside ? 1f : 0f;
			if (Mathf.Approximately(_blend, target))
			{
				return;
			}

			var step = transitionSeconds <= 0f ? 1f : Time.unscaledDeltaTime / transitionSeconds;
			_blend = Mathf.MoveTowards(_blend, target, step);
			Apply(_blend);
		}

		void Apply(float blend)
		{
			plate.color = _pressed ? platePressed : Color.Lerp(plateNormal, plateFocus, blend);

			if (border != null)
			{
				border.effectColor = Color.Lerp(borderNormal, borderFocus, blend);
			}

			if (label != null)
			{
				label.color = Color.Lerp(labelNormal, labelFocus, blend);
			}

			if (accent != null)
			{
				accent.color = Color.Lerp(accentNormal, accentFocus, blend);
			}

			if (indicator != null)
			{
				var visible = blend > 0.5f;
				if (visible != _indicatorVisible)
				{
					_indicatorVisible = visible;
					indicator.gameObject.SetActive(visible);
				}
			}

			if (!Mathf.Approximately(focusScale, 1f))
			{
				transform.localScale = Vector3.LerpUnclamped(_baseScale, _baseScale * focusScale, blend);
			}
		}

		public void OnSelect(BaseEventData eventData)
		{
			_selected = true;
		}

		public void OnDeselect(BaseEventData eventData)
		{
			_selected = false;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			_pointerInside = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			_pointerInside = false;
			_pressed = false;
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			_pressed = true;
			Apply(_blend);
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			_pressed = false;
			Apply(_blend);
		}
	}
}
