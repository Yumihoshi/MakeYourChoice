using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
	/// <summary>
	/// 艺术馆入场：玩家在地漏里坠落，全程只有近处能看清（线性雾拉到 3.5 m 以内），
	/// 落地后雾在 2 s 内恢复、约 1.73 s 交还控制并播落地台词。
	/// </summary>
	public sealed class GalleryEntryFall : MonoBehaviour
	{
		[SerializeField, Required, LabelText("玩家控制器")] CharacterController controller;
		[SerializeField, Required, LabelText("第一人称控制")] FirstPersonController firstPerson;
		[SerializeField, Required, LabelText("落地剧情")] DialogueTrigger landingDialogue;

		[SerializeField, LabelText("重力加速度")] float gravity = -22f;
		[SerializeField, LabelText("最大下落速度")] float maxFallSpeed = 25f;
		[SerializeField, LabelText("落地高度")] float groundY = 0.05f;
		[SerializeField, LabelText("落地超时兜底（秒）")] float fallTimeout = 3f;

		[SerializeField, LabelText("近雾起点")] float nearFogStart = 0.2f;
		[SerializeField, LabelText("近雾终点")] float nearFogEnd = 3.5f;
		[SerializeField, LabelText("正常雾起点")] float normalFogStart = 4f;
		[SerializeField, LabelText("正常雾终点")] float normalFogEnd = 30f;
		[SerializeField, LabelText("雾恢复时长")] float fogRestoreSeconds = 2f;
		[SerializeField, LabelText("交还控制延迟")] float releaseDelay = 1.73f;

		float _fallSpeed;
		float _fallTime;
		bool _landed;

		void Awake()
		{
			if (controller == null || firstPerson == null || landingDialogue == null)
			{
				Debug.LogError("GalleryEntryFall 缺少玩家控制器、第一人称控制或落地剧情引用", this);
				enabled = false;
				return;
			}

			// 雾当黑屏用：玩家此刻只能看清伸手可及的地方。
			RenderSettings.fog = true;
			RenderSettings.fogMode = FogMode.Linear;
			RenderSettings.fogColor = Color.black;
			RenderSettings.fogStartDistance = nearFogStart;
			RenderSettings.fogEndDistance = nearFogEnd;

			firstPerson.enabled = false;
			_fallSpeed = 0f;
			_fallTime = 0f;
		}

		void Update()
		{
			if (_landed)
			{
				return;
			}

			_fallTime += Time.deltaTime;
			_fallSpeed = Mathf.Max(_fallSpeed + gravity * Time.deltaTime, -maxFallSpeed);
			controller.Move(new Vector3(0f, _fallSpeed * Time.deltaTime, 0f));

			var grounded = controller.isGrounded || controller.transform.position.y <= groundY;
			if (grounded || _fallTime >= fallTimeout)
			{
				Land();
			}
		}

		void Land()
		{
			_landed = true;
			RestoreAfterLanding(destroyCancellationToken).Forget();
		}

		/// <summary>落地后把雾拉回正常值、交还控制，再播落地台词。</summary>
		async UniTaskVoid RestoreAfterLanding(CancellationToken cancellationToken)
		{
			try
			{
				var fogTween = DOTween.To(
						() => RenderSettings.fogEndDistance,
						value =>
						{
							RenderSettings.fogStartDistance = value * normalFogStart / normalFogEnd;
							RenderSettings.fogEndDistance = value;
						},
						normalFogEnd,
						fogRestoreSeconds)
					.SetEase(Ease.OutQuad);
				fogTween.WaitForCompletionAsync(cancellationToken).Forget();

				await UniTask.Delay(TimeSpan.FromSeconds(releaseDelay), DelayType.DeltaTime, cancellationToken: cancellationToken);

				firstPerson.enabled = true;
				landingDialogue.StartDialogue();
			}
			catch (OperationCanceledException)
			{
				// 场景卸载时静默退出
			}
		}
	}
}
