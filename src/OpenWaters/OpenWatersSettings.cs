namespace OpenWaters
{
    internal static class OpenWatersSettings
    {
        public const float MinLandCoverage = 0.05f;
        public const float MaxLandCoverage = 0.40f;
        public const int MinWorldRadius = 6000;
        public const int MaxWorldRadius = 10000;
        public const int VanillaIslandAmount = 2;

        public static float LandCoverage = 0.15f;
        public static int WorldRadius = 10000;
        public static int IslandAmount = VanillaIslandAmount;
        public static int BiomeLimit;
        public static int StartBiomeLimit;
        public static bool ExcludeMountains = true;

        public static bool AffectsGeneration => IslandAmount != VanillaIslandAmount;
    }
}
