using UnityEngine;
using Random = System.Random;

public static class TerrainPainter {
    // a few texture pixels per cell keep the view pixel-like but not blocky
    private const int PixelsPerCell = 3;
    private const int PixelsPerUnit = 6;

    public static Sprite Paint(TerrainMap map, int seed, out Texture2D texture) {
        int width = map.Width * PixelsPerCell;
        int height = map.Height * PixelsPerCell;

        // point filtering keeps each pixel sharp when the camera zooms
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[width * height];
        var random = new Random(seed);
        float offsetX = random.Next(-10000, 10000);
        float offsetY = random.Next(-10000, 10000);

        // each terrain cell owns a small group of image pixels
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int cellX = x / PixelsPerCell;
                int cellY = y / PixelsPerCell;
                int index = map.Index(cellX, cellY);
                var biome = map.Biomes[index];
                float elevation = map.Elevation[index];

                // grain is detail, changes color on wider areas
                float grain = Mathf.PerlinNoise(offsetX + x * 0.21f, offsetY + y * 0.21f) - 0.5f;
                float patches = Mathf.PerlinNoise(offsetX + 53f + x * 0.045f, offsetY - 37f + y * 0.045f) - 0.5f;
                
                // height dif makes one side of slopes look lighter
                float slope = map.HeightAt(cellX - 1, cellY + 1) - map.HeightAt(cellX + 1, cellY - 1);
                var color = BaseColor(biome, elevation, map.Lake[index], map.Forest[index]);
                float light = 1f + grain * 0.13f + patches * 0.17f + slope * 0.4f;

                switch (biome) {
                    // each biome gets a small light change that fits its surface
                    case Biome.Valley:
                        light += (0.38f - elevation) * 0.12f;
                        break;
                    case Biome.Hill:
                        light += (elevation - 0.5f) * 0.15f;
                        break;
                    case Biome.Mountain:
                    case Biome.SnowMountain:
                        light += slope * 0.45f;
                        break;
                    case Biome.Lake:
                        light += grain * 0.055f;
                        break;
                    default:
                        light += patches * 0.07f;
                        break;
                }

                // a few darker or lighter pixels add grass, snow, and water detail
                if (biome == Biome.Valley && grain > 0.32f && patches > 0.08f) {
                    color = Color.Lerp(color, new Color(0.31f, 0.48f, 0.22f), 0.35f);
                }

                if (biome == Biome.SnowMountain && grain < -0.31f) {
                    color = Color.Lerp(color, new Color(0.72f, 0.82f, 0.84f), 0.3f);
                }

                if (biome == Biome.Lake && grain > 0.32f) {
                    color = Color.Lerp(color, new Color(0.46f, 0.74f, 0.76f), 0.2f);
                }

                if (biome == Biome.SparseForest || biome == Biome.DenseForest) {
                    // more canopy pixels make dense forests look darker
                    float canopy = Mathf.PerlinNoise(offsetX + 217f + x * 0.3f, offsetY - 193f + y * 0.3f);
                    float threshold = biome == Biome.DenseForest ? 0.47f : 0.62f;
                    
                    if (canopy > threshold) {
                        color = Color.Lerp(color, new Color(0.12f, 0.32f, 0.17f),
                            biome == Biome.DenseForest ? 0.7f : 0.48f);
                    }
                }

                pixels[y * width + x] = (Color32)(color * Mathf.Clamp(light, 0.75f, 1.23f));
            }
        }

        // 1 sprite lets Unity draw the whole map in a single piece
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // defines the biomes color: deeper cells -> darker color
    private static Color BaseColor(Biome biome, float elevation, float lakeDepth, float forestDensity) {
        switch (biome) {
            case Biome.Lake:
                return Color.Lerp(
                    new Color(0.36f, 0.63f, 0.65f),
                    new Color(0.13f, 0.38f, 0.53f),
                    Mathf.Clamp01(lakeDepth * 1.8f));
            
            case Biome.SparseForest:
                return Color.Lerp(
                    new Color(0.34f, 0.61f, 0.31f),
                    new Color(0.27f, 0.48f, 0.27f),
                    Mathf.Clamp01(forestDensity));
            
            case Biome.DenseForest:
                return new Color(0.18f, 0.42f, 0.23f);
            
            case Biome.Hill:
                return Color.Lerp(
                    new Color(0.34f, 0.62f, 0.32f),
                    new Color(0.44f, 0.53f, 0.37f),
                    Mathf.SmoothStep(0f, 1f,
                        (elevation - TerrainMap.HillLevel) / (TerrainMap.MountainLevel - TerrainMap.HillLevel)));
            
            case Biome.Mountain:
                // rock grows lighter as it gets close to the snow line
                return Color.Lerp(
                    new Color(0.44f, 0.53f, 0.37f),
                    new Color(0.72f, 0.77f, 0.74f),
                    Mathf.SmoothStep(0f, 1f,
                        (elevation - TerrainMap.MountainLevel) / (TerrainMap.SnowLevel - TerrainMap.MountainLevel)));
            
            case Biome.SnowMountain:
                return Color.Lerp(
                    new Color(0.72f, 0.77f, 0.74f),
                    new Color(0.91f, 0.94f, 0.91f),
                    Mathf.SmoothStep(0f, 1f, (elevation - TerrainMap.SnowLevel) / 0.13f));
            
            default:
                return new Color(0.34f, 0.62f, 0.32f);
        }
    }
}
