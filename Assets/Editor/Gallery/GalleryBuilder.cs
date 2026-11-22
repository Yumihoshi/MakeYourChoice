using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTools
{
	/// <summary>
	/// 把《Back D Room》艺术馆区域的单元格网格烘成 4 份网格资产（墙 / 地面 / 天花板 / 中心地毯），
	/// UV 按世界坐标 1 m 一平铺，中心地毯 UV 为 0–1。可重复执行，已有资产会被覆盖。
	/// </summary>
	public static class GalleryBuilder
	{
		const float CeilingHeight = 4.35f;
		const float DoorHeight = 2.44f;
		const float RedCarpetHeight = 0.012f;
		const string MeshFolder = "Assets/Art/Meshes/艺术馆";
		const string MaterialFolder = "Assets/Materials/Art";

		// 52 列（x=0..51）× 46 行（z=0..45），一格 1 m。
		// 表格的列从左往右数，但美术要的是「表格左边 = 场景 -x」，所以落几何时统一把列号镜像一次。
		const int MapWidth = 52;

		static int MirrorX(int cellX) => MapWidth - 1 - cellX;

		// 52 列（x=0..51）× 46 行（z=0..45），一格 1 m。
		// 符号：# 墙 / . 地面 / R 中心红毯 / D 暗橙标记（画、提交区）/ Y 杯子 / O 门 / B 豆袋
		static readonly string[] Map =
		{
			"####################################################",
			"#.DDD.#.....#...........#.....#....................#",
			"#.....#.....#...........#.....#....................#",
			"#.....#..####..####..#..#..#..#..#..#############..#",
			"#B....#........#.....#..#..#..#..#..#...........#..#",
			"#.....#........#.....#..#..#..#..#..#Y..........#..#",
			"###O###..#######..#######..#..####..#######..####..#",
			"#.....#..#.....#.....#.....#.....#..#.....#........#",
			"#.....#..#.....#.....#.....#.....#..#.....#........#",
			"#D....#..#..#..####..#..#..#######..#..#..#######..#",
			"#D....#..#..#........#..#........#.....#........#..#",
			"#D....O..#..#........#..#........#.....#........#..#",
			"#D....####..##########..#######..#############..#..#",
			"#D....#.....#..#..............#.....#........#..#..#",
			"#.....#.....#..#..............#.....#........#..#..#",
			"#.....#..####..#..##########..#######..#######..####",
			"#######.....#.....#........#..#........#........#..#",
			"......#.....#.....#........#..#........#........#..#",
			"......#..#..#######..#..####..#..####..#..#######..#",
			"......#..#........#..#..#RRRRRRRR#.....#........#..#",
			"......#..#........#..#..#RRRRRRRR#.....#........#..#",
			"......#..#######..#..#..#RRRRRRRR#..#######..#..#..#",
			"......#..#..#.....#..#..#RRRRRRRR#........#..#.....#",
			"......#..#..#.....#..#..#RRRRRRRR#........#..#.....#",
			"......#..#..#..####..#..#RRRRRRRR#######..#..####..#",
			"......#.....#........#...RRRRRRRR............#..#..#",
			"......#.....#........#...RRRRRRRR............#..#..#",
			"......####..################..#..##########..#..####",
			"......#.....#..............#..#...........#..#.....#",
			"......#.....#..............#..#...........#..#.....#",
			"......#..####..####..####..#..#######..#..#..####..#",
			"......#..#.....#..#.....#.....#........#..#..#.....#",
			"......#..#.....#..#.....#.....#........#..#..#.....#",
			"......#..#..####..####..#..#..#..#..#######..#..####",
			"......#..#.....#.....#.....#.....#........#.....#.Y#",
			"......#..#.....#.....#.....#.....#........#.....#..#",
			"......#..####..#..##########..####..####..#######..#",
			"......#..#.....#...........#.....#.....#........#..#",
			"......#..#.....#...........#.....#.....#........#..#",
			"......#..#..####..#..####..####..#######..####..#..#",
			"......#..#..#.....#.....#..#.....#.....#.....#.....#",
			"......#..#..#.....#.....#..#.....#.....#.....#.....#",
			"......#..#..#..#######..####..####..#..####..####..#",
			"......#.....#.......Y#..............#........#.....#",
			"......#.....#........#..............#........#.....#",
			"......##############################################",
		};

		/// <summary>天花板上的 2×2 洞口（玩家从地漏坠入的位置）。</summary>
		static readonly Vector2Int[] Hole =
		{
			new Vector2Int(28, 22), new Vector2Int(29, 22),
			new Vector2Int(28, 23), new Vector2Int(29, 23),
		};

		/// <summary>建筑之外的场外空地：x &lt; 6 且 z ≥ 17 的格子不建任何几何。</summary>
		static bool IsOutsideBuilding(Vector2Int cell) => cell.x < 6 && cell.y >= 17;

		[MenuItem("Tools/艺术馆/1 生成网格资产")]
		public static void BuildMeshes()
		{
			EnsureFolder(MeshFolder);
			EnsureFolder(MaterialFolder);

			var wallCells = Select(c => c == '#');
			var floorCells = Select(c => c == '.' || c == 'Y' || c == 'D' || c == 'B' || c == 'O');
			floorCells.RemoveWhere(IsOutsideBuilding);
			var redCells = Select(c => c == 'R');
			var doorCells = Select(c => c == 'O');

			var ceilingCells = new HashSet<Vector2Int>(floorCells);
			ceilingCells.UnionWith(redCells);
			ceilingCells.ExceptWith(new HashSet<Vector2Int>(Hole));

			SaveMesh(BuildWalls(Merge(wallCells), Merge(doorCells)), MeshFolder + "/艺术馆墙.asset");
			SaveMesh(BuildFloor(Merge(floorCells)), MeshFolder + "/艺术馆地面.asset");
			SaveMesh(BuildCeiling(Merge(ceilingCells)), MeshFolder + "/艺术馆天花板.asset");
			SaveMesh(BuildRedCarpet(redCells), MeshFolder + "/中心地毯.asset");

			EnsureMaterial(MaterialFolder + "/艺术馆墙.mat", "Assets/Art/艺术馆/艺术馆墙.png");
			EnsureMaterial(MaterialFolder + "/艺术馆地面.mat", "Assets/Art/艺术馆/地毯.png");
			EnsureMaterial(MaterialFolder + "/艺术馆中心地毯.mat", "Assets/Art/艺术馆/中心地毯.png");

			AssetDatabase.SaveAssets();
			Debug.Log($"艺术馆几何已生成：墙 {wallCells.Count} 格 / 地面 {floorCells.Count} 格 / 天花 {ceilingCells.Count} 格 / 红毯 {redCells.Count} 格");
		}

		[MenuItem("Tools/艺术馆/2 摆入当前场景")]
		public static void PlaceIntoScene()
		{
			var parent = EnsurePath("= 关卡/艺术馆/建筑");
			Place(parent, "墙", MeshFolder + "/艺术馆墙.asset", MaterialFolder + "/艺术馆墙.mat", true);
			Place(parent, "地面", MeshFolder + "/艺术馆地面.asset", MaterialFolder + "/艺术馆地面.mat", true);
			Place(parent, "天花板", MeshFolder + "/艺术馆天花板.asset", "Assets/Materials/Level/Ceiling.mat", false);
			// 中心地毯占了 8×8 格，但地面网格不含红毯格，所以红毯自己必须带碰撞体，否则玩家坠入时直接穿地。
			Place(parent, "中心地毯", MeshFolder + "/中心地毯.asset", MaterialFolder + "/艺术馆中心地毯.mat", true);

			EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
			Debug.Log("艺术馆几何已摆入当前场景的「= 关卡/艺术馆/建筑」下");
		}

		/// <summary>收集满足条件的格子（x=列，z=行）。</summary>
		static HashSet<Vector2Int> Select(System.Func<char, bool> match)
		{
			var cells = new HashSet<Vector2Int>();
			for (var z = 0; z < Map.Length; z++)
			{
				for (var x = 0; x < Map[z].Length; x++)
				{
					if (match(Map[z][x]))
					{
						cells.Add(new Vector2Int(x, z));
					}
				}
			}

			return cells;
		}

		/// <summary>把格子贪心合并成矩形，减少面的数量。</summary>
		static List<RectInt> Merge(HashSet<Vector2Int> cells)
		{
			var remaining = new HashSet<Vector2Int>(cells);
			var rects = new List<RectInt>();

			while (remaining.Count > 0)
			{
				var minZ = int.MaxValue;
				var minX = int.MaxValue;
				foreach (var cell in remaining)
				{
					if (cell.y < minZ || (cell.y == minZ && cell.x < minX))
					{
						minZ = cell.y;
						minX = cell.x;
					}
				}

				var width = 0;
				while (remaining.Contains(new Vector2Int(minX + width, minZ)))
				{
					width++;
				}

				var height = 0;
				while (true)
				{
					var full = true;
					for (var x = minX; x < minX + width; x++)
					{
						if (!remaining.Contains(new Vector2Int(x, minZ + height)))
						{
							full = false;
							break;
						}
					}

					if (!full)
					{
						break;
					}

					height++;
				}

				for (var z = minZ; z < minZ + height; z++)
				{
					for (var x = minX; x < minX + width; x++)
					{
						remaining.Remove(new Vector2Int(x, z));
					}
				}

				rects.Add(new RectInt(minX, minZ, width, height));
			}

			return rects;
		}

		/// <summary>墙：每格一根 0..层高的实体柱，外加门格上方的门楣（2.44..层高）。</summary>
		static Mesh BuildWalls(List<RectInt> wallRects, List<RectInt> doorRects)
		{
			var mesh = new Mesh { name = "艺术馆墙" };
			var vertices = new List<Vector3>();
			var triangles = new List<int>();
			var uvs = new List<Vector2>();

			foreach (var rect in wallRects)
			{
				var x0 = MirrorX(rect.xMax - 1);
				var x1 = MirrorX(rect.xMin) + 1;
				var z0 = rect.yMin;
				var z1 = rect.yMax;
				AddBox(vertices, triangles, uvs, x0, x1, z0, z1, 0f, CeilingHeight);
			}

			foreach (var rect in doorRects)
			{
				var x0 = MirrorX(rect.xMax - 1);
				var x1 = MirrorX(rect.xMin) + 1;
				AddBox(vertices, triangles, uvs, x0, x1, rect.yMin, rect.yMax, DoorHeight, CeilingHeight);
			}

			mesh.SetVertices(vertices);
			mesh.SetTriangles(triangles, 0);
			mesh.SetUVs(0, uvs);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		static Mesh BuildFloor(List<RectInt> rects)
		{
			var mesh = new Mesh { name = "艺术馆地面" };
			var vertices = new List<Vector3>();
			var triangles = new List<int>();
			var uvs = new List<Vector2>();

			foreach (var rect in rects)
			{
				var x0 = MirrorX(rect.xMax - 1);
				var x1 = MirrorX(rect.xMin) + 1;
				var z0 = rect.yMin;
				var z1 = rect.yMax;
				AddQuad(
					vertices, triangles, uvs,
					new Vector3(x0, 0f, z0), new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1), new Vector3(x1, 0f, z0),
					new Vector2(x0, z0), new Vector2(x0, z1), new Vector2(x1, z1), new Vector2(x1, z0));
			}

			mesh.SetVertices(vertices);
			mesh.SetTriangles(triangles, 0);
			mesh.SetUVs(0, uvs);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		static Mesh BuildCeiling(List<RectInt> rects)
		{
			var mesh = new Mesh { name = "艺术馆天花板" };
			var vertices = new List<Vector3>();
			var triangles = new List<int>();
			var uvs = new List<Vector2>();

			foreach (var rect in rects)
			{
				var x0 = MirrorX(rect.xMax - 1);
				var x1 = MirrorX(rect.xMin) + 1;
				var z0 = rect.yMin;
				var z1 = rect.yMax;
				// 朝下的单面：绕序与地面相反，玩家从上方看是背面（被剔除），从下方看正常显示。
				AddQuad(
					vertices, triangles, uvs,
					new Vector3(x0, CeilingHeight, z0), new Vector3(x1, CeilingHeight, z0),
					new Vector3(x1, CeilingHeight, z1), new Vector3(x0, CeilingHeight, z1),
					new Vector2(x0, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0, z1));
			}

			mesh.SetVertices(vertices);
			mesh.SetTriangles(triangles, 0);
			mesh.SetUVs(0, uvs);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		/// <summary>中心红毯：一整张 8×8 的 Quad，UV 0–1 拉伸到边界，不平铺。</summary>
		static Mesh BuildRedCarpet(HashSet<Vector2Int> redCells)
		{
			var mesh = new Mesh { name = "中心地毯" };
			var vertices = new List<Vector3>();
			var triangles = new List<int>();
			var uvs = new List<Vector2>();

			var minX = int.MaxValue;
			var minZ = int.MaxValue;
			var maxX = int.MinValue;
			var maxZ = int.MinValue;
			foreach (var cell in redCells)
			{
				minX = Mathf.Min(minX, cell.x);
				minZ = Mathf.Min(minZ, cell.y);
				maxX = Mathf.Max(maxX, cell.x + 1);
				maxZ = Mathf.Max(maxZ, cell.y + 1);
			}

			// 红毯与其它几何一样按表格列镜像：格 [minX, maxX) → 世界 [MapWidth - maxX, MapWidth - minX)。
			var worldMinX = (float)(MapWidth - maxX);
			var worldMaxX = (float)(MapWidth - minX);
			AddQuad(
				vertices, triangles, uvs,
				new Vector3(worldMinX, RedCarpetHeight, minZ), new Vector3(worldMinX, RedCarpetHeight, maxZ),
				new Vector3(worldMaxX, RedCarpetHeight, maxZ), new Vector3(worldMaxX, RedCarpetHeight, minZ),
				new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));

			mesh.SetVertices(vertices);
			mesh.SetTriangles(triangles, 0);
			mesh.SetUVs(0, uvs);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		/// <summary>写一个轴对齐盒子的六个面；UV 用世界坐标，保证 1 m 一平铺。</summary>
		static void AddBox(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs,
			float x0, float x1, float z0, float z1, float y0, float y1)
		{
			AddQuad(vertices, triangles, uvs,
				new Vector3(x1, y0, z1), new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1),
				new Vector2(z1, y0), new Vector2(z0, y0), new Vector2(z0, y1), new Vector2(z1, y1));
			AddQuad(vertices, triangles, uvs,
				new Vector3(x0, y0, z0), new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0),
				new Vector2(z0, y0), new Vector2(z1, y0), new Vector2(z1, y1), new Vector2(z0, y1));
			AddQuad(vertices, triangles, uvs,
				new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1),
				new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1));
			AddQuad(vertices, triangles, uvs,
				new Vector3(x1, y0, z0), new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0),
				new Vector2(x1, y0), new Vector2(x0, y0), new Vector2(x0, y1), new Vector2(x1, y1));
			AddQuad(vertices, triangles, uvs,
				new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0),
				new Vector2(x0, z0), new Vector2(x0, z1), new Vector2(x1, z1), new Vector2(x1, z0));
		}

		static void AddQuad(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs,
			Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
		{
			var start = vertices.Count;
			vertices.Add(a);
			vertices.Add(b);
			vertices.Add(c);
			vertices.Add(d);
			uvs.Add(ua);
			uvs.Add(ub);
			uvs.Add(uc);
			uvs.Add(ud);
			triangles.Add(start);
			triangles.Add(start + 1);
			triangles.Add(start + 2);
			triangles.Add(start);
			triangles.Add(start + 2);
			triangles.Add(start + 3);
		}

		static void SaveMesh(Mesh mesh, string path)
		{
			if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
			{
				AssetDatabase.DeleteAsset(path);
			}

			AssetDatabase.CreateAsset(mesh, path);
		}

		static Material EnsureMaterial(string path, string texturePath)
		{
			var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
			if (texture == null)
			{
				Debug.LogError($"艺术馆材质缺少贴图：{texturePath}");
				return null;
			}

			var material = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (material == null)
			{
				material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
				AssetDatabase.CreateAsset(material, path);
			}

			material.SetTexture("_BaseMap", texture);
			material.SetFloat("_Smoothness", 0.05f);
			material.SetFloat("_Metallic", 0f);
			EditorUtility.SetDirty(material);
			return material;
		}

		static void Place(Transform parent, string name, string meshPath, string materialPath, bool withCollider)
		{
			var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
			var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
			if (mesh == null || material == null)
			{
				Debug.LogError($"艺术馆几何缺少资产：{meshPath} 或 {materialPath}，请先执行「1 生成网格资产」");
				return;
			}

			var existing = parent.Find(name);
			var go = existing != null ? existing.gameObject : new GameObject(name);
			if (existing == null)
			{
				go.transform.SetParent(parent, false);
			}

			var filter = go.GetComponent<MeshFilter>();
			if (filter == null)
			{
				filter = go.AddComponent<MeshFilter>();
			}

			filter.sharedMesh = mesh;

			var rendererComponent = go.GetComponent<MeshRenderer>();
			if (rendererComponent == null)
			{
				rendererComponent = go.AddComponent<MeshRenderer>();
			}

			rendererComponent.sharedMaterial = material;
			rendererComponent.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

			var collider = go.GetComponent<MeshCollider>();
			if (withCollider)
			{
				collider = collider != null ? collider : go.AddComponent<MeshCollider>();
				collider.sharedMesh = mesh;
			}
			else if (collider != null)
			{
				Object.DestroyImmediate(collider);
			}
		}

		static Transform EnsurePath(string path)
		{
			var current = GameObject.Find(path);
			if (current != null)
			{
				return current.transform;
			}

			Transform parent = null;
			foreach (var segment in path.Split('/'))
			{
				var child = parent == null ? GameObject.Find(segment)?.transform : parent.Find(segment);
				if (child == null)
				{
					child = new GameObject(segment).transform;
					child.SetParent(parent, false);
				}

				parent = child;
			}

			return parent;
		}

		static void EnsureFolder(string path)
		{
			if (AssetDatabase.IsValidFolder(path))
			{
				return;
			}

			var parts = path.Split('/');
			var current = parts[0];
			for (var i = 1; i < parts.Length; i++)
			{
				var next = current + "/" + parts[i];
				if (!AssetDatabase.IsValidFolder(next))
				{
					AssetDatabase.CreateFolder(current, parts[i]);
				}

				current = next;
			}
		}
	}
}

