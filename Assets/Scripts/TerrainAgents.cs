using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static class TerrainAgents {
    public static TerrainMap Generate(int width, int height, int seed) {
        var map = new TerrainMap(width, height);
        var random = new Random(seed);

        // the seed moves both noise samples to a fresh part of the noise plane
        float offsetX = random.Next(-10000, 10000);
        float offsetY = random.Next(-10000, 10000);

        // broad noise sets the main ground level; detail noise adds small changes
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float broad = Mathf.PerlinNoise(offsetX + x * 0.008f, offsetY + y * 0.008f);
                float detail = Mathf.PerlinNoise(offsetX + 89f + x * 0.024f, offsetY - 67f + y * 0.024f);
                map.Elevation[map.Index(x, y)] = 0.23f + broad * 0.23f + detail * 0.07f;
            }
        }

        // ridge agents leave tall stamps as they walk, so peaks form chains
        for (int i = 0; i < 10; i++) {
            Vector2 position = GetRandPoint(random, width, height, 20);
            float angle = GetNextF(random, 0f, Mathf.PI * 2f);
            int steps = random.Next(65, 120);
            float peak = GetNextF(random, 0.85f, 1.2f);
            for (int step = 0; step < steps; step++) {
                float radius = GetNextF(random, 8f, 14f);
                float strength = (0.025f + 0.055f * Mathf.Sin(Mathf.PI * step / steps)) * peak;
                Stamp(map, position, radius, strength);

                // small turns make the ridge bend; the margin keeps it on the map
                angle += GetNextF(random, -0.24f, 0.24f);
                position += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.35f;
                if (!IsInsideMargin(map, position, 8f)) {
                    break;
                }
            }
        }

        // hill agents use weaker stamps, so they build low rolling ground
        for (int i = 0; i < 30; i++) {
            var pos = GetRandPoint(random, width, height, 10);
            float angle = GetNextF(random, 0f, Mathf.PI * 2f);
            
            for (int step = 0; step < 34; step++) {
                Stamp(map, pos, GetNextF(random, 5f, 10f), 0.035f);
                angle += GetNextF(random, -0.38f, 0.38f);
                pos += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.5f;
                
                if (!IsInsideMargin(map, pos, 4f)) {
                    break;
                }
            }
        }

        // valley agents lower height along a path between the higher areas
        for (int i = 0; i < 18; i++) {
            Vector2 position = GetRandPoint(random, width, height, 8);
            float angle = GetNextF(random, 0f, Mathf.PI * 2f);
            
            for (int step = 0; step < 65; step++) {
                Stamp(map, position, GetNextF(random, 8f, 14f), -0.055f);
                angle += GetNextF(random, -0.22f, 0.22f);
                position += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.2f;
                if (!IsInsideMargin(map, position, 3f)) {
                    break;
                }
            }
        }

        // lake centers must be low and far apart; each center grows one lake
        var lakeCenters = new List<Vector2>();
        int lakeCount = random.Next(6, 11);
        
        for (int i = 0; i < lakeCount; i++) {
            if (!TryFindLakeCenter(map, random, lakeCenters, out Vector2 center)) {
                continue;
            }

            lakeCenters.Add(center);
            GrowLake(map, random, center);
        }

        // dense and sparse agents paint separate forest density patches
        for (int i = 0; i < random.Next(16, 23); i++) {
            GrowForest(map, random, true);
        }

        for (int i = 0; i < random.Next(20, 30); i++) {
            GrowForest(map, random, false);
        }

        // biome order matters: water and high rock win over forest density
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
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
        }

        return map;
    }

    private static bool TryFindLakeCenter(TerrainMap map, Random random, List<Vector2> centers, out Vector2 center) {
        
        // try random low cells, then reject spots too close to an older lake
        for (int attempt = 0; attempt < 500; attempt++) {
            Vector2 point = GetRandPoint(random, map.Width, map.Height, 20);
            float elevation = map.HeightAt((int)point.x, (int)point.y);
            if (elevation >= 0.39f) {
                continue;
            }

            bool close = false;
            
            foreach (Vector2 oldCenter in centers) {
                if (Vector2.Distance(point, oldCenter) < 35f) {
                    close = true;
                    
                    break;
                }
            }

            if (close) {
                continue;
            }

            center = point;
            return true;
        }

        // a map may have no free low spot left for this agent
        center = Vector2.zero;
        
        return false;
    }

    private static void GrowLake(TerrainMap map, Random random, Vector2 center) {
        // the 1st stamp fills the middle, so short walks stay one lake
        float coreRadius = GetNextF(random, 8f, 14f);
        float noiseX = GetNextF(random, -9000f, 9000f);
        float noiseY = GetNextF(random, -9000f, 9000f);
        
        PaintLake(map, center, coreRadius, noiseX, noiseY);

        int lobes = random.Next(4, 8);
        
        for (int lobe = 0; lobe < lobes; lobe++) {
            // each lobe starts at the same center and grows in a new direction
            Vector2 pos = center;
            float angle = GetNextF(random, 0f, Mathf.PI * 2f);
            int steps = random.Next(6, 14);
            
            for (int step = 0; step < steps; step++) {
                // noise bends the shore a bit, while the radius keeps it compact
                angle += (Mathf.PerlinNoise(noiseX + lobe * 5f + step * 0.12f, noiseY) - 0.5f) * 0.25f;
                pos += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.9f;
                
                if (Vector2.Distance(pos, center) > coreRadius * 1.15f) {
                    break;
                }

                PaintLake(map, pos, coreRadius * GetNextF(random, 0.42f, 0.66f), noiseX, noiseY);
            }
        }
    }

    private static void PaintLake(TerrainMap map, Vector2 center, float radius, float noiseX, float noiseY) {
        // visit only the cells near this stamp (not all the map)
        var extent = Mathf.CeilToInt(radius * 1.2f);
        var minX = Mathf.Max(0, Mathf.FloorToInt(center.x) - extent);
        var maxX = Mathf.Min(map.Width - 1, Mathf.CeilToInt(center.x) + extent);
        var minY = Mathf.Max(0, Mathf.FloorToInt(center.y) - extent);
        var maxY = Mathf.Min(map.Height - 1, Mathf.CeilToInt(center.y) + extent);
        
        for (int y = minY; y <= maxY; y++) {
            for (int x = minX; x <= maxX; x++) {
                int index = map.Index(x, y);
                if (map.Elevation[index] >= TerrainMap.HillLevel + 0.03f) {
                    continue;
                }

                // small noise change makes the round stamp edge less even
                float edge = Mathf.Lerp(0.82f, 1.18f, Mathf.PerlinNoise(noiseX + x * 0.065f, noiseY + y * 0.065f));
                float fill = 1f - Vector2.Distance(center, new Vector2(x, y)) / (radius * edge);
                
                if (fill > 0f) {
                    map.Lake[index] = Mathf.Max(map.Lake[index], fill);
                }
            }
        }
    }

    private static void GrowForest(TerrainMap map, Random random, bool dense) {
        // begin on open low ground, away from water and high mountains
        var pos = Vector2.zero;
        bool found = false;
        
        for (int attempt = 0; attempt < 100; attempt++) {
            pos = GetRandPoint(random, map.Width, map.Height, 12);
            int index = map.Index((int)pos.x, (int)pos.y);
            
            if (map.Elevation[index] >= TerrainMap.MountainLevel || map.Lake[index] > 0.1f) {
                continue;
            }

            found = true;
            break;
        }

        if (!found) {
            return;
        }

        // many short branches make a wide patch instead of one thin stripe
        var center = pos;
        int branches = dense ? random.Next(4, 7) : random.Next(3, 5);
        
        for (int b = 0; b < branches; b++) {
            pos = center;
            float angle = GetNextF(random, 0f, Mathf.PI * 2f);
            var steps = dense ? random.Next(12, 22) : random.Next(8, 16);
            
            for (int step = 0; step < steps; step++) {
                var radius = dense ? GetNextF(random, 10f, 16f) : GetNextF(random, 9f, 14f);
                
                // dense agents add more value to the forest layer on each step
                PaintForest(map, pos, radius, dense ? 0.075f : 0.024f);
                angle += GetNextF(random, -0.25f, 0.25f);
                pos += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.2f;
                
                if (!IsInsideMargin(map, pos, 3f)) {
                    break;
                }
            }
        }
    }

    private static void PaintForest(TerrainMap map, Vector2 center, float radius, float strength) {
        // each brush adds density, but it never grows on water or high rock
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int maxX = Mathf.Min(map.Width - 1, Mathf.CeilToInt(center.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int maxY = Mathf.Min(map.Height - 1, Mathf.CeilToInt(center.y + radius));
        
        for (int y = minY; y <= maxY; y++) {
            for (int x = minX; x <= maxX; x++) {
                int index = map.Index(x, y);
                if (map.Elevation[index] >= TerrainMap.MountainLevel || map.Lake[index] > 0.1f) {
                    continue;
                }

                float distance = Vector2.Distance(center, new Vector2(x, y)) / radius;
                if (distance >= 1f) {
                    continue;
                }

                float falloff = 1f - distance * distance;
                map.Forest[index] = Mathf.Clamp01(map.Forest[index] + strength * falloff * falloff);
            }
        }
    }

    private static void Stamp(TerrainMap map, Vector2 center, float radius, float strength) {
        // positive strength raises land; negative strength cuts a valley
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int maxX = Mathf.Min(map.Width - 1, Mathf.CeilToInt(center.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int maxY = Mathf.Min(map.Height - 1, Mathf.CeilToInt(center.y + radius));
        
        for (int y = minY; y <= maxY; y++) {
            for (int x = minX; x <= maxX; x++) {
                float distance = Vector2.Distance(center, new Vector2(x, y)) / radius;
                
                if (distance >= 1f) {
                    continue;
                }

                // the squared falloff keeps the stamp edge soft
                float falloff = 1f - distance * distance;
                int i = map.Index(x, y);
                
                map.Elevation[i] = Mathf.Clamp01(map.Elevation[i] + strength * falloff * falloff);
            }
        }
    }

    private static Vector2 GetRandPoint(Random random, int width, int height, int margin) {
        // keep new agents away from the outer border
        return new Vector2(
            random.Next(margin, width - margin),
            random.Next(margin, height - margin)
            );
    }

    private static bool IsInsideMargin(TerrainMap map, Vector2 point, float margin) {
        return point.x >= margin && point.y >= margin
            && point.x < map.Width - margin && point.y < map.Height - margin;
    }

    private static float GetNextF(Random random, float min, float max) {
        return min + (float)random.NextDouble() * (max - min);
    }
}
