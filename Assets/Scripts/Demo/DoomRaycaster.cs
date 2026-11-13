using System;
using System.Collections.Generic;
using UnityEngine;

namespace Demo
{
    /// <summary>
    /// 逐列光线投射渲染器：把俯视网格地图投影成第一人称画面，结果写入 <see cref="Buffer"/>。
    /// 全部在 CPU 上完成，不依赖渲染管线或 Shader，因此 2D/3D 管线里都能跑。
    /// </summary>
    public sealed class DoomRaycaster
    {
        public const int TextureSize = 64;
        private const int TextureMask = TextureSize - 1;

        /// <summary>眼睛离地高度（世界单位），墙和天花板都是 1 单位高。</summary>
        private const double EyeHeight = 0.5;

        private readonly int _width;
        private readonly int _height;
        private readonly int _halfHeight;

        /// <summary>tan(水平 FOV / 2)；固定 90°，所以等于 1。</summary>
        private readonly double _planeScale;

        /// <summary>投影平面到眼睛的距离（像素）：projDist = (width / 2) / tan(hFov / 2)。</summary>
        private readonly double _projDistance;

        private readonly Color32[][] _wallTextures;
        private readonly Color32[] _floorTexture;
        private readonly Color32[] _ceilingTexture;
        private readonly Color32[] _demonTexture;
        private readonly Color32[] _barrelTexture;

        private readonly double[] _zBuffer;
        private readonly Color32[] _buffer;
        private readonly List<DoomSprite> _spriteOrder = new List<DoomSprite>(16);

        public Color32[] Buffer => _buffer;

        public DoomRaycaster(int width, int height)
        {
            _width = width;
            _height = height;
            _halfHeight = height / 2;
            _planeScale = 1.0;
            _projDistance = width * 0.5 / _planeScale;

            _buffer = new Color32[width * height];
            _zBuffer = new double[width];

            _wallTextures = new[]
            {
                CreateBrickTexture(),
                CreateStoneTexture(),
                CreateWoodTexture(),
                CreateTechTexture(),
            };
            _floorTexture = CreateFloorTexture();
            _ceilingTexture = CreateCeilingTexture();
            _demonTexture = CreateDemonTexture();
            _barrelTexture = CreateBarrelTexture();
        }

        /// <summary>渲染一帧。屏幕坐标 y=0 在顶部，与 <see cref="Buffer"/> 的行序一致。</summary>
        public void Render(DoomDemoMap map, Vector2 position, float yawDegrees)
        {
            double radians = yawDegrees * Math.PI / 180.0;
            double dirX = Math.Cos(radians);
            double dirY = Math.Sin(radians);
            // 相机平面取「右手方向」，这样屏幕最左列对应 dir 逆时针转 90°。
            double planeX = dirY * _planeScale;
            double planeY = -dirX * _planeScale;

            RenderFloorAndCeiling(position, dirX, dirY, planeX, planeY);
            RenderWalls(map, position, dirX, dirY, planeX, planeY);
            RenderSprites(map, position, dirX, dirY, planeX, planeY);
        }

        private void RenderFloorAndCeiling(Vector2 position, double dirX, double dirY, double planeX, double planeY)
        {
            int width = _width;
            int height = _height;

            double leftRayX = dirX - planeX;
            double leftRayY = dirY - planeY;
            double rightRayX = dirX + planeX;
            double rightRayY = dirY + planeY;

            // 天花板：屏幕上方的行，y 越小越远。
            for (int y = 0; y < _halfHeight; y++)
            {
                int rowsFromHorizon = _halfHeight - y;
                double rowDistance = EyeHeight * _projDistance / rowsFromHorizon;
                DrawFloorRow(y, position, leftRayX, leftRayY, rightRayX, rightRayY, rowDistance,
                    _ceilingTexture, LightFor(rowDistance) * 5 / 8);
            }

            // 地板：屏幕下方的行，y 越大越近。
            for (int y = _halfHeight + 1; y < height; y++)
            {
                int rowsFromHorizon = y - _halfHeight;
                double rowDistance = EyeHeight * _projDistance / rowsFromHorizon;
                DrawFloorRow(y, position, leftRayX, leftRayY, rightRayX, rightRayY, rowDistance,
                    _floorTexture, LightFor(rowDistance));
            }
        }

