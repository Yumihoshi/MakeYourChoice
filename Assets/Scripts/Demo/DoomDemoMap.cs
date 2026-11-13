using System.Collections.Generic;
using UnityEngine;

namespace Demo
{
    /// <summary>
    /// 俯视网格世界。格子值 0 表示可通行，1~4 表示四种不同贴图的墙。
    /// 地图坐标 Y 轴向上，与渲染用的世界坐标保持一致。
    /// </summary>
    public sealed class DoomDemoMap
    {
        public const int Width = 16;
        public const int Height = 16;

        // 1 砖墙 / 2 石墙 / 3 木墙 / 4 科技墙
        private static readonly int[,] Cells =
        {
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            { 1, 0, 2, 2, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, 0, 1 },
            { 1, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 1 },
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            { 1, 0, 3, 0, 0, 0, 4, 4, 4, 0, 0, 0, 2, 0, 0, 1 },
            { 1, 0, 0, 0, 0, 0, 4, 0, 4, 0, 0, 0, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 0, 0, 4, 0, 4, 0, 0, 0, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        };

        public readonly List<DoomSprite> Sprites = new List<DoomSprite>(8);

        public DoomDemoMap()
        {
            Sprites.Add(new DoomSprite(8.5f, 8.5f, DoomSpriteKind.Demon));
            Sprites.Add(new DoomSprite(3.5f, 3.5f, DoomSpriteKind.Demon));
            Sprites.Add(new DoomSprite(12.5f, 3.5f, DoomSpriteKind.Demon));
            Sprites.Add(new DoomSprite(5.5f, 10.5f, DoomSpriteKind.Barrel));
            Sprites.Add(new DoomSprite(10.5f, 10.5f, DoomSpriteKind.Demon));
            Sprites.Add(new DoomSprite(4.5f, 13.5f, DoomSpriteKind.Barrel));
        }

        /// <summary>取格子值；越界返回 -1，调用方按墙处理即可。</summary>
        public int CellAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return -1;
            }

            return Cells[x, y];
        }

        public bool IsWall(int x, int y)
        {
            return CellAt(x, y) != 0;
        }
    }

    public enum DoomSpriteKind
    {
        Demon = 0,
        Barrel = 1,
    }

    /// <summary>世界里的公告板精灵，<see cref="VerticalOffset"/> 为正表示离地高度。</summary>
    public sealed class DoomSprite
    {
        public readonly Vector2 Position;
        public readonly DoomSpriteKind Kind;
        public float WorldHeight = 1f;
        public float VerticalOffset;

        public DoomSprite(float x, float y, DoomSpriteKind kind)
        {
            Position = new Vector2(x, y);
            Kind = kind;
        }
    }
}
