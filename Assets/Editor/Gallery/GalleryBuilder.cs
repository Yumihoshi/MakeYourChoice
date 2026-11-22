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
		/// <summary>可走格子的边长：一格 2 m，走廊因此比表格原尺寸宽一倍。</summary>
		const float CellSize = 2f;
		/// <summary>墙体厚度固定 1 m，落在 2 m 的格距里，墙两边各留出 0.5 m 走道。</summary>
		const float WallThickness = 1f;
		const string MeshFolder = "Assets/Art/Meshes/艺术馆";
		const string MaterialFolder = "Assets/Materials/Art";

		// 52 列（x=0..51）× 46 行（z=0..45）。
		// 表格的列从左往右数，但美术要的是「表格左边 = 场景 -x」，所以落几何时统一把列号镜像一次。
		const int MapWidth = 52;

		// 52 列（x=0..51）× 46 行（z=0..45）。
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

		[MenuItem("Tools/艺术馆/1 生成网格资产")]
		public static void BuildMeshes()
		{
			EnsureFolder(MeshFolder);
			EnsureFolder(MaterialFolder);

			var wallCells = Select(c => c == '#');
			var doorCells = Select(c => c == 'O');
			var redCells = Select(c => c == 'R');

			SaveMesh(BuildWalls(wallCells, doorCells), MeshFolder + "/艺术馆墙.asset");
			SaveMesh(BuildFloor(), MeshFolder + "/艺术馆地面.asset");
			SaveMesh(BuildCeiling(), MeshFolder + "/艺术馆天花板.asset");
			SaveMesh(BuildRedCarpet(redCells), MeshFolder + "/中心地毯.asset");

			EnsureMaterial(MaterialFolder + "/艺术馆墙.mat", "Assets/Art/艺术馆/艺术馆墙.png");
			EnsureMaterial(MaterialFolder + "/艺术馆地面.mat", "Assets/Art/艺术馆/地毯.png");
			EnsureMaterial(MaterialFolder + "/艺术馆中心地毯.mat", "Assets/Art/艺术馆/中心地毯.png");

			AssetDatabase.SaveAssets();
			Debug.Log($"艺术馆几何已生成：墙 {wallCells.Count} 格 / 门 {doorCells.Count} 格 / 红毯 {redCells.Count} 格（一格 {CellSize} m，墙厚 {WallThickness} m）");
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

		/// <summary>格心世界坐标：表格列号镜像后乘格距，格子中心对中心。</summary>
		static Vector3 CellCenter(int cellX, int cellY) =>
			new Vector3((MapWidth - cellX - 0.5f) * CellSize, 0f, (cellY + 0.5f) * CellSize);

		static bool HasWall(HashSet<Vector2Int> wallCells, int cellX, int cellY) =>
			wallCells.Contains(new Vector2Int(cellX, cellY));

		/// <summary>
		/// 按四邻判断这格墙是沿 x 走、沿 z 走、拐角还是孤柱，据此决定 1 m 厚度压在哪个轴上；
		/// 拐角用整格填实，两个方向的墙才能接住、不留缝。
		/// </summary>
		static void WallHalfExtents(HashSet<Vector2Int> wallCells, Vector2Int cell, out float halfX, out float halfZ)
		{
			var alongX = HasWall(wallCells, cell.x - 1, cell.y) || HasWall(wallCells, cell.x + 1, cell.y);
			var alongZ = HasWall(wallCells, cell.x, cell.y - 1) || HasWall(wallCells, cell.x, cell.y + 1);

			if (alongX && alongZ)
			{
				halfX = CellSize * 0.5f;
				halfZ = CellSize * 0.5f;
			}
			else if (alongX)
			{
				halfX = CellSize * 0.5f;
				halfZ = WallThickness * 0.5f;
			}
			else if (alongZ)
			{
				halfX = WallThickness * 0.5f;
				halfZ = CellSize * 0.5f;
			}
			else
			{
				halfX = WallThickness * 0.5f;
				halfZ = WallThickness * 0.5f;
			}
		}

		/// <summary>建筑投影：z 17 行以上的 x&lt;6 列是场外空地，不铺地面与天花板。</summary>
		static readonly Rect[] FootprintPieces =
		{
			new Rect(0f, 0f, MapWidth * CellSize, 17 * CellSize),
			new Rect(0f, 17 * CellSize, (MapWidth - 6) * CellSize, (Map.Length - 17) * CellSize),
		};

		/// <summary>天花板洞口的水平范围（2×2 格，玩家从地漏坠入的位置）。</summary>
		static Rect HoleRect()
		{
			var minX = float.MaxValue;
			var maxX = float.MinValue;
			var minZ = float.MaxValue;
			var maxZ = float.MinValue;
			foreach (var cell in Hole)
			{
				var center = CellCenter(cell.x, cell.y);
				minX = Mathf.Min(minX, center.x - CellSize * 0.5f);
				maxX = Mathf.Max(maxX, center.x + CellSize * 0.5f);
				minZ = Mathf.Min(minZ, center.z - CellSize * 0.5f);
				maxZ = Mathf.Max(maxZ, center.z + CellSize * 0.5f);
			}

			return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
		}

		/// <summary>墙：逐格 1 m 厚的实体（拐角填实），门格上方补门楣（2.44..层高）。</summary>
		static Mesh BuildWalls(HashSet<Vector2Int> wallCells, HashSet<Vector2Int> doorCells)
		{
			var mesh = new Mesh { name = "艺术馆墙" };
			var vertices = new List<Vector3>();
			var triangles = new List<int>();
			var uvs = new List<Vector2>();

			foreach (var cell in wallCells)
			{
				WallHalfExtents(wallCells, cell, out var halfX, out var halfZ);
				var center = CellCenter(cell.x, cell.y);
				AddBox(vertices, triangles, uvs,
					center.x - halfX, center.x + halfX, center.z - halfZ, center.z + halfZ, 0f, CeilingHeight);
			}

			foreach (var cell in doorCells)
			{
				WallHalfExtents(wallCells, cell, out var halfX, out var halfZ);
				var center = CellCenter(cell.x, cell.y);
				AddBox(vertices, triangles, uvs,
					center.x - halfX, center.x + halfX, center.z - halfZ, center.z + halfZ, DoorHeight, CeilingHeight);
			}

			mesh.SetVertices(vertices);
			mesh.SetTriangles(triangles, 0);
			mesh.SetUVs(0, uvs);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		/// <summary>地面：整片铺满建筑投影（含墙下），墙压在它上面，墙脚不会露出缝。</summary>
		static Mesh BuildFloor()
		{
			var mesh = new Mesh { name = "艺术馆地面" };
			var vertices = new List<Vector3>();
			var triangles = new List<int>();
			var uvs = new List<Vector2>();

			foreach (var rect in FootprintPieces)
			{
				AddQuad(
					vertices, triangles, uvs,
					new Vector3(rect.xMin, 0f, rect.yMin), new Vector3(rect.xMin, 0f, rect.yMax),
					new Vector3(rect.xMax, 0f, rect.yMax), new Vector3(rect.xMax, 0f, rect.yMin),
					new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin, rect.yMax),
					new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.yMin));
			}

			mesh.SetVertices(vertices);
			mesh.SetTriangles(triangles, 0);
			mesh.SetUVs(0, uvs);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		/// <summary>天花板：铺满建筑投影，扣掉 2×2 格的洞口（玩家坠入的通道）。</summary>
		static Mesh BuildCeiling()
		{
			var mesh = new Mesh { name = "艺术馆天花板" };
			var vertices = new List<Vector3>();
			var triangles = new List<int>();
			var uvs = new List<Vector2>();

			var hole = HoleRect();
			foreach (var rect in FootprintPieces)
			{
				if (rect.xMax <= hole.xMin || rect.xMin >= hole.xMax || rect.yMax <= hole.yMin || rect.yMin >= hole.yMax)
				{
					AddCeilingQuad(vertices, triangles, uvs, rect);
					continue;
				}

				// 洞口只落在这一块里：把它拆成洞口四周的四条。
				AddCeilingQuad(vertices, triangles, uvs, Rect.MinMaxRect(rect.xMin, rect.yMin, rect.xMax, hole.yMin));
				AddCeilingQuad(vertices, triangles, uvs, Rect.MinMaxRect(rect.xMin, hole.yMax, rect.xMax, rect.yMax));
				AddCeilingQuad(vertices, triangles, uvs, Rect.MinMaxRect(rect.xMin, hole.yMin, hole.xMin, hole.yMax));
				AddCeilingQuad(vertices, triangles, uvs, Rect.MinMaxRect(hole.xMax, hole.yMin, rect.xMax, hole.yMax));
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

			// 红毯与其它几何一样按表格列镜像后再乘格距：格 [minX, maxX) → 世界 [MapWidth - maxX, MapWidth - minX) × CellSize。
			var worldMinX = (MapWidth - maxX) * CellSize;
			var worldMaxX = (MapWidth - minX) * CellSize;
			var worldMinZ = minZ * CellSize;
			var worldMaxZ = maxZ * CellSize;
			AddQuad(
				vertices, triangles, uvs,
				new Vector3(worldMinX, RedCarpetHeight, worldMinZ), new Vector3(worldMinX, RedCarpetHeight, worldMaxZ),
				new Vector3(worldMaxX, RedCarpetHeight, worldMaxZ), new Vector3(worldMaxX, RedCarpetHeight, worldMinZ),
				new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));

			mesh.SetVertices(vertices);
			mesh.SetTriangles(triangles, 0);
			mesh.SetUVs(0, uvs);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		/// <summary>天花板的一块朝下四边形；UV 用世界坐标，保证 1 m 一平铺。</summary>
		static void AddCeilingQuad(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Rect rect)
		{
			if (rect.width <= 0f || rect.height <= 0f)
			{
				return;
			}

			// 朝下的单面：绕序与地面相反，玩家从上方看是背面（被剔除），从下方看正常显示。
			AddQuad(
				vertices, triangles, uvs,
				new Vector3(rect.xMin, CeilingHeight, rect.yMin), new Vector3(rect.xMax, CeilingHeight, rect.yMin),
				new Vector3(rect.xMax, CeilingHeight, rect.yMax), new Vector3(rect.xMin, CeilingHeight, rect.yMax),
				new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin),
				new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax));
		}

		/// <summary>写一个轴对齐盒子的五个面（不出底面）；UV 用世界坐标，保证 1 m 一平铺。</summary>
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

