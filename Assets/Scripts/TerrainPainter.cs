using UnityEngine;

namespace MiddleAgents
{
    public static class TerrainPainter
    {
        private const int PixelsPerCell = 3;
        private const int PixelsPerUnit = 6;

        public static Sprite Paint(TerrainMap map, int seed, out Texture2D texture)
        {
            int width = map.Width * PixelsPerCell;
            int height = map.Height * PixelsPerCell;
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[width * height];
            var random = new System.Random(seed);
            float offsetX = random.Next(-10000, 10000);
            float offsetY = random.Next(-10000, 10000);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int cellX = x / PixelsPerCell;
                int cellY = y / PixelsPerCell;
                int index = map.Index(cellX, cellY);
                Biome biome = map.Biomes[index];
                float elevation = map.Elevation[index];
                float grain = Mathf.PerlinNoise(offsetX + x * 0.21f, offsetY + y * 0.21f) - 0.5f;
                float patches = Mathf.PerlinNoise(offsetX + 53f + x * 0.045f, offsetY - 37f + y * 0.045f) - 0.5f;
                float slope = map.HeightAt(cellX - 1, cellY + 1) - map.HeightAt(cellX + 1, cellY - 1);
                Color color = BaseColor(biome, elevation, map.Lake[index], map.Forest[index]);
                float light = 1f + grain * 0.13f + patches * 0.17f + slope * 0.4f;

                if (biome == Biome.Valley)
                    light += (0.38f - elevation) * 0.12f;
                else if (biome == Biome.Hill)
                    light += (elevation - 0.5f) * 0.15f;
                else if (biome == Biome.Mountain || biome == Biome.SnowMountain)
                    light += slope * 0.45f;
                else if (biome == Biome.Lake)
                    light += grain * 0.055f;
                else
                    light += patches * 0.07f;

                // tiny perlin marks keep the map pixel-ish without chunky tiles
                if (biome == Biome.Valley && grain > 0.32f && patches > 0.08f)
                    color = Color.Lerp(color, new Color(0.31f, 0.48f, 0.22f), 0.35f);
                if (biome == Biome.SnowMountain && grain < -0.31f)
                    color = Color.Lerp(color, new Color(0.72f, 0.82f, 0.84f), 0.3f);
                if (biome == Biome.Lake && grain > 0.32f)
                    color = Color.Lerp(color, new Color(0.46f, 0.74f, 0.76f), 0.2f);
                if (biome == Biome.SparseForest || biome == Biome.DenseForest)
                {
                    float canopy = Mathf.PerlinNoise(offsetX + 217f + x * 0.3f, offsetY - 193f + y * 0.3f);
                    float threshold = biome == Biome.DenseForest ? 0.47f : 0.62f;
                    if (canopy > threshold)
                        color = Color.Lerp(color, new Color(0.12f, 0.32f, 0.17f),
                            biome == Biome.DenseForest ? 0.7f : 0.48f);
                }

                pixels[y * width + x] = (Color32)(color * Mathf.Clamp(light, 0.75f, 1.23f));
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static Color BaseColor(Biome biome, float elevation, float lakeDepth, float forestDensity)
        {
            switch (biome)
            {
                case Biome.Lake:
                    return Color.Lerp(new Color(0.36f, 0.63f, 0.65f), new Color(0.13f, 0.38f, 0.53f),
                        Mathf.Clamp01(lakeDepth * 1.8f));
                case Biome.SparseForest:
                    return Color.Lerp(new Color(0.34f, 0.61f, 0.31f), new Color(0.27f, 0.48f, 0.27f),
                        Mathf.Clamp01(forestDensity));
                case Biome.DenseForest: return new Color(0.18f, 0.42f, 0.23f);
                case Biome.Hill:
                    return Color.Lerp(new Color(0.34f, 0.62f, 0.32f), new Color(0.44f, 0.53f, 0.37f),
                        Mathf.SmoothStep(0f, 1f, (elevation - TerrainMap.HillLevel) /
                            (TerrainMap.MountainLevel - TerrainMap.HillLevel)));
                case Biome.Mountain:
                    return Color.Lerp(new Color(0.44f, 0.53f, 0.37f), new Color(0.72f, 0.77f, 0.74f),
                        Mathf.SmoothStep(0f, 1f, (elevation - TerrainMap.MountainLevel) /
                            (TerrainMap.SnowLevel - TerrainMap.MountainLevel)));
                case Biome.SnowMountain:
                    return Color.Lerp(new Color(0.72f, 0.77f, 0.74f), new Color(0.91f, 0.94f, 0.91f),
                        Mathf.SmoothStep(0f, 1f, (elevation - TerrainMap.SnowLevel) / 0.13f));
                default: return new Color(0.34f, 0.62f, 0.32f);
            }
        }
    }
}
