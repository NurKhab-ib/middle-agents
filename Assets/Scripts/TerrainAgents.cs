using System.Collections.Generic;
using UnityEngine;

namespace MiddleAgents
{
    public static class TerrainAgents
    {
        public static TerrainMap Generate(int width, int height, int seed)
        {
            var map = new TerrainMap(width, height);
            var random = new System.Random(seed);
            float offsetX = random.Next(-10000, 10000);
            float offsetY = random.Next(-10000, 10000);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float broad = Mathf.PerlinNoise(offsetX + x * 0.008f, offsetY + y * 0.008f);
                float detail = Mathf.PerlinNoise(offsetX + 89f + x * 0.024f, offsetY - 67f + y * 0.024f);
                map.Elevation[map.Index(x, y)] = 0.23f + broad * 0.23f + detail * 0.07f;
            }

            // ridge agents draw long mountain chains; each step can turn a bit
            for (int i = 0; i < 10; i++)
            {
                Vector2 position = RandomPoint(random, width, height, 20);
                float angle = NextFloat(random, 0f, Mathf.PI * 2f);
                int steps = random.Next(65, 120);
                float peak = NextFloat(random, 0.85f, 1.2f);
                for (int step = 0; step < steps; step++)
                {
                    float radius = NextFloat(random, 8f, 14f);
                    Stamp(map, position, radius, (0.025f + 0.055f * Mathf.Sin(Mathf.PI * step / steps)) * peak);
                    angle += NextFloat(random, -0.24f, 0.24f);
                    position += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.35f;
                    if (!InsideMargin(map, position, 8f)) break;
                }
            }

            // hill agents wander and leave softer, wider bumps
            for (int i = 0; i < 30; i++)
            {
                Vector2 position = RandomPoint(random, width, height, 10);
                float angle = NextFloat(random, 0f, Mathf.PI * 2f);
                for (int step = 0; step < 34; step++)
                {
                    Stamp(map, position, NextFloat(random, 5f, 10f), 0.035f);
                    angle += NextFloat(random, -0.38f, 0.38f);
                    position += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.5f;
                    if (!InsideMargin(map, position, 4f)) break;
                }
            }

            // valley agents cut green passes through the rough ground
            for (int i = 0; i < 18; i++)
            {
                Vector2 position = RandomPoint(random, width, height, 8);
                float angle = NextFloat(random, 0f, Mathf.PI * 2f);
                for (int step = 0; step < 65; step++)
                {
                    Stamp(map, position, NextFloat(random, 8f, 14f), -0.055f);
                    angle += NextFloat(random, -0.22f, 0.22f);
                    position += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.2f;
                    if (!InsideMargin(map, position, 3f)) break;
                }
            }

            // lake agents stay near one low spot and grow uneven shore lines
            var lakeCenters = new List<Vector2>();
            int lakeCount = random.Next(6, 11);
            for (int i = 0; i < lakeCount; i++)
            {
                if (!TryFindLakeCenter(map, random, lakeCenters, out Vector2 center)) continue;
                lakeCenters.Add(center);
                GrowLake(map, random, center);
            }

            for (int i = 0; i < random.Next(16, 23); i++)
                GrowForest(map, random, true);
            for (int i = 0; i < random.Next(20, 30); i++)
                GrowForest(map, random, false);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int index = map.Index(x, y);
                float elevation = map.Elevation[index];
                map.Biomes[index] = map.Lake[index] > 0.1f ? Biome.Lake
                    : elevation >= TerrainMap.SnowLevel ? Biome.SnowMountain
                    : elevation >= TerrainMap.MountainLevel ? Biome.Mountain
                    : map.Forest[index] > 0.43f ? Biome.DenseForest
                    : map.Forest[index] > 0.15f ? Biome.SparseForest
                    : elevation >= TerrainMap.HillLevel ? Biome.Hill
                    : Biome.Valley;
            }

