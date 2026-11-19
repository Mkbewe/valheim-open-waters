using UnityEngine;

namespace OpenWaters
{
    internal static class BiomePalette
    {
        public static readonly Color32 Outside = new Color32(4, 8, 16, 255);
        public static readonly Color32 Water = new Color32(30, 70, 122, 255);
        public static readonly Color32 Land = new Color32(116, 178, 94, 255);
        public static readonly Color32 SwampWater = new Color32(88, 116, 92, 255);
        public static readonly Color32 HiddenLand = new Color32(72, 100, 66, 255);
        public static readonly Color32 SpawnMarker = new Color32(255, 120, 20, 255);

        public static Color32 ForBiome(Heightmap.Biome biome)
        {
            switch (biome)
            {
                case Heightmap.Biome.BlackForest:
                    return new Color32(36, 122, 74, 255);
                case Heightmap.Biome.Swamp:
                    return new Color32(160, 142, 78, 255);
                case Heightmap.Biome.Mountain:
                    return new Color32(198, 202, 210, 255);
                case Heightmap.Biome.Plains:
                    return new Color32(210, 186, 84, 255);
                case Heightmap.Biome.Mistlands:
                    return new Color32(132, 108, 154, 255);
                case Heightmap.Biome.AshLands:
                    return new Color32(142, 54, 38, 255);
                case Heightmap.Biome.DeepNorth:
                    return new Color32(174, 204, 220, 255);
                default:
                    return Land;
            }
        }
    }
}