        private void DrawFloorRow(int y, Vector2 position, double leftRayX, double leftRayY,
            double rightRayX, double rightRayY, double rowDistance, Color32[] texture, int light)
        {
            int width = _width;
            double stepX = rowDistance * (rightRayX - leftRayX) / width;
            double stepY = rowDistance * (rightRayY - leftRayY) / width;
            double floorX = position.x + rowDistance * leftRayX;
            double floorY = position.y + rowDistance * leftRayY;
            int rowOffset = y * width;

            for (int x = 0; x < width; x++)
            {
                int cellX = (int)Math.Floor(floorX);
                int cellY = (int)Math.Floor(floorY);
                int texX = (int)(TextureSize * (floorX - cellX)) & TextureMask;
                int texY = (int)(TextureSize * (floorY - cellY)) & TextureMask;
                floorX += stepX;
                floorY += stepY;
                _buffer[rowOffset + x] = Shade(texture[texY * TextureSize + texX], light);
            }
        }

        private void RenderWalls(DoomDemoMap map, Vector2 position, double dirX, double dirY,
            double planeX, double planeY)
        {
            int width = _width;
            int height = _height;

            for (int x = 0; x < width; x++)
            {
                double cameraX = 2.0 * x / width - 1.0;
                double rayDirX = dirX + planeX * cameraX;
                double rayDirY = dirY + planeY * cameraX;

                int mapX = (int)Math.Floor(position.x);
                int mapY = (int)Math.Floor(position.y);

                double deltaDistX = Math.Abs(rayDirX) < 1e-9 ? 1e30 : Math.Abs(1.0 / rayDirX);
                double deltaDistY = Math.Abs(rayDirY) < 1e-9 ? 1e30 : Math.Abs(1.0 / rayDirY);

                int stepX, stepY;
                double sideDistX, sideDistY;

                if (rayDirX < 0)
                {
                    stepX = -1;
                    sideDistX = (position.x - mapX) * deltaDistX;
                }
                else
                {
                    stepX = 1;
                    sideDistX = (mapX + 1.0 - position.x) * deltaDistX;
                }

                if (rayDirY < 0)
                {
                    stepY = -1;
                    sideDistY = (position.y - mapY) * deltaDistY;
                }
                else
                {
                    stepY = 1;
                    sideDistY = (mapY + 1.0 - position.y) * deltaDistY;
                }

                // DDA：沿射线逐格前进，直到撞到非空格子。
                int side = 0;
                int wallType;
                int guard = 0;
                do
                {
                    if (sideDistX < sideDistY)
                    {
                        sideDistX += deltaDistX;
                        mapX += stepX;
                        side = 0;
                    }
                    else
                    {
                        sideDistY += deltaDistY;
                        mapY += stepY;
                        side = 1;
                    }

                    wallType = map.CellAt(mapX, mapY);
                } while (wallType == 0 && ++guard < 256);

                if (wallType <= 0)
                {
                    wallType = 1;
                }

                // 垂直距离（不是欧氏距离），顺便消掉鱼眼。
                double perpendicular = side == 0
                    ? (mapX - position.x + (1 - stepX) / 2.0) / rayDirX
                    : (mapY - position.y + (1 - stepY) / 2.0) / rayDirY;
                if (perpendicular < 1e-4)
                {
                    perpendicular = 1e-4;
                }

                _zBuffer[x] = perpendicular;

                int lineHeight = (int)(_projDistance / perpendicular);
                if (lineHeight < 1)
                {
                    lineHeight = 1;
                }

                int drawStart = _halfHeight - lineHeight / 2;
                int drawEnd = _halfHeight + lineHeight / 2;

                // 命中点在墙面上的横向位置 → 纹理列。
                double wallX = side == 0
                    ? position.y + perpendicular * rayDirY
                    : position.x + perpendicular * rayDirX;
                wallX -= Math.Floor(wallX);

                int texX = (int)(wallX * TextureSize);
                if (texX < 0)
                {
                    texX = 0;
                }
                else if (texX >= TextureSize)
                {
                    texX = TextureSize - 1;
                }

                if ((side == 0 && rayDirX > 0) || (side == 1 && rayDirY < 0))
                {
                    texX = TextureSize - texX - 1;
                }

                Color32[] texture = _wallTextures[(wallType - 1) & 3];
                int light = LightFor(perpendicular);
                if (side == 1)
                {
                    light = light * 3 / 4;
                }

                double textureStep = (double)TextureSize / lineHeight;

                int from = Math.Max(drawStart, 0);
                int to = Math.Min(drawEnd, height - 1);

                // 从「裁剪后的首行」开始采样：贴近墙时 drawStart 为负（墙比屏幕高），
                // 必须按 from 与 drawStart 的差算出纵向起点，否则贴图从第 0 行开始铺，
                // 横缝会随距离弯折。
                double texturePos = (from - drawStart) * textureStep;

                for (int y = from; y <= to; y++)
                {
                    int texY = (int)texturePos & TextureMask;
                    texturePos += textureStep;
                    _buffer[y * width + x] = Shade(texture[texY * TextureSize + texX], light);
                }
            }
        }

