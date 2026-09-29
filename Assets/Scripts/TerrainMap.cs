using UnityEngine;

namespace MiddleAgents
{
    public enum Biome : byte
    {
        Valley,
        SparseForest,
        DenseForest,
        Lake,
        Hill,
        Mountain,
        SnowMountain
    }

    public sealed class TerrainMap
    {
        public const float HillLevel = 0.43f;
        public const float MountainLevel = 0.54f;
        public const float SnowLevel = 0.81f;

        public readonly int Width;
        public readonly int Height;
        public readonly float[] Elevation;
        public readonly float[] Lake;
        public readonly float[] Forest;
        public readonly Biome[] Biomes;

        public TerrainMap(int width, int height)
        {
            Width = width;
            Height = height;
            int count = width * height;
            Elevation = new float[count];
            Lake = new float[count];
            Forest = new float[count];
            Biomes = new Biome[count];
        }

        public int Index(int x, int y)
        {
            return y * Width + x;
        }

        public bool Contains(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public float HeightAt(int x, int y)
        {
            return Elevation[Index(Mathf.Clamp(x, 0, Width - 1), Mathf.Clamp(y, 0, Height - 1))];
        }
    }
}