            return map;
        }

        private static bool TryFindLakeCenter(TerrainMap map, System.Random random,
            List<Vector2> centers, out Vector2 center)
        {
            for (int attempt = 0; attempt < 500; attempt++)
            {
                Vector2 point = RandomPoint(random, map.Width, map.Height, 20);
                float elevation = map.HeightAt((int)point.x, (int)point.y);
                if (elevation >= 0.39f) continue;
                bool close = false;
                foreach (Vector2 oldCenter in centers)
                {
                    if (Vector2.Distance(point, oldCenter) < 35f)
                    {
                        close = true;
                        break;
                    }
                }
                if (close) continue;
                center = point;
                return true;
            }
            center = Vector2.zero;
            return false;
        }

        private static void GrowLake(TerrainMap map, System.Random random, Vector2 center)
        {
            float coreRadius = NextFloat(random, 8f, 14f);
            float noiseX = NextFloat(random, -9000f, 9000f);
            float noiseY = NextFloat(random, -9000f, 9000f);
            PaintLake(map, center, coreRadius, noiseX, noiseY);

            int lobes = random.Next(4, 8);
            for (int lobe = 0; lobe < lobes; lobe++)
            {
                Vector2 position = center;
                float angle = NextFloat(random, 0f, Mathf.PI * 2f);
                int steps = random.Next(6, 14);
                for (int step = 0; step < steps; step++)
                {
                    angle += (Mathf.PerlinNoise(noiseX + lobe * 5f + step * 0.12f, noiseY) - 0.5f) * 0.25f;
                    position += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.9f;
                    if (Vector2.Distance(position, center) > coreRadius * 1.15f) break;
                    PaintLake(map, position, coreRadius * NextFloat(random, 0.42f, 0.66f), noiseX, noiseY);
                }
            }
        }

        private static void PaintLake(TerrainMap map, Vector2 center, float radius, float noiseX, float noiseY)
        {
            int extent = Mathf.CeilToInt(radius * 1.2f);
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x) - extent);
            int maxX = Mathf.Min(map.Width - 1, Mathf.CeilToInt(center.x) + extent);
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y) - extent);
            int maxY = Mathf.Min(map.Height - 1, Mathf.CeilToInt(center.y) + extent);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                int index = map.Index(x, y);
                if (map.Elevation[index] >= TerrainMap.HillLevel + 0.03f) continue;
                float edge = Mathf.Lerp(0.82f, 1.18f,
                    Mathf.PerlinNoise(noiseX + x * 0.065f, noiseY + y * 0.065f));
                float fill = 1f - Vector2.Distance(center, new Vector2(x, y)) / (radius * edge);
                if (fill > 0f) map.Lake[index] = Mathf.Max(map.Lake[index], fill);
            }
        }

        private static void GrowForest(TerrainMap map, System.Random random, bool dense)
        {
            Vector2 position = Vector2.zero;
            bool found = false;
            for (int attempt = 0; attempt < 100; attempt++)
            {
                position = RandomPoint(random, map.Width, map.Height, 12);
                int index = map.Index((int)position.x, (int)position.y);
                if (map.Elevation[index] >= TerrainMap.MountainLevel || map.Lake[index] > 0.1f) continue;
                found = true;
                break;
            }
            if (!found) return;
            Vector2 center = position;
            int branches = dense ? random.Next(4, 7) : random.Next(3, 5);
            for (int branch = 0; branch < branches; branch++)
            {
                position = center;
                float angle = NextFloat(random, 0f, Mathf.PI * 2f);
                int steps = dense ? random.Next(12, 22) : random.Next(8, 16);
                for (int step = 0; step < steps; step++)
                {
                    float radius = dense ? NextFloat(random, 10f, 16f) : NextFloat(random, 9f, 14f);
                    PaintForest(map, position, radius, dense ? 0.075f : 0.024f);
                    angle += NextFloat(random, -0.25f, 0.25f);
                    position += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.2f;
                    if (!InsideMargin(map, position, 3f)) break;
                }
            }
        }

        private static void PaintForest(TerrainMap map, Vector2 center, float radius, float strength)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            int maxX = Mathf.Min(map.Width - 1, Mathf.CeilToInt(center.x + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            int maxY = Mathf.Min(map.Height - 1, Mathf.CeilToInt(center.y + radius));
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                int index = map.Index(x, y);
                if (map.Elevation[index] >= TerrainMap.MountainLevel || map.Lake[index] > 0.1f) continue;
                float distance = Vector2.Distance(center, new Vector2(x, y)) / radius;
                if (distance >= 1f) continue;
                float falloff = 1f - distance * distance;
                map.Forest[index] = Mathf.Clamp01(map.Forest[index] + strength * falloff * falloff);
            }
        }

        private static void Stamp(TerrainMap map, Vector2 center, float radius, float strength)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            int maxX = Mathf.Min(map.Width - 1, Mathf.CeilToInt(center.x + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            int maxY = Mathf.Min(map.Height - 1, Mathf.CeilToInt(center.y + radius));
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float distance = Vector2.Distance(center, new Vector2(x, y)) / radius;
                if (distance >= 1f) continue;
                float falloff = 1f - distance * distance;
                int index = map.Index(x, y);
                map.Elevation[index] = Mathf.Clamp01(map.Elevation[index] + strength * falloff * falloff);
            }
        }

        private static Vector2 RandomPoint(System.Random random, int width, int height, int margin)
        {
            return new Vector2(random.Next(margin, width - margin), random.Next(margin, height - margin));
        }

        private static bool InsideMargin(TerrainMap map, Vector2 point, float margin)
        {
            return point.x >= margin && point.y >= margin
                && point.x < map.Width - margin && point.y < map.Height - margin;
        }

        private static float NextFloat(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
