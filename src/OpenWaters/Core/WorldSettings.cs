using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OpenWaters
{
    internal sealed class WorldSettings
    {
        public const int SidecarVersion = 1;
        public const int GeneratorVersion = 1;
        private static readonly int[] _supportedGeneratorVersions = { GeneratorVersion };
        public static string SupportedVersionsText => string.Join(", ", _supportedGeneratorVersions);

        public const int FewIslands = 0;
        public const int MediumIslands = 1;
        public const int VanillaIslands = 2;
        public const int MinWorldRadius = 6000;
        public const int MaxWorldRadius = 10000;
        public const int DefaultWorldRadius = 10000;
        public const int MaxBiomeLimit = 3;

        public readonly int IslandAmount;
        public readonly int WorldRadius;
        public readonly int BiomeLimit;
        public readonly int StartBiomeLimit;
        public readonly bool ExcludeMountains;
        public readonly int SidecarFormatVersion;
        public readonly int GeneratorVersionValue;
        public readonly string ModVersion;
        public readonly float FewLandFraction;
        public readonly float FewFrequencyScale;
        public readonly float FewPeakProtection;
        public readonly float FewDetailDamping;
        public readonly float FewMountainCompressionStart;
        public readonly float FewMountainCompressionFactor;
        public readonly float MediumLandFraction;
        public readonly float MediumFrequencyScale;
        public readonly float MediumPeakProtection;
        public readonly float MediumDetailDamping;

        public WorldSettings(
            int islandAmount,
            int worldRadius,
            int biomeLimit,
            int startBiomeLimit,
            bool excludeMountains,
            string modVersion = "0.1.0",
            float fewLandFraction = 0.27f,
            float fewFrequencyScale = 0.60f,
            float fewPeakProtection = 0.20f,
            float fewDetailDamping = 0.35f,
            float fewMountainCompressionStart = 0.34f,
            float fewMountainCompressionFactor = 0.55f,
            float mediumLandFraction = 0.50f,
            float mediumFrequencyScale = 0.70f,
            float mediumPeakProtection = 0.60f,
            float mediumDetailDamping = 0.35f,
            int sidecarFormatVersion = SidecarVersion,
            int generatorVersion = GeneratorVersion)
        {
            IslandAmount = islandAmount;
            WorldRadius = worldRadius;
            BiomeLimit = biomeLimit;
            StartBiomeLimit = startBiomeLimit;
            ExcludeMountains = excludeMountains;
            ModVersion = modVersion;
            FewLandFraction = fewLandFraction;
            FewFrequencyScale = fewFrequencyScale;
            FewPeakProtection = fewPeakProtection;
            FewDetailDamping = fewDetailDamping;
            FewMountainCompressionStart = fewMountainCompressionStart;
            FewMountainCompressionFactor = fewMountainCompressionFactor;
            MediumLandFraction = mediumLandFraction;
            MediumFrequencyScale = mediumFrequencyScale;
            MediumPeakProtection = mediumPeakProtection;
            MediumDetailDamping = mediumDetailDamping;
            SidecarFormatVersion = sidecarFormatVersion;
            GeneratorVersionValue = generatorVersion;
        }

        public static WorldSettings Vanilla => new WorldSettings(VanillaIslands, DefaultWorldRadius, 0, 0, false);

        public bool AffectsGeneration =>
            IslandAmount != VanillaIslands || ExcludeMountains || StartBiomeLimit != 0;

        public bool IsGeneratorCompatible
        {
            get
            {
                foreach (int version in _supportedGeneratorVersions)
                {
                    if (version == GeneratorVersionValue)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public float LandFraction =>
            IslandAmount == FewIslands ? FewLandFraction :
            IslandAmount == MediumIslands ? MediumLandFraction : 1f;

        public float FrequencyScale =>
            IslandAmount == FewIslands ? FewFrequencyScale : MediumFrequencyScale;

        public float DetailDamping =>
            IslandAmount == FewIslands ? FewDetailDamping : MediumDetailDamping;

        public float PeakProtection =>
            IslandAmount == FewIslands ? FewPeakProtection : MediumPeakProtection;
    }

    internal static class WorldSettingsCodec
    {
        public static string Serialize(WorldSettings settings, string seed)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("# Open Waters world settings");
            WriteInt(text, "version", settings.SidecarFormatVersion);
            WriteInt(text, "generatorVersion", settings.GeneratorVersionValue);
            WriteString(text, "modVersion", settings.ModVersion);
            WriteString(text, "seed", Encode(seed));
            WriteInt(text, "islandAmount", settings.IslandAmount);
            WriteInt(text, "worldRadius", settings.WorldRadius);
            WriteInt(text, "biomeLimit", settings.BiomeLimit);
            WriteInt(text, "startBiomeLimit", settings.StartBiomeLimit);
            WriteBool(text, "excludeMountains", settings.ExcludeMountains);
            WriteFloat(text, "fewLandFraction", settings.FewLandFraction);
            WriteFloat(text, "fewFrequencyScale", settings.FewFrequencyScale);
            WriteFloat(text, "fewPeakProtection", settings.FewPeakProtection);
            WriteFloat(text, "fewDetailDamping", settings.FewDetailDamping);
            WriteFloat(text, "fewMountainCompressionStart", settings.FewMountainCompressionStart);
            WriteFloat(text, "fewMountainCompressionFactor", settings.FewMountainCompressionFactor);
            WriteFloat(text, "mediumLandFraction", settings.MediumLandFraction);
            WriteFloat(text, "mediumFrequencyScale", settings.MediumFrequencyScale);
            WriteFloat(text, "mediumPeakProtection", settings.MediumPeakProtection);
            WriteFloat(text, "mediumDetailDamping", settings.MediumDetailDamping);
            return text.ToString();
        }

        public static bool TryParse(string text, string seed, out WorldSettings settings)
        {
            settings = null;
            if (string.IsNullOrEmpty(text) || seed == null)
            {
                return false;
            }

            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string rawLine in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }
                int separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    return false;
                }
                string key = line.Substring(0, separator).Trim();
                if (values.ContainsKey(key))
                {
                    return false;
                }
                values[key] = line.Substring(separator + 1).Trim();
            }

            if (!TryGetInt(values, "version", out int version) ||
                version != WorldSettings.SidecarVersion ||
                !TryGetInt(values, "generatorVersion", out int generatorVersion) ||
                !TryGetString(values, "modVersion", out string modVersion) ||
                !values.TryGetValue("seed", out string encodedSeed) ||
                !TryDecode(encodedSeed, out string fileSeed) || fileSeed != seed ||
                !TryGetInt(values, "islandAmount", out int islandAmount) ||
                !TryGetInt(values, "worldRadius", out int worldRadius) ||
                !TryGetInt(values, "biomeLimit", out int biomeLimit) ||
                !TryGetInt(values, "startBiomeLimit", out int startBiomeLimit) ||
                !TryGetBool(values, "excludeMountains", out bool excludeMountains) ||
                !TryGetFloat(values, "fewLandFraction", out float fewLandFraction) ||
                !TryGetFloat(values, "fewFrequencyScale", out float fewFrequencyScale) ||
                !TryGetFloat(values, "fewPeakProtection", out float fewPeakProtection) ||
                !TryGetFloat(values, "fewDetailDamping", out float fewDetailDamping) ||
                !TryGetFloat(values, "fewMountainCompressionStart", out float fewMountainCompressionStart) ||
                !TryGetFloat(values, "fewMountainCompressionFactor", out float fewMountainCompressionFactor) ||
                !TryGetFloat(values, "mediumLandFraction", out float mediumLandFraction) ||
                !TryGetFloat(values, "mediumFrequencyScale", out float mediumFrequencyScale) ||
                !TryGetFloat(values, "mediumPeakProtection", out float mediumPeakProtection) ||
                !TryGetFloat(values, "mediumDetailDamping", out float mediumDetailDamping))
            {
                return false;
            }

            if (islandAmount < WorldSettings.FewIslands ||
                islandAmount > WorldSettings.VanillaIslands ||
                worldRadius < WorldSettings.MinWorldRadius ||
                worldRadius > WorldSettings.MaxWorldRadius ||
                biomeLimit < 0 || biomeLimit > WorldSettings.MaxBiomeLimit ||
                startBiomeLimit < 0 || startBiomeLimit > WorldSettings.MaxBiomeLimit ||
                !IsInRange(fewLandFraction, 0.01f, 1f) ||
                !IsInRange(fewFrequencyScale, 0.10f, 2f) ||
                !IsInRange(fewPeakProtection, 0f, 1f) ||
                !IsInRange(fewDetailDamping, 0f, 1f) ||
                !IsInRange(fewMountainCompressionStart, 0f, 1f) ||
                !IsInRange(fewMountainCompressionFactor, 0f, 1f) ||
                !IsInRange(mediumLandFraction, 0.01f, 1f) ||
                !IsInRange(mediumFrequencyScale, 0.10f, 2f) ||
                !IsInRange(mediumPeakProtection, 0f, 1f) ||
                !IsInRange(mediumDetailDamping, 0f, 1f))
            {
                return false;
            }

            settings = new WorldSettings(
                islandAmount, worldRadius, biomeLimit, startBiomeLimit, excludeMountains,
                modVersion, fewLandFraction, fewFrequencyScale, fewPeakProtection, fewDetailDamping,
                fewMountainCompressionStart, fewMountainCompressionFactor,
                mediumLandFraction, mediumFrequencyScale, mediumPeakProtection, mediumDetailDamping,
                version, generatorVersion);
            return true;
        }

        private static bool IsInRange(float value, float min, float max)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) &&
                   value >= min && value <= max;
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static bool TryDecode(string value, out string decoded)
        {
            decoded = null;
            try
            {
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value));
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static void WriteInt(StringBuilder text, string key, int value)
        {
            text.Append(key).Append('=').Append(value.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        private static void WriteBool(StringBuilder text, string key, bool value)
        {
            text.Append(key).Append('=').Append(value ? "true" : "false").Append('\n');
        }

        private static void WriteFloat(StringBuilder text, string key, float value)
        {
            text.Append(key).Append('=').Append(value.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
        }

        private static void WriteString(StringBuilder text, string key, string value)
        {
            text.Append(key).Append('=').Append(value).Append('\n');
        }

        private static bool TryGetString(Dictionary<string, string> values, string key, out string value)
        {
            return values.TryGetValue(key, out value) && !string.IsNullOrEmpty(value);
        }

        private static bool TryGetInt(Dictionary<string, string> values, string key, out int value)
        {
            value = 0;
            return values.TryGetValue(key, out string text) &&
                int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryGetBool(Dictionary<string, string> values, string key, out bool value)
        {
            value = false;
            return values.TryGetValue(key, out string text) && bool.TryParse(text, out value);
        }

        private static bool TryGetFloat(Dictionary<string, string> values, string key, out float value)
        {
            value = 0f;
            return values.TryGetValue(key, out string text) &&
                float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }

    internal static class SidecarLocator
    {
        public static string FindBySeed(
            string[] directories,
            string seed,
            System.Func<string, string> read,
            System.Func<string, string> toSidecarPath)
        {
            if (directories == null || string.IsNullOrEmpty(seed))
            {
                return null;
            }
            foreach (string directory in directories)
            {
                string path = toSidecarPath(directory);
                try
                {
                    if (WorldSettingsCodec.TryParse(read(path), seed, out _))
                    {
                        return path;
                    }
                }
                catch (System.Exception)
                {
                }
            }
            return null;
        }
    }

    internal interface ISidecarStorage
    {
        bool Exists(string path);
        string Read(string path);
        void Write(string path, string text);
        void Delete(string path);
        void Move(string source, string destination);
        string[] GetDirectories(string path);
    }

    internal sealed class FileSidecarStorage : ISidecarStorage
    {
        public bool Exists(string path) => System.IO.File.Exists(path);
        public string Read(string path) => System.IO.File.ReadAllText(path);
        public void Write(string path, string text) => System.IO.File.WriteAllText(path, text);
        public void Delete(string path) => System.IO.File.Delete(path);
        public void Move(string source, string destination) => System.IO.File.Move(source, destination);
        public string[] GetDirectories(string path) => System.IO.Directory.GetDirectories(path);
    }
}
