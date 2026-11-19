using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OpenWaters
{
    internal sealed class WorldSettings
    {
        public const int SidecarVersion = 1;
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

        public WorldSettings(
            int islandAmount,
            int worldRadius,
            int biomeLimit,
            int startBiomeLimit,
            bool excludeMountains)
        {
            IslandAmount = islandAmount;
            WorldRadius = worldRadius;
            BiomeLimit = biomeLimit;
            StartBiomeLimit = startBiomeLimit;
            ExcludeMountains = excludeMountains;
        }

        public static WorldSettings Vanilla =>
            new WorldSettings(VanillaIslands, DefaultWorldRadius, 0, 0, false);

        public bool AffectsGeneration =>
            IslandAmount != VanillaIslands || ExcludeMountains || StartBiomeLimit != 0;
    }

    internal static class WorldSettingsCodec
    {
        public static string Serialize(WorldSettings settings, string seed)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("# Open Waters world settings");
            text.AppendLine("version=" + WorldSettings.SidecarVersion.ToString(CultureInfo.InvariantCulture));
            text.AppendLine("seed=" + seed);
            text.AppendLine("islandAmount=" + settings.IslandAmount.ToString(CultureInfo.InvariantCulture));
            text.AppendLine("worldRadius=" + settings.WorldRadius.ToString(CultureInfo.InvariantCulture));
            text.AppendLine("biomeLimit=" + settings.BiomeLimit.ToString(CultureInfo.InvariantCulture));
            text.AppendLine("startBiomeLimit=" + settings.StartBiomeLimit.ToString(CultureInfo.InvariantCulture));
            text.AppendLine("excludeMountains=" + settings.ExcludeMountains.ToString().ToLowerInvariant());
            return text.ToString();
        }

        public static bool TryParse(string text, string seed, out WorldSettings settings)
        {
            settings = null;
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(seed))
            {
                return false;
            }

            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }
                int separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }
                values[line.Substring(0, separator).Trim()] = line.Substring(separator + 1).Trim();
            }

            if (!TryGetInt(values, "version", out int version) ||
                version != WorldSettings.SidecarVersion ||
                !TryGetString(values, "seed", out string fileSeed) || fileSeed != seed ||
                !TryGetInt(values, "islandAmount", out int islandAmount) ||
                !TryGetInt(values, "worldRadius", out int worldRadius) ||
                !TryGetInt(values, "biomeLimit", out int biomeLimit) ||
                !TryGetInt(values, "startBiomeLimit", out int startBiomeLimit) ||
                !TryGetBool(values, "excludeMountains", out bool excludeMountains))
            {
                return false;
            }

            if (islandAmount < WorldSettings.FewIslands ||
                islandAmount > WorldSettings.VanillaIslands ||
                worldRadius < WorldSettings.MinWorldRadius ||
                worldRadius > WorldSettings.MaxWorldRadius ||
                biomeLimit < 0 || biomeLimit > WorldSettings.MaxBiomeLimit ||
                startBiomeLimit < 0 || startBiomeLimit > WorldSettings.MaxBiomeLimit)
            {
                return false;
            }

            settings = new WorldSettings(
                islandAmount, worldRadius, biomeLimit, startBiomeLimit, excludeMountains);
            return true;
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
    }

    internal interface ISidecarStorage
    {
        bool Exists(string path);
        string Read(string path);
        void Write(string path, string text);
        void Delete(string path);
        void Move(string source, string destination);
    }

    internal sealed class FileSidecarStorage : ISidecarStorage
    {
        public bool Exists(string path)
        {
            return System.IO.File.Exists(path);
        }

        public string Read(string path)
        {
            return System.IO.File.ReadAllText(path);
        }

        public void Write(string path, string text)
        {
            System.IO.File.WriteAllText(path, text);
        }

        public void Delete(string path)
        {
            System.IO.File.Delete(path);
        }

        public void Move(string source, string destination)
        {
            System.IO.File.Move(source, destination);
        }
    }
}
