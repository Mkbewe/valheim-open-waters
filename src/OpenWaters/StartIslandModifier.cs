using UnityEngine;

namespace OpenWaters
{
    internal static class StartIslandModifier
    {
        private const float SafeRadius = 250f;
        private const float SuppressStart = 0.34f;
        private const float SuppressFactor = 0.35f;
        private const float SuppressCap = 0.39f;

        public static bool TryGetBiomeOverride(
            float wx,
            float wy,
            Heightmap.Biome originalBiome,
            out Heightmap.Biome biome)
        {
            biome = Heightmap.Biome.None;
            int limit = OpenWatersSettings.StartBiomeLimit;
            if ((limit != 1 && limit != 2) || !IslandAnalysis.IsStartIsland(wx, wy))
            {
                return false;
            }

            if (limit == 1)
            {
                biome = Heightmap.Biome.Meadows;
                return true;
            }

            float distance = Mathf.Sqrt(wx * wx + wy * wy);
            if (distance <= SafeRadius || originalBiome == Heightmap.Biome.Meadows)
            {
                biome = Heightmap.Biome.Meadows;
                return true;
            }

            if (originalBiome == Heightmap.Biome.BlackForest)
            {
                biome = Heightmap.Biome.BlackForest;
                return true;
            }

            float noise = Mathf.PerlinNoise((wx + 1000f) * 0.0015f, (wy + 1000f) * 0.0015f);
            biome = noise > 0.5f ? Heightmap.Biome.Meadows : Heightmap.Biome.BlackForest;
            return true;
        }

        public static float Apply(float value, float wx, float wy)
        {
            if (!OpenWatersSettings.ExcludeMountains || !IslandAnalysis.IsStartIsland(wx, wy))
            {
                return value;
            }

            if (value <= SuppressStart)
            {
                return value;
            }

            float compressed = SuppressStart + (value - SuppressStart) * SuppressFactor;
            return Mathf.Min(compressed, SuppressCap);
        }
    }
}
