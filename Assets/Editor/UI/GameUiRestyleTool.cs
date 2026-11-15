using System;
using Game.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorTools
{
	/// <summary>
	/// 一次性重建「开始面板」与「暂停面板」的视觉结构：配色、排版、焦点指示条与选中反馈组件。
	/// 通过 Unity 的预制体序列化流程写回，不手写 YAML；可重复执行，已有节点会被复用并按规格校正。
	/// </summary>
	public static class GameUiRestyleTool
	{
		const string StartPanelPath = "Assets/Prefabs/UI/StartPanel.prefab";
		const string PausePanelPath = "Assets/Prefabs/UI/PausePanel.prefab";
		const string FontPath = "Assets/Fonts/SourceHanSansCN-Medium SDF.asset";

		// 深色基调 + 琥珀强调色，与游戏内警示文字的暖色调保持一致。
		static readonly Color Ink = new Color32(0x12, 0x14, 0x1A, 0xFF);
		static readonly Color Card = new Color(0.09f, 0.102f, 0.125f, 0.96f);
		static readonly Color Amber = new Color(0.949f, 0.757f, 0.306f, 1f);
		static readonly Color OffWhite = new Color(0.929f, 0.910f, 0.863f, 1f);
		static readonly Color Muted = new Color(0.549f, 0.541f, 0.518f, 1f);
		static readonly Color Divider = new Color(0.2f, 0.216f, 0.247f, 1f);
		static readonly Color PlateNormal = new Color(0.102f, 0.118f, 0.149f, 0.92f);
		static readonly Color PlateFocus = Amber;
		static readonly Color PlatePressed = new Color(0.839f, 0.647f, 0.216f, 1f);
		static readonly Color LabelNormal = new Color(0.929f, 0.910f, 0.863f, 1f);
		static readonly Color LabelFocus = new Color(0.078f, 0.086f, 0.106f, 1f);
		static readonly Color BorderNormal = new Color(0.949f, 0.757f, 0.306f, 0.35f);
		static readonly Color BorderFocus = new Color(1f, 0.953f, 0.816f, 0.95f);

		static TMP_FontAsset Font;

		[MenuItem("Tools/Game/重排开始与暂停面板 UI")]
		public static void RestyleAll()
		{
			Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
			if (Font == null)
			{
				Debug.LogError($"找不到 TMP 字体资产：{FontPath}，UI 重排中止");
				return;
			}

			EditPrefab(StartPanelPath, RestyleStartPanel);
			EditPrefab(PausePanelPath, RestylePausePanel);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log("开始面板与暂停面板 UI 已按规格重排");
		}

		static void EditPrefab(string path, Action<GameObject> edit)
		{
			var root = PrefabUtility.LoadPrefabContents(path);
			if (root == null)
			{
				Debug.LogError($"无法载入预制体：{path}");
				return;
			}

			try
			{
				edit(root);
				PrefabUtility.SaveAsPrefabAsset(root, path);
			}
			catch (Exception exception)
			{
				Debug.LogError($"重排 {path} 失败：{exception.Message}");
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		#region 开始面板

		static void RestyleStartPanel(GameObject root)
		{
			var panelTransform = root.transform;

			var background = Require<Image>(panelTransform, "Img_bg");
			background.sprite = null;
			background.type = Image.Type.Simple;
			background.color = new Color(Ink.r, Ink.g, Ink.b, 0.62f);
			background.raycastTarget = true;
			Stretch(Rect(background));

			var title = Require<TMP_Text>(panelTransform, "TMP_Title");
			StyleText(title, 104f, Amber, TextAlignmentOptions.Center,
				new Vector2(0f, 356f), new Vector2(1400f, 120f), 10f);

			var teamName = Require<TMP_Text>(panelTransform, "TMP_TeamName");
			StyleText(teamName, 40f, OffWhite, TextAlignmentOptions.Center,
				new Vector2(0f, 248f), new Vector2(1200f, 56f), 12f);

			EnsureImage(panelTransform, "Deco_Line", new Color(Amber.r, Amber.g, Amber.b, 0.5f),
				new Vector2(0f, 190f), new Vector2(620f, 2f));

			var startButton = Require<Button>(panelTransform, "Btn_Start");
			StyleButton(startButton, 34f, new Vector2(0f, -8f), new Vector2(460f, 88f));

			var exitButton = Require<Button>(panelTransform, "Btn_Exit");
			StyleButton(exitButton, 34f, new Vector2(0f, -128f), new Vector2(460f, 88f));

			EnsureText(panelTransform, "TMP_Hint", "鼠标点击　·　十字键 选择　·　A 确认",
				24f, Muted, TextAlignmentOptions.Center, new Vector2(0f, -262f), new Vector2(1200f, 40f), 4f);
		}

		#endregion

		#region 暂停面板

		static void RestylePausePanel(GameObject root)
		{
			var panel = Require<Image>(root.transform, "Panel");
			panel.sprite = null;
			panel.type = Image.Type.Simple;
			panel.color = new Color(Ink.r, Ink.g, Ink.b, 0.72f);
			panel.raycastTarget = true;
			Stretch(Rect(panel));

			var card = EnsureImage(panel.transform, "Card", Card, Vector2.zero, new Vector2(820f, 760f));
			card.transform.SetAsFirstSibling();
			EnsureOutline(card);

			var topBar = EnsureImage(card.transform, "Card_TopBar", Amber, Vector2.zero, new Vector2(0f, 4f));
			var topBarRect = Rect(topBar);
			topBarRect.anchorMin = new Vector2(0f, 1f);
			topBarRect.anchorMax = new Vector2(1f, 1f);
			topBarRect.pivot = new Vector2(0.5f, 1f);
			topBarRect.anchoredPosition = Vector2.zero;
			topBarRect.sizeDelta = new Vector2(0f, 4f);

			EnsureText(card.transform, "TMP_Kicker", "PAUSED",
				22f, Amber, TextAlignmentOptions.Center, new Vector2(0f, 312f), new Vector2(600f, 30f), 14f);

			var title = Require<TMP_Text>(panel.transform, "TMP_Title");
			title.transform.SetParent(card.transform, false);
			StyleText(title, 62f, OffWhite, TextAlignmentOptions.Center,
				new Vector2(0f, 250f), new Vector2(700f, 90f), 12f);

			EnsureImage(card.transform, "Deco_Line_Top", Divider, new Vector2(0f, 196f), new Vector2(660f, 1f));
			EnsureImage(card.transform, "Deco_Line_Bottom", Divider, new Vector2(0f, -4f), new Vector2(660f, 1f));

			var bgmSlider = Require<Slider>(panel.transform, "Slider_Bgm");
			bgmSlider.transform.SetParent(card.transform, false);
			var bgmLabel = Require<TMP_Text>(panel.transform, "TMP_Bgm");
			bgmLabel.transform.SetParent(card.transform, false);
			StyleText(bgmLabel, 32f, LabelNormal, TextAlignmentOptions.MidlineLeft,
				new Vector2(-236f, 138f), new Vector2(260f, 50f), 2f);
			StyleSlider(bgmSlider, bgmLabel, new Vector2(86f, 138f));

			var bgmValue = Require<TMP_Text>(panel.transform, "TMP_BgmVal");
			bgmValue.transform.SetParent(card.transform, false);
			StyleText(bgmValue, 30f, Amber, TextAlignmentOptions.MidlineRight,
				new Vector2(318f, 138f), new Vector2(100f, 50f), 0f);

			var sfxSlider = Require<Slider>(panel.transform, "Slider_Sfx");
			sfxSlider.transform.SetParent(card.transform, false);
			var sfxLabel = Require<TMP_Text>(panel.transform, "TMP_Sfx");
			sfxLabel.transform.SetParent(card.transform, false);
			StyleText(sfxLabel, 32f, LabelNormal, TextAlignmentOptions.MidlineLeft,
				new Vector2(-236f, 54f), new Vector2(260f, 50f), 2f);
			StyleSlider(sfxSlider, sfxLabel, new Vector2(86f, 54f));

			var sfxValue = Require<TMP_Text>(panel.transform, "TMP_SfxVal");
			sfxValue.transform.SetParent(card.transform, false);
			StyleText(sfxValue, 30f, Amber, TextAlignmentOptions.MidlineRight,
				new Vector2(318f, 54f), new Vector2(100f, 50f), 0f);

			var continueButton = Require<Button>(panel.transform, "Btn_Continue");
			continueButton.transform.SetParent(card.transform, false);
			StyleButton(continueButton, 32f, new Vector2(0f, -80f), new Vector2(420f, 84f));

			var exitButton = Require<Button>(panel.transform, "Btn_Exit");
			exitButton.transform.SetParent(card.transform, false);
			StyleButton(exitButton, 32f, new Vector2(0f, -180f), new Vector2(420f, 84f));

			EnsureText(card.transform, "TMP_Hint", "十字键 选择　·　左右键 调节音量　·　A 确认　·　B / Esc 返回",
				20f, Muted, TextAlignmentOptions.Center, new Vector2(0f, -272f), new Vector2(780f, 44f), 2f);
		}

		#endregion

		#region 控件样式

		static void StyleButton(Button button, float fontSize, Vector2 position, Vector2 size)
		{
			var buttonRect = Rect(button);
			SetRect(buttonRect, position, size);

			var plate = button.GetComponent<Image>();
			if (plate == null)
			{
				throw new InvalidOperationException($"{button.name} 上没有 Image，无法做底色高亮");
			}

			plate.sprite = null;
			plate.type = Image.Type.Simple;
			plate.color = PlateNormal;
			plate.raycastTarget = true;

			// 焦点反馈交给 SelectableHighlight，避免 ColorTint 的过渡把高亮颜色覆盖掉。
			button.transition = Selectable.Transition.None;
			button.targetGraphic = plate;

			var border = EnsureOutline(plate);
			var label = EnsureButtonLabel(button.transform, fontSize);
			var indicator = EnsureFocusBar(button.transform, size);

			var highlight = GetOrAdd<SelectableHighlight>(button.gameObject);
			ConfigureHighlight(highlight, plate, border, label, null, indicator, 1.04f);
		}

		static void StyleSlider(Slider slider, TMP_Text label, Vector2 position)
		{
			var sliderRect = Rect(slider);
			SetRect(sliderRect, position, new Vector2(340f, 30f));

			slider.transition = Selectable.Transition.None;

			var backgroundTransform = Require<Image>(slider.transform, "Background").transform;
			var background = backgroundTransform.GetComponent<Image>();
			background.sprite = null;
			background.type = Image.Type.Simple;
			background.color = PlateNormal;
			background.raycastTarget = true;
			var backgroundRect = (RectTransform)backgroundTransform;
			backgroundRect.anchorMin = new Vector2(0f, 0.5f);
			backgroundRect.anchorMax = new Vector2(1f, 0.5f);
			backgroundRect.pivot = new Vector2(0.5f, 0.5f);
			backgroundRect.offsetMin = new Vector2(0f, -6f);
			backgroundRect.offsetMax = new Vector2(0f, 6f);
			slider.targetGraphic = background;

			var fillArea = Require<RectTransform>(slider.transform, "Fill Area");
			fillArea.anchorMin = new Vector2(0f, 0.5f);
			fillArea.anchorMax = new Vector2(1f, 0.5f);
			fillArea.pivot = new Vector2(0.5f, 0.5f);
			fillArea.offsetMin = new Vector2(12f, -6f);
			fillArea.offsetMax = new Vector2(-12f, 6f);

			var fill = Require<Image>(slider.transform, "Fill Area/Fill");
			fill.sprite = null;
			fill.type = Image.Type.Simple;
			fill.color = Amber;
			fill.raycastTarget = false;
			Rect(fill).sizeDelta = Vector2.zero;

			var handleArea = Require<RectTransform>(slider.transform, "Handle Slide Area");
			handleArea.anchorMin = new Vector2(0f, 0.5f);
			handleArea.anchorMax = new Vector2(1f, 0.5f);
			handleArea.pivot = new Vector2(0.5f, 0.5f);
			handleArea.offsetMin = new Vector2(12f, -10f);
			handleArea.offsetMax = new Vector2(-12f, 10f);

			var handle = Require<Image>(slider.transform, "Handle Slide Area/Handle");
			handle.sprite = null;
			handle.type = Image.Type.Simple;
			handle.color = LabelNormal;
			handle.raycastTarget = true;
			var handleRect = Rect(handle);
			handleRect.pivot = new Vector2(0.5f, 0.5f);
			handleRect.anchoredPosition = Vector2.zero;
			handleRect.sizeDelta = new Vector2(20f, 20f);

			var highlight = GetOrAdd<SelectableHighlight>(slider.gameObject);
			ConfigureHighlight(highlight, background, null, label, handle, null, 1f);

			var highlightObject = new SerializedObject(highlight);
			highlightObject.FindProperty("labelFocus").colorValue = Amber;
			highlightObject.FindProperty("accentNormal").colorValue = LabelNormal;
			highlightObject.FindProperty("accentFocus").colorValue = Amber;
			highlightObject.ApplyModifiedPropertiesWithoutUndo();
		}

		static void ConfigureHighlight(SelectableHighlight highlight, Graphic plate, Outline border,
			TMP_Text label, Graphic accent, RectTransform indicator, float focusScale)
		{
			var serialized = new SerializedObject(highlight);
			serialized.FindProperty("plate").objectReferenceValue = plate;
			serialized.FindProperty("border").objectReferenceValue = border;
			serialized.FindProperty("label").objectReferenceValue = label;
			serialized.FindProperty("accent").objectReferenceValue = accent;
			serialized.FindProperty("indicator").objectReferenceValue = indicator;
			serialized.FindProperty("plateNormal").colorValue = PlateNormal;
			serialized.FindProperty("plateFocus").colorValue = PlateFocus;
			serialized.FindProperty("platePressed").colorValue = PlatePressed;
			serialized.FindProperty("borderNormal").colorValue = BorderNormal;
			serialized.FindProperty("borderFocus").colorValue = BorderFocus;
			serialized.FindProperty("labelNormal").colorValue = LabelNormal;
			serialized.FindProperty("labelFocus").colorValue = LabelFocus;
			serialized.FindProperty("focusScale").floatValue = focusScale;
			serialized.FindProperty("transitionSeconds").floatValue = 0.1f;
			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		static TMP_Text EnsureButtonLabel(Transform button, float fontSize)
		{
			var labelTransform = button.Find("Label") ?? button.Find("Text (TMP)");
			if (labelTransform == null)
			{
				var labelObject = new GameObject("Label", typeof(RectTransform));
				labelObject.layer = 0;
				labelObject.transform.SetParent(button, false);
				labelTransform = labelObject.transform;
			}

			labelTransform.gameObject.name = "Label";
			var label = labelTransform.GetComponent<TMP_Text>();
			if (label == null)
			{
				label = labelTransform.gameObject.AddComponent<TextMeshProUGUI>();
			}

			Stretch((RectTransform)labelTransform);
			label.font = Font;
			label.fontSize = fontSize;
			label.color = LabelNormal;
			label.alignment = TextAlignmentOptions.Center;
			label.characterSpacing = 2f;
			label.enableWordWrapping = false;
			label.overflowMode = TextOverflowModes.Overflow;
			label.raycastTarget = false;
			return label;
		}

		static RectTransform EnsureFocusBar(Transform button, Vector2 buttonSize)
		{
			var bar = EnsureImage(button, "FocusBar", Amber,
				new Vector2(-(buttonSize.x * 0.5f + 26f), 0f), new Vector2(6f, 34f));
			bar.gameObject.SetActive(false);
			return Rect(bar);
		}

		static Outline EnsureOutline(Graphic graphic)
		{
			var outline = graphic.GetComponent<Outline>();
			if (outline == null)
			{
				outline = graphic.gameObject.AddComponent<Outline>();
			}

			outline.effectColor = BorderNormal;
			outline.effectDistance = new Vector2(2f, -2f);
			outline.useGraphicAlpha = false;
			return outline;
		}

		#endregion

		#region 基础工具

		static Image EnsureImage(Transform parent, string name, Color color, Vector2 position, Vector2 size)
		{
			var existing = parent.Find(name);
			GameObject target;
			if (existing != null)
			{
				target = existing.gameObject;
			}
			else
			{
				target = new GameObject(name, typeof(RectTransform));
				target.layer = parent.gameObject.layer;
				target.transform.SetParent(parent, false);
			}

			var image = target.GetComponent<Image>();
			if (image == null)
			{
				image = target.AddComponent<Image>();
			}

			image.sprite = null;
			image.type = Image.Type.Simple;
			image.color = color;
			image.raycastTarget = false;
			SetRect(Rect(image), position, size);
			return image;
		}

		static TMP_Text EnsureText(Transform parent, string name, string content, float fontSize, Color color,
			TextAlignmentOptions alignment, Vector2 position, Vector2 size, float spacing)
		{
			var existing = parent.Find(name);
			TMP_Text text;
			if (existing != null)
			{
				text = existing.GetComponent<TMP_Text>();
			}
			else
			{
				var textObject = new GameObject(name, typeof(RectTransform));
				textObject.layer = parent.gameObject.layer;
				textObject.transform.SetParent(parent, false);
				text = textObject.AddComponent<TextMeshProUGUI>();
				text.text = content;
			}

			if (text == null)
			{
				throw new InvalidOperationException($"{name} 上缺少 TextMeshPro 组件");
			}

			StyleText(text, fontSize, color, alignment, position, size, spacing);
			return text;
		}

		static void StyleText(TMP_Text text, float fontSize, Color color, TextAlignmentOptions alignment,
			Vector2 position, Vector2 size, float spacing)
		{
			SetRect(Rect(text), position, size);
			text.font = Font;
			text.fontSize = fontSize;
			text.color = color;
			text.alignment = alignment;
			text.characterSpacing = spacing;
			text.enableWordWrapping = false;
			text.overflowMode = TextOverflowModes.Overflow;
			text.raycastTarget = false;
		}

		static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
		{
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.anchoredPosition = position;
			rect.sizeDelta = size;
		}

		static void Stretch(RectTransform rect)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
		}

		static RectTransform Rect(Component component)
		{
			return (RectTransform)component.transform;
		}

		static T Require<T>(Transform root, string path) where T : Component
		{
			var found = root.Find(path);
			if (found == null)
			{
				// 节点在第一轮重排里可能已被挂到新父级下，按名字兜底查找保证脚本可重复执行。
				var leafName = path.Substring(path.LastIndexOf('/') + 1);
				found = FindChildByName(root, leafName);
			}

			if (found == null)
			{
				throw new InvalidOperationException($"{root.name} 下找不到节点 {path}");
			}

			var component = found.GetComponent<T>();
			if (component == null)
			{
				throw new InvalidOperationException($"{root.name}/{path} 上没有 {typeof(T).Name}");
			}

			return component;
		}

		static Transform FindChildByName(Transform parent, string name)
		{
			var direct = parent.Find(name);
			if (direct != null)
			{
				return direct;
			}

			foreach (Transform child in parent)
			{
				var nested = FindChildByName(child, name);
				if (nested != null)
				{
					return nested;
				}
			}

			return null;
		}

		static T GetOrAdd<T>(GameObject target) where T : Component
		{
			var component = target.GetComponent<T>();
			return component != null ? component : target.AddComponent<T>();
		}

		#endregion
	}
}