        private void RenderSprites(DoomDemoMap map, Vector2 position, double dirX, double dirY,
            double planeX, double planeY)
        {
            _spriteOrder.Clear();
            _spriteOrder.AddRange(map.Sprites);
            // 从远到近，配合 z-buffer 做逐列遮挡。
            _spriteOrder.Sort((a, b) =>
            {
                float distanceA = (a.Position - position).sqrMagnitude;
                float distanceB = (b.Position - position).sqrMagnitude;
                return distanceB.CompareTo(distanceA);
            });

            foreach (DoomSprite sprite in _spriteOrder)
            {
                double spriteX = sprite.Position.x - position.x;
                double spriteY = sprite.Position.y - position.y;

                double transformX = dirY * spriteX - dirX * spriteY;
                double depth = dirX * spriteX + dirY * spriteY;
                if (depth < 0.15)
                {
                    continue;
                }

                int screenX = (int)(_width * 0.5 * (1 + transformX / depth));
                int spriteHeight = (int)(_projDistance * sprite.WorldHeight / depth);
                if (spriteHeight < 1)
                {
                    continue;
                }

                int spriteWidth = spriteHeight;
                int verticalShift = (int)(-_projDistance * sprite.VerticalOffset / depth);

                int startY = _halfHeight - spriteHeight / 2 + verticalShift;
                int endY = _halfHeight + spriteHeight / 2 + verticalShift;
                int startX = screenX - spriteWidth / 2;
                int endX = screenX + spriteWidth / 2;

                Color32[] texture = sprite.Kind == DoomSpriteKind.Demon ? _demonTexture : _barrelTexture;
                int light = LightFor(depth);

                int fromX = Math.Max(startX, 0);
                int toX = Math.Min(endX, _width - 1);
                int fromY = Math.Max(startY, 0);
                int toY = Math.Min(endY, _height - 1);

                for (int x = fromX; x <= toX; x++)
                {
                    // 这一列后面已经有更近的墙，跳过。
                    if (depth >= _zBuffer[x])
                    {
                        continue;
                    }

                    int texX = (x - startX) * TextureSize / spriteWidth;
                    if (texX < 0 || texX >= TextureSize)
                    {
                        continue;
                    }

                    for (int y = fromY; y <= toY; y++)
                    {
                        int texY = (y - startY) * TextureSize / spriteHeight;
                        if (texY < 0 || texY >= TextureSize)
                        {
                            continue;
                        }

                        Color32 pixel = texture[texY * TextureSize + texX];
                        if (pixel.a < 128)
                        {
                            continue;
                        }

                        _buffer[y * _width + x] = Shade(pixel, light);
                    }
                }
            }
        }

