using System;
using System.IO;

namespace OpenWaters
{
    internal enum SidecarStatus
    {
        Ok,
        Missing,
        Invalid
    }

    internal static class WorldSettingsStore
    {
        private const string SidecarFileName = "OpenWaters.worldSettings";

        private static ISidecarStorage _storage = new FileSidecarStorage();

        public static SidecarStatus LastStatus { get; private set; } = SidecarStatus.Missing;
        public static string LastMessage { get; private set; } = string.Empty;

        internal static void SetStorage(ISidecarStorage storage)
        {
            _storage = storage ?? new FileSidecarStorage();
        }

        public static WorldSettings FromCurrentSettings(string seed)
        {
            return new WorldSettings(
                OpenWatersSettings.IslandAmount,
                OpenWatersSettings.WorldRadius,
                OpenWatersSettings.BiomeLimit,
                OpenWatersSettings.StartBiomeLimit,
                OpenWatersSettings.ExcludeMountains,
                OpenWatersPlugin.PluginVersion,
                OpenWatersSettings.FewLandFraction,
                OpenWatersSettings.FewFrequencyScale,
                OpenWatersSettings.FewPeakProtection,
                OpenWatersSettings.FewDetailDamping,
                OpenWatersSettings.FewMountainCompressionStart,
                OpenWatersSettings.FewMountainCompressionFactor,
                OpenWatersSettings.MediumLandFraction,
                OpenWatersSettings.MediumFrequencyScale,
                OpenWatersSettings.MediumPeakProtection,
                OpenWatersSettings.MediumDetailDamping);
        }

        public static bool TryWriteForNewWorld(string name, string seed, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(name) || seed == null)
            {
                error = "World name is empty.";
                return false;
            }

            string path = GetSidecarPath(name);
            if (path == null)
            {
                error = "Could not resolve the local save directory.";
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string temporaryPath = path + ".tmp";
                _storage.Write(
                    temporaryPath,
                    WorldSettingsCodec.Serialize(FromCurrentSettings(seed), seed));
                if (_storage.Exists(path))
                {
                    _storage.Delete(path);
                }
                _storage.Move(temporaryPath, path);
                OpenWatersPlugin.Log.LogInfo($"[Settings] Saved sidecar '{path}'.");
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                OpenWatersPlugin.Log.LogError($"[Settings] Failed to write sidecar '{path}': {exception}");
                return false;
            }
        }

        public static WorldSettings Apply(World world)
        {
            LastStatus = SidecarStatus.Missing;
            LastMessage = string.Empty;
            if (world == null || world.m_menu)
            {
                return WorldSettings.Vanilla;
            }
            if (world.m_worldName == OpenWatersSettings.PreviewWorldName)
            {
                LastStatus = SidecarStatus.Ok;
                return FromCurrentSettings(world.m_seedName);
            }
            if (!OpenWatersSettings.Enabled)
            {
                LastMessage = "Open Waters is disabled; generation stays vanilla.";
                OpenWatersPlugin.Log.LogInfo($"[Settings] {LastMessage}");
                return WorldSettings.Vanilla;
            }

            string path = GetSidecarPath(world.m_worldName);
            if (path == null || !_storage.Exists(path))
            {
                LastMessage = $"No sidecar for '{world.m_worldName}'; using vanilla generation.";
                OpenWatersPlugin.Log.LogInfo($"[Settings] {LastMessage}");
                return WorldSettings.Vanilla;
            }

            WorldSettings settings;
            try
            {
                if (!WorldSettingsCodec.TryParse(_storage.Read(path), world.m_seedName, out settings))
                {
                    LastStatus = SidecarStatus.Invalid;
                    LastMessage = $"Sidecar for '{world.m_worldName}' is unreadable or has an incompatible format.";
                    OpenWatersPlugin.Log.LogError($"[Settings] {LastMessage}");
                    return WorldSettings.Vanilla;
                }
            }
            catch (Exception exception)
            {
                LastStatus = SidecarStatus.Invalid;
                LastMessage = $"Failed to read sidecar for '{world.m_worldName}': {exception.Message}";
                OpenWatersPlugin.Log.LogError($"[Settings] {LastMessage}");
                return WorldSettings.Vanilla;
            }

            if (!settings.IsGeneratorCompatible)
            {
                int found = settings.GeneratorVersionValue;
                settings = WorldSettings.Vanilla;
                LastStatus = SidecarStatus.Invalid;
                LastMessage =
                    $"World '{world.m_worldName}' was generated by generator version {found}, " +
                    $"but this mod supports {WorldSettings.SupportedVersionsText}.";
                OpenWatersPlugin.Log.LogError($"[Settings] {LastMessage}");
                return settings;
            }

            LastStatus = SidecarStatus.Ok;
            LastMessage = $"Loaded settings for '{world.m_worldName}': preset={settings.IslandAmount}, radius={settings.WorldRadius}.";
            OpenWatersPlugin.Log.LogInfo($"[Settings] {LastMessage}");
            return settings;
        }

        public static SidecarStatus ValidateForLoad(World world)
        {
            Apply(world);
            return LastStatus;
        }

        public static string GetDisplayName(WorldSettings settings)
        {
            return settings.IslandAmount == WorldSettings.FewIslands ? "Few" :
                settings.IslandAmount == WorldSettings.MediumIslands ? "Medium" : "Vanilla";
        }

        private static string GetSidecarPath(string worldName)
        {
            if (string.IsNullOrEmpty(worldName) || worldName == "." || worldName == "..")
            {
                return null;
            }
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                if (worldName.IndexOf(invalid) >= 0)
                {
                    return null;
                }
            }

            try
            {
                string directory = SaveSystem.GetWorldsSaveRootPath(FileHelpers.FileSource.Local) + "/" + worldName + "/";
                return Path.Combine(directory, SidecarFileName);
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning(
                    $"[Settings] Failed to resolve save directory for '{worldName}': {exception.Message}");
                return null;
            }
        }
    }
}
