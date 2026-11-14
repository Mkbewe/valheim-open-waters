namespace OpenWaters
{
    internal static class OpenWatersSettings
    {
        public const float MinLandCoverage = 0.05f;
        public const float MaxLandCoverage = 0.40f;
        public const int MinWorldRadius = 6000;
        public const int MaxWorldRadius = 10000;
        public const int FewIslands = 0;
        public const int MediumIslands = 1;
        public const int VanillaIslandAmount = 2;
        public const float FewTargetLandCoverage = 0.08f;
        public const float MediumTargetLandCoverage = 0.15f;

        public static float LandCoverage = MediumTargetLandCoverage;
        public static int WorldRadius = 10000;
        public static int IslandAmount = VanillaIslandAmount;
        public static int BiomeLimit;
        public static int StartBiomeLimit;
        public static bool ExcludeMountains = true;

        public static bool AffectsGeneration => IslandAmount != VanillaIslandAmount;

        public static string IslandAmountName
        {
            get
            {
                if (IslandAmount == FewIslands)
                {
                    return "Few";
                }
                if (IslandAmount == MediumIslands)
                {
                    return "Medium";
                }
                if (IslandAmount == VanillaIslandAmount)
                {
                    return "Vanilla";
                }
                return "Unknown";
            }
        }

        public static float TargetLandCoverage
        {
            get
            {
                if (IslandAmount == FewIslands)
                {
                    return FewTargetLandCoverage;
                }
                if (IslandAmount == MediumIslands)
                {
                    return MediumTargetLandCoverage;
                }
                return 0f;
            }
        }
    }
}
