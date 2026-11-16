namespace OpenWaters
{
    internal static class OpenWatersSettings
    {
        public const int MinWorldRadius = 6000;
        public const int MaxWorldRadius = 10000;
        public const int DefaultWorldRadius = 10000;
        public const int FewIslands = 0;
        public const int MediumIslands = 1;
        public const int VanillaIslandAmount = 2;
        public const float FewLandFraction = 0.27f;
        public const float MediumLandFraction = 0.50f;
        public const string PreviewWorldName = "OpenWatersPreview";

        public static int WorldRadius = DefaultWorldRadius;
        public static int IslandAmount = VanillaIslandAmount;
        public static int BiomeLimit;
        public static int StartBiomeLimit;
        public static bool ExcludeMountains = true;
        public static bool Enabled = true;
        public static bool PreviewEnabled;

        public static bool AffectsGeneration => Enabled && IslandAmount != VanillaIslandAmount;

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

        public static float LandFraction
        {
            get
            {
                if (IslandAmount == FewIslands)
                {
                    return FewLandFraction;
                }
                if (IslandAmount == MediumIslands)
                {
                    return MediumLandFraction;
                }
                return 1f;
            }
        }
    }
}