        /// <summary>把俯视地图画到画面左上角，方便确认「世界其实只有两层」。</summary>
        public void DrawMinimap(DoomDemoMap map, Vector2 position, float yawDegrees)
        {
            const int scale = 5;
            const int originX = 6;
            const int originY = 6;
            int mapPixels = DoomDemoMap.Width * scale;

            FillRect(originX - 3, originY - 3, mapPixels + 6, mapPixels + 6, new Color32(8, 9, 13, 255));

            for (int mapY = 0; mapY < DoomDemoMap.Height; mapY++)
            {
                for (int mapX = 0; mapX < DoomDemoMap.Width; mapX++)
                {
                    int cell = map.CellAt(mapX, mapY);
                    Color32 color = cell == 0
                        ? new Color32(30, 33, 40, 255)
                        : MinimapWallColor(cell);

                    int pixelX = originX + mapX * scale;
                    int pixelY = originY + (DoomDemoMap.Height - 1 - mapY) * scale;
                    FillRect(pixelX, pixelY, scale - 1, scale - 1, color);
                }
            }

            foreach (DoomSprite sprite in map.Sprites)
            {
                int spriteX = originX + (int)(sprite.Position.x * scale);
                int spriteY = originY + (int)((DoomDemoMap.Height - sprite.Position.y) * scale);
                FillRect(spriteX - 1, spriteY - 1, 3, 3, new Color32(220, 60, 50, 255));
            }

            int playerX = originX + (int)(position.x * scale);
            int playerY = originY + (int)((DoomDemoMap.Height - position.y) * scale);
            FillRect(playerX - 1, playerY - 1, 3, 3, new Color32(255, 214, 64, 255));

            double radians = yawDegrees * Math.PI / 180.0;
            for (int step = 2; step <= 7; step++)
            {
                int tipX = playerX + (int)Math.Round(Math.Cos(radians) * step);
                int tipY = playerY - (int)Math.Round(Math.Sin(radians) * step);
                SetPixel(tipX, tipY, new Color32(255, 214, 64, 255));
            }
        }

        private static Color32 MinimapWallColor(int cell)
        {
            switch (cell)
            {
                case 1: return new Color32(150, 74, 62, 255);
                case 2: return new Color32(120, 124, 132, 255);
                case 3: return new Color32(140, 96, 54, 255);
                default: return new Color32(90, 150, 190, 255);
            }
        }

        private void FillRect(int x, int y, int width, int height, Color32 color)
        {
            int fromX = Math.Max(x, 0);
            int toX = Math.Min(x + width - 1, _width - 1);
            int fromY = Math.Max(y, 0);
            int toY = Math.Min(y + height - 1, _height - 1);

            for (int py = fromY; py <= toY; py++)
            {
                int rowOffset = py * _width;
                for (int px = fromX; px <= toX; px++)
                {
                    _buffer[rowOffset + px] = color;
                }
            }
        }

        private void SetPixel(int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height)
            {
                return;
            }

            _buffer[y * _width + x] = color;
        }

        /// <summary>按距离衰减的亮度，返回 0~256。</summary>
        private static int LightFor(double distance)
        {
            double value = 1.0 - distance * 0.055;
            if (value < 0.18)
            {
                value = 0.18;
            }

            return (int)(value * 256);
        }

        private static Color32 Shade(Color32 color, int light)
        {
            if (light >= 256)
            {
                return color;
            }

            return new Color32(
                (byte)((color.r * light) >> 8),
                (byte)((color.g * light) >> 8),
                (byte)((color.b * light) >> 8),
                color.a);
        }

        // ---------------------------------------------------------------- 贴图

        private static Color32[] CreateBrickTexture()
        {
            Color32[] pixels = new Color32[TextureSize * TextureSize];
            System.Random random = new System.Random(11);

            for (int y = 0; y < TextureSize; y++)
            {
                int row = y / 16;
                int shift = (row & 1) * 16;
                for (int x = 0; x < TextureSize; x++)
                {
                    int localX = (x + shift) % 32;
                    int localY = y % 16;

                    Color32 color;
                    if (localY < 2 || localX < 2)
                    {
                        color = new Color32(46, 42, 40, 255);
                    }
                    else
                    {
                        int brick = row * 4 + (x + shift) / 32;
                        int tint = brick * 37 % 21 - 10;
                        color = new Color32(
                            (byte)Mathf.Clamp(134 + tint + random.Next(-7, 8), 0, 255),
                            (byte)Mathf.Clamp(62 + tint + random.Next(-7, 8), 0, 255),
                            (byte)Mathf.Clamp(48 + tint + random.Next(-7, 8), 0, 255),
                            255);
                    }

                    pixels[y * TextureSize + x] = color;
                }
            }

            return pixels;
        }

