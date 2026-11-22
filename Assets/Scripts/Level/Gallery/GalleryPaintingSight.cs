using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
	/// <summary>
	/// 白鱼的画的视线判定：提交完杯子后，玩家在可视距离内正对画作时触发一次旁白。
	/// </summary>
	public sealed class GalleryPaintingSight : MonoBehaviour
	{
		[SerializeField, Required, LabelText("杯子站")] GalleryCupStation station;
		[SerializeField, Required, LabelText("画作")] Transform painting;
		[SerializeField, Required, LabelText("剧情")] DialogueTrigger dialogue;
		[SerializeField, LabelText("视线起点（玩家眼睛）")] Transform eye;
		[SerializeField, LabelText("判定相机")] Camera viewCamera;
		[SerializeField, LabelText("可视距离")] float maxDistance = 10f;
		[SerializeField, LabelText("对准角度")] float maxAngle = 18f;

		void Awake()
		{
			if (station == null || painting == null || dialogue == null)
			{
				Debug.LogError("GalleryPaintingSight 缺少杯子站、画作或剧情引用", this);
				enabled = false;
				return;
			}

			var mainCamera = Camera.main;
			if (eye == null)
			{
				eye = mainCamera != null ? mainCamera.transform : null;
			}

			if (viewCamera == null)
			{
				viewCamera = mainCamera;
			}

			if (eye == null || viewCamera == null)
			{
				Debug.LogError("GalleryPaintingSight 的视线起点或判定相机为空，且场景里没有 Main Camera", this);
				enabled = false;
			}
		}

		void Update()
		{
			if (station == null || station.PaintingSeen || !station.Submitted || dialogue == null)
			{
				return;
			}

			if (dialogue.IsRunning || !GameCursorState.IsPlaying)
			{
				return;
			}

			if (Vector3.Distance(eye.position, painting.position) > maxDistance)
			{
				return;
			}

			var viewport = viewCamera.WorldToViewportPoint(painting.position);
			if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
			{
				return;
			}

			var toPainting = painting.position - eye.position;
			var flatForward = eye.forward;
			toPainting.y = 0f;
			flatForward.y = 0f;
			if (flatForward.sqrMagnitude < 0.0001f || toPainting.sqrMagnitude < 0.0001f)
			{
				return;
			}

			if (Vector3.Angle(flatForward, toPainting) > maxAngle)
			{
				return;
			}

			station.MarkPaintingSeen();
			dialogue.StartDialogue();
		}
	}
}
