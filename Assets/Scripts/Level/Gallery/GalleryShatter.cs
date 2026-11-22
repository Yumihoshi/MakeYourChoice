using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Level
{
	/// <summary>
	/// 迷宫碎裂：撤掉可碎墙体与天花板，并在原位置生成一批受重力的碎块。
	/// 保留区域（提交区、白鱼的画房间）的墙体不参与碎裂。
	/// </summary>
	public sealed class GalleryShatter : MonoBehaviour
	{
		[SerializeField, Required, LabelText("可碎墙")] GameObject breakableWalls;
		[SerializeField, Required, LabelText("可碎天花板")] GameObject breakableCeiling;
		[SerializeField, LabelText("保留天花板")] GameObject retainedCeiling;
		[SerializeField, LabelText("墙碎块数量")] int wallChunks = 120;
		[SerializeField, LabelText("天花板碎块数量")] int ceilingChunks = 90;
		[SerializeField, LabelText("碎块存活秒数")] float chunkLifetime = 8f;
		[SerializeField, LabelText("爆炸力度")] float explosionSpeed = 3.5f;
		[SerializeField, LabelText("保留区域中心")] Vector2 retainedCenter = new Vector2(96.5f, 12f);
		[SerializeField, LabelText("保留区域尺寸")] Vector2 retainedSize = new Vector2(14f, 28f);

		bool _shattered;

		/// <summary>
		/// 播放一次碎裂效果：隐藏原网格并抛出碎块。重复调用无效。
		/// </summary>
		public void Shatter()
		{
			if (_shattered)
			{
				return;
			}

			_shattered = true;

			Transform player = FindPlayer();
			Vector3 origin = player != null ? player.position : transform.position;

			SpawnWallChunks(breakableWalls, wallChunks, origin);
			SpawnCeilingChunks(breakableCeiling, ceilingChunks, origin);

			if (retainedCeiling != null)
			{
				retainedCeiling.SetActive(true);
			}

			FlashAsync().Forget();
			CollapseAsync().Forget();
		}

		/// <summary>
		/// 稍等一帧再隐藏原网格，让碎块先脱离原位，避免墙体“瞬间消失”。
		/// </summary>
		async UniTaskVoid CollapseAsync()
		{
			try
			{
				await UniTask.Delay(TimeSpan.FromSeconds(0.12f), DelayType.DeltaTime, cancellationToken: destroyCancellationToken);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			Hide(breakableWalls);
			Hide(breakableCeiling);
		}

		/// <summary>
		/// 碎裂瞬间的闪光，照亮四散的碎块。
		/// </summary>
		async UniTaskVoid FlashAsync()
		{
			GameObject flash = new GameObject("碎裂闪光");
			flash.transform.SetParent(transform, false);
			Light light = flash.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = new Color(1f, 0.95f, 0.85f);
			light.range = 26f;
			light.intensity = 9f;

			Tween fade = DOTween.To(() => light.intensity, value => light.intensity = value, 0f, 1.1f);
			try
			{
				await fade.WaitForCompletionAsync(destroyCancellationToken);
			}
			catch (OperationCanceledException)
			{
				//
			}

			if (flash != null)
			{
				Destroy(flash);
			}
		}

		static Transform FindPlayer()
		{
			GameObject player = GameObject.FindWithTag("Player");
			if (player != null)
			{
				return player.transform;
			}

			Camera camera = Camera.main;
			return camera != null ? camera.transform : null;
		}

		static void Hide(GameObject root)
		{
			if (root == null)
			{
				return;
			}

			foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
			{
				renderer.enabled = false;
			}

			foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
			{
				collider.enabled = false;
			}
		}

		void SpawnWallChunks(GameObject root, int count, Vector3 origin)
		{
			if (root == null || count <= 0)
			{
				return;
			}

			MeshFilter filter = root.GetComponentInChildren<MeshFilter>(true);
			MeshRenderer renderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
			if (filter == null || filter.sharedMesh == null)
			{
				Debug.LogError("GalleryShatter 的可碎墙缺少网格：" + root.name, this);
				return;
			}

			Mesh mesh = filter.sharedMesh;
			Vector3[] vertices = mesh.vertices;
			int[] triangles = mesh.triangles;
			if (triangles.Length < 3)
			{
				return;
			}

			Material material = renderer != null && renderer.sharedMaterials.Length > 0 ? renderer.sharedMaterials[0] : null;
			Transform meshTransform = filter.transform;
			int triangleCount = triangles.Length / 3;

			for (int i = 0; i < count; i++)
			{
				int triangle = Random.Range(0, triangleCount);
				Vector3 a = meshTransform.TransformPoint(vertices[triangles[triangle * 3]]);
				Vector3 b = meshTransform.TransformPoint(vertices[triangles[triangle * 3 + 1]]);
				Vector3 c = meshTransform.TransformPoint(vertices[triangles[triangle * 3 + 2]]);
				Vector3 center = (a + b + c) / 3f;
				float size = Mathf.Clamp(Vector3.Distance(a, b) * 0.35f, 0.25f, 1.2f);
				CreateChunk(center, new Vector3(size, size * Random.Range(0.6f, 1.4f), 0.25f), material, origin);
			}
		}

		void SpawnCeilingChunks(GameObject root, int count, Vector3 origin)
		{
			if (root == null || count <= 0)
			{
				return;
			}

			Renderer renderer = root.GetComponentInChildren<Renderer>(true);
			MeshFilter filter = root.GetComponentInChildren<MeshFilter>(true);
			if (renderer == null || filter == null)
			{
				Debug.LogError("GalleryShatter 的可碎天花板缺少渲染器：" + root.name, this);
				return;
			}

			Bounds bounds = renderer.bounds;
			Material material = renderer.sharedMaterials.Length > 0 ? renderer.sharedMaterials[0] : null;
			Vector2 min = retainedCenter - retainedSize * 0.5f;
			Vector2 max = retainedCenter + retainedSize * 0.5f;

			for (int i = 0; i < count; i++)
			{
				Vector3 position = Vector3.zero;
				for (int attempt = 0; attempt < 12; attempt++)
				{
					float x = Random.Range(bounds.min.x, bounds.max.x);
					float z = Random.Range(bounds.min.z, bounds.max.z);
					position = new Vector3(x, bounds.max.y - 0.05f, z);
					bool retained = x >= min.x && x <= max.x && z >= min.y && z <= max.y;
					if (!retained)
					{
						break;
					}
				}

				float size = Random.Range(0.4f, 1.3f);
				CreateChunk(position, new Vector3(size, 0.25f, size * Random.Range(0.6f, 1.5f)), material, origin);
			}
		}

		void CreateChunk(Vector3 position, Vector3 scale, Material material, Vector3 origin)
		{
			GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
			chunk.name = "碎裂块";
			chunk.transform.SetParent(transform, false);
			chunk.transform.position = position;
			chunk.transform.rotation = Random.rotation;
			chunk.transform.localScale = scale;

			if (material != null)
			{
				chunk.GetComponent<MeshRenderer>().sharedMaterial = material;
			}

			Rigidbody body = chunk.AddComponent<Rigidbody>();
			body.mass = 1f;
			body.interpolation = RigidbodyInterpolation.Interpolate;
			Vector3 direction = (position - origin).normalized;
			body.velocity = direction * Random.Range(0.5f, explosionSpeed) + Vector3.up * Random.Range(1f, 3f);
			Destroy(chunk, chunkLifetime);
		}
	}
}