        private static Color32[] CreateStoneTexture()
        {
            Color32[] pixels = new Color32[TextureSize * TextureSize];
            System.Random random = new System.Random(23);

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    int block = (x / 16 + y / 16 * 4) * 13;
                    int baseValue = 96 + block % 17 - 8;
                    int value = baseValue + random.Next(-14, 15);
                    if (x % 16 < 1 || y % 16 < 1)
                    {
                        value -= 44;
                    }

                    byte level = (byte)Mathf.Clamp(value, 0, 255);
                    pixels[y * TextureSize + x] = new Color32(
                        level,
                        (byte)Mathf.Clamp(level - 4, 0, 255),
                        (byte)Mathf.Clamp(level + 6, 0, 255),
                        255);
                }
            }

            return pixels;
        }

        private static Color32[] CreateWoodTexture()
        {
            Color32[] pixels = new Color32[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    int plank = x / 16;
                    float grain = Mathf.Sin(x * 0.9f + plank * 5f) * 7f + Mathf.Sin(y * 0.32f + plank) * 5f;
                    int seam = x % 16 < 1 || y % 32 < 1 ? -46 : 0;
                    int value = (int)(112 + grain) + seam;

                    pixels[y * TextureSize + x] = new Color32(
                        (byte)Mathf.Clamp(value, 0, 255),
                        (byte)Mathf.Clamp((int)(value * 0.66f), 0, 255),
                        (byte)Mathf.Clamp((int)(value * 0.40f), 0, 255),
                        255);
                }
            }

            return pixels;
        }

        private static Color32[] CreateTechTexture()
        {
            Color32[] pixels = new Color32[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    int localX = x % 32;
                    int localY = y % 32;
                    bool border = localX < 2 || localY < 2;
                    int value = border ? 42 : 78;

                    // 竖向凹槽
                    if (!border && localX >= 14 && localX < 18)
                    {
                        value -= 20;
                    }

                    // 四角铆钉
                    int rivetX = localX < 8 ? 5 : 27;
                    int rivetY = localY < 8 ? 5 : 27;
                    int dx = localX - rivetX;
                    int dy = localY - rivetY;
                    if (!border && dx * dx + dy * dy <= 4)
                    {
                        value += 52;
                    }

                    pixels[y * TextureSize + x] = new Color32(
                        (byte)Mathf.Clamp(value - 6, 0, 255),
                        (byte)Mathf.Clamp(value, 0, 255),
                        (byte)Mathf.Clamp(value + 10, 0, 255),
                        255);
                }
            }

            return pixels;
        }

        private static Color32[] CreateFloorTexture()
        {
            Color32[] pixels = new Color32[TextureSize * TextureSize];
            System.Random random = new System.Random(37);

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    int localX = x % 32;
                    int localY = y % 32;
                    bool grout = localX < 2 || localY < 2;
                    int value = grout ? 30 : 62 + (x / 32 + y / 32) % 2 * 10;

                    if (!grout && random.Next(0, 12) == 0)
                    {
                        value += 18;
                    }

                    byte level = (byte)Mathf.Clamp(value + random.Next(-6, 7), 0, 255);
                    pixels[y * TextureSize + x] = new Color32(
                        (byte)Mathf.Clamp(level - 4, 0, 255),
                        level,
                        (byte)Mathf.Clamp(level + 8, 0, 255),
                        255);
                }
            }

            return pixels;
        }

        private static Color32[] CreateCeilingTexture()
        {
            Color32[] pixels = new Color32[TextureSize * TextureSize];
            System.Random random = new System.Random(53);

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    int localX = x % 16;
                    int localY = y % 16;
                    bool seam = localX < 1 || localY < 1;
                    int value = seam ? 18 : 44 + random.Next(-5, 6);

                    byte level = (byte)Mathf.Clamp(value, 0, 255);
                    pixels[y * TextureSize + x] = new Color32(
                        (byte)Mathf.Clamp(level - 2, 0, 255),
                        level,
                        (byte)Mathf.Clamp(level + 12, 0, 255),
                        255);
                }
            }

            return pixels;
        }

        private static Color32[] CreateDemonTexture()
        {
            Color32[] pixels = new Color32[TextureSize * TextureSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            const float centerX = 32f;
            const float centerY = 36f;
            const float radiusX = 23f;
            const float radiusY = 25f;

            // 球体本身：法线近似出来的明暗 + 偏红的肉色。
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float nx = (x - centerX) / radiusX;
                    float ny = (y - centerY) / radiusY;
                    float squared = nx * nx + ny * ny;
                    if (squared > 1f)
                    {
                        continue;
                    }

                    float nz = Mathf.Sqrt(1f - squared);
                    float light = 0.42f + 0.62f * (nz * 0.55f - nx * 0.42f - ny * 0.30f);
                    light = Mathf.Clamp(light, 0.18f, 1.15f);

                    pixels[y * TextureSize + x] = new Color32(
                        (byte)Mathf.Clamp(152f * light, 0, 255),
                        (byte)Mathf.Clamp(38f * light, 0, 255),
                        (byte)Mathf.Clamp(32f * light, 0, 255),
                        255);
                }
            }

            // 两只眼睛。
            DrawDisc(pixels, 24, 28, 7.5f, new Color32(240, 232, 190, 255));
            DrawDisc(pixels, 40, 28, 7.5f, new Color32(240, 232, 190, 255));
            DrawDisc(pixels, 25, 29, 3.6f, new Color32(18, 8, 6, 255));
            DrawDisc(pixels, 39, 29, 3.6f, new Color32(18, 8, 6, 255));

            // 嘴。
            for (int y = 43; y <= 55; y++)
            {
                for (int x = 17; x <= 47; x++)
                {
                    float mx = (x - 32f) / 14.5f;
                    float my = (y - 49f) / 6f;
                    if (mx * mx + my * my > 1f)
                    {
                        continue;
                    }

                    pixels[y * TextureSize + x] = new Color32(28, 9, 9, 255);
                }
            }

            // 牙。
            for (int tooth = 0; tooth < 7; tooth++)
            {
                int toothX = 21 + tooth * 4;
                for (int y = 43; y <= 47; y++)
                {
                    for (int x = toothX; x < toothX + 2; x++)
                    {
                        if (x < 0 || x >= TextureSize)
                        {
                            continue;
                        }

                        pixels[y * TextureSize + x] = new Color32(228, 220, 198, 255);
                    }
                }
            }

            return pixels;
        }

        private static Color32[] CreateBarrelTexture()
        {
            Color32[] pixels = new Color32[TextureSize * TextureSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            for (int y = 6; y < TextureSize - 1; y++)
            {
                for (int x = 12; x < TextureSize - 12; x++)
                {
                    float dx = (x - 31.5f) / 20f;
                    if (Mathf.Abs(dx) > 1f)
                    {
                        continue;
                    }

                    float nz = Mathf.Sqrt(1f - dx * dx);
                    float light = 0.35f + 0.80f * nz - dx * 0.25f;
                    light = Mathf.Clamp(light, 0.2f, 1.2f);

                    float baseValue = 96f;
                    if (y < 10 || y > 55)
                    {
                        baseValue = 120f;
                    }
                    else if (y >= 18 && y <= 23 || y >= 42 && y <= 47)
                    {
                        baseValue = 68f;
                    }

                    pixels[y * TextureSize + x] = new Color32(
                        (byte)Mathf.Clamp(baseValue * light * 0.72f, 0, 255),
                        (byte)Mathf.Clamp(baseValue * light, 0, 255),
                        (byte)Mathf.Clamp(baseValue * light * 0.62f, 0, 255),
                        255);
                }
            }

            return pixels;
        }

        private static void DrawDisc(Color32[] pixels, int centerX, int centerY, float radius, Color32 color)
        {
            int fromX = Mathf.Max(0, (int)(centerX - radius));
            int toX = Mathf.Min(TextureSize - 1, (int)(centerX + radius));
            int fromY = Mathf.Max(0, (int)(centerY - radius));
            int toY = Mathf.Min(TextureSize - 1, (int)(centerY + radius));
            float squaredRadius = radius * radius;

            for (int y = fromY; y <= toY; y++)
            {
                for (int x = fromX; x <= toX; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    if (dx * dx + dy * dy <= squaredRadius)
                    {
                        pixels[y * TextureSize + x] = color;
                    }
                }
            }
        }
    }
}
