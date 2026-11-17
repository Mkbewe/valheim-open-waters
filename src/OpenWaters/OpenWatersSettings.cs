using UnityEngine;

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
        public const string PreviewWorldName = "OpenWatersPreview";

        public static int WorldRadius = DefaultWorldRadius;
        public static int IslandAmount = VanillaIslandAmount;
        public static int BiomeLimit;
        public static int StartBiomeLimit;
        public static bool ExcludeMountains = false;
        public static bool Enabled = true;
        public static bool PreviewEnabled;

        public static float FewLandFraction =>
            Clamp(OpenWatersPlugin.FewLandFractionConfig?.Value, 0.27f, 0.01f, 1f);
        public static float MediumLandFraction =>
            Clamp(OpenWatersPlugin.MediumLandFractionConfig?.Value, 0.50f, 0.01f, 1f);
        public static float FewFrequencyScale =>
            Clamp(OpenWatersPlugin.FewFrequencyScaleConfig?.Value, 0.60f, 0.10f, 2f);
        public static float MediumFrequencyScale =>
            Clamp(OpenWatersPlugin.MediumFrequencyScaleConfig?.Value, 0.70f, 0.10f, 2f);
        public static float FewPeakProtection =>
            Clamp(OpenWatersPlugin.FewPeakProtectionConfig?.Value, 0.20f, 0f, 1f);
        public static float MediumPeakProtection =>
            Clamp(OpenWatersPlugin.MediumPeakProtectionConfig?.Value, 0.60f, 0f, 1f);
        public static float FewDetailDamping =>
            Clamp(OpenWatersPlugin.FewDetailDampingConfig?.Value, 0.35f, 0f, 1f);
        public static float MediumDetailDamping =>
            Clamp(OpenWatersPlugin.MediumDetailDampingConfig?.Value, 0.35f, 0f, 1f);
        public static float FewMountainCompressionStart =>
            Clamp(OpenWatersPlugin.FewMountainCompressionStartConfig?.Value, 0.34f, 0f, 1f);
        public static float FewMountainCompressionFactor =>
            Clamp(OpenWatersPlugin.FewMountainCompressionFactorConfig?.Value, 0.55f, 0f, 1f);

        public static bool AffectsGeneration => Enabled &&
            (IslandAmount != VanillaIslandAmount || ExcludeMountains);

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

        private static float Clamp(float? value, float fallback, float min, float max)
        {
            return Mathf.Clamp(value ?? fallback, min, max);
        }
    }
}
