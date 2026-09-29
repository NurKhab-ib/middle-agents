using System;
using System.IO;
using UnityEngine;

namespace MiddleAgents.Editor {
    public static class TerrainGenerationValidation {
        public static void Preview() {
            // make one fixed map so the preview can be compared later
            TerrainMap map = TerrainAgents.Generate(384, 256, 81342);
            Sprite sprite = TerrainPainter.Paint(map, 81342, out Texture2D texture);

            // save only a png; the runtime sprite and texture can be released
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "terrain-preview.png"));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(texture);
            Debug.Log("Terrain preview saved to " + path);
        }

        public static void Run() {
            // fixed seeds cover several map layouts in one quick check
            int[] seeds = { 1, 42, 81342, 1234567, -89434, int.MaxValue, 2026, 900000 };
            foreach (int seed in seeds) {
                TerrainMap map = TerrainAgents.Generate(384, 256, seed);
                var counts = new int[Enum.GetValues(typeof(Biome)).Length];

                // count every biome and make sure lakes stay on low ground
                for (int index = 0; index < map.Biomes.Length; index++) {
                    Biome biome = map.Biomes[index];
                    counts[(int)biome]++;
                    if (biome == Biome.Lake && map.Elevation[index] >= TerrainMap.HillLevel + 0.03f) {
                        throw new Exception("Lake reached high ground for seed " + seed);
                    }
                }

                // every expected terrain type should show up for each seed
                foreach (Biome biome in Enum.GetValues(typeof(Biome))) {
                    if (counts[(int)biome] == 0) {
                        throw new Exception("Missing " + biome + " for seed " + seed);
                    }
                }
                Debug.Log("Terrain seed " + seed + " passed: " + string.Join(", ", counts));
            }
        }
    }
}
