using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Dialogue;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
	/// <summary>
	/// 与豆袋交互：看过白鱼的画之后才能躺下；躺下播完台词后画面淡出到黑屏并停住。
	/// </summary>
	public sealed class BeanbagInteract : InteractableBase
	{
		[SerializeField, Required, LabelText("第一人称控制")] FirstPersonController firstPerson;
		[SerializeField, Required, LabelText("相机枢轴（眼睛）")] Transform lookPivot;
		[SerializeField, Required, LabelText("躺下剧情")] DialogueTrigger dialogue;
		[SerializeField, Required, LabelText("杯子站")] GalleryCupStation station;
		[SerializeField, LabelText("躺下朝向锚点")] Transform roomAnchor;
		[SerializeField, LabelText("黑屏层")] CanvasGroup blackout;

		[SerializeField, LabelText("躺下时长")] float lieDownSeconds = 1.2f;
		[SerializeField, LabelText("躺下高度")] float lieHeight = 0.55f;
		[SerializeField, LabelText("躺下前移")] float lieForward = 0.25f;
		[SerializeField, LabelText("躺下仰角")] float liePitch = 55f;
		[SerializeField, LabelText("躺下侧倾")] float lieRoll = 8f;
		[SerializeField, LabelText("黑屏时长")] float fadeSeconds = 2.5f;

		bool _busy;
		bool _fadedToBlack;

		protected override void Awake()
		{
			base.Awake();
			if (!enabled)
			{
				return;
			}

			if (firstPerson == null || lookPivot == null || dialogue == null || station == null)
			{
				Debug.LogError("BeanbagInteract 缺少第一人称控制、相机枢轴、躺下剧情或杯子站引用", this);
				enabled = false;
			}
		}

		protected override bool CanInteract()
		{
			return !_busy
				&& station != null
				&& station.PaintingSeen
				&& firstPerson != null
				&& dialogue != null
				&& !dialogue.IsRunning;
		}

		protected override void OnInteract() => Lie().Forget();

		async UniTaskVoid Lie()
		{
			_busy = true;
			var startPosition = lookPivot.localPosition;

			var yaw = firstPerson.transform.eulerAngles.y;
			if (roomAnchor != null)
			{
				var direction = roomAnchor.position - lookPivot.position;
				direction.y = 0f;
				if (direction.sqrMagnitude > 0.001f)
				{
					yaw = Quaternion.LookRotation(direction).eulerAngles.y;
				}
			}
			else
			{
				Debug.LogWarning("BeanbagInteract 没有指定躺下朝向锚点，改用玩家当前朝向", this);
			}

			firstPerson.enabled = false;

			var completion = new UniTaskCompletionSource();
			void OnDialogueEnded(DialogueEndedEvent ended)
			{
				if (ended.Trigger == dialogue)
				{
					completion.TrySetResult();
				}
			}

			var registration = TypeEventSystem.Global.Register<DialogueEndedEvent>(OnDialogueEnded).UnRegisterWhenGameObjectDestroyed(gameObject);

			try
			{
				var lieRotation = Quaternion.Euler(liePitch, yaw, lieRoll);
				var layDown = DOTween.Sequence();
				layDown.Append(lookPivot.DOLocalMove(new Vector3(0f, lieHeight, lieForward), lieDownSeconds).SetEase(Ease.OutBack));
				layDown.Join(lookPivot.DOLocalRotateQuaternion(lieRotation, lieDownSeconds).SetEase(Ease.OutQuad));
				await layDown.WaitForCompletionAsync(destroyCancellationToken);

				dialogue.StartDialogue();
				await completion.Task.AttachExternalCancellation(destroyCancellationToken);

				if (blackout != null)
				{
					blackout.gameObject.SetActive(true);
					blackout.alpha = 0f;
					await blackout.DOFade(1f, fadeSeconds).WaitForCompletionAsync(destroyCancellationToken);
					blackout.blocksRaycasts = true;
				}

				_fadedToBlack = true;
			}
			catch (OperationCanceledException)
			{
				return;
			}
			finally
			{
				registration.UnRegister();
				if (!_fadedToBlack && firstPerson != null)
				{
					firstPerson.enabled = true;
				}

				_busy = false;
			}
		}
	}
}
