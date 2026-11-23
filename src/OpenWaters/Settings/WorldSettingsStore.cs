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
        private static readonly RemoteSettingsState _remote = new RemoteSettingsState();

        internal static void SetStorage(ISidecarStorage storage)
        {
            _storage = storage ?? new FileSidecarStorage();
        }

        public static void SetRemoteSnapshot(WorldSettings settings, string seed)
        {
            _remote.Set(settings, seed);
        }

        public static void MarkPeerInfoReceived()
        {
            _remote.MarkPeerInfoReceived();
        }

        public static void MarkRemotePayloadInvalid()
        {
            _remote.MarkPayloadInvalid();
        }

        public static void ClearRemoteSnapshot()
        {
            _remote.Clear();
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
                error = string.IsNullOrEmpty(name) ? "World name is empty." : "World seed is missing.";
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
                OpenWatersPlugin.Log.LogInfo("[Settings] Apply: menu or null world, staying vanilla.");
                return WorldSettings.Vanilla;
            }

            if (!OpenWatersSettings.Enabled)
            {
                LastMessage = "Open Waters is disabled; generation stays vanilla.";
                OpenWatersPlugin.Log.LogInfo($"[Settings] {LastMessage}");
                return WorldSettings.Vanilla;
            }

            OpenWatersPlugin.Log.LogInfo(
                $"[Settings] Apply: world='{world.m_worldName}', seed='{world.m_seedName}', " +
                $"enabled={OpenWatersSettings.Enabled}.");

            if (world.m_worldName == OpenWatersSettings.PreviewWorldName)
            {
                LastStatus = SidecarStatus.Ok;
                WorldSettings preview = FromCurrentSettings(world.m_seedName);
                OpenWatersPlugin.Log.LogInfo(
                    $"[Settings] Apply: preview world, preset={GetDisplayName(preview)}, " +
                    $"radius={preview.WorldRadius}, biomes={preview.StartBiomeLimit}, mountains={preview.ExcludeMountains}.");
                return preview;
            }

            RemoteSettingsStatus remoteStatus = _remote.Resolve(world.m_seedName, out WorldSettings remoteSettings);
            if (remoteStatus != RemoteSettingsStatus.NotReceived)
            {
                switch (remoteStatus)
                {
                    case RemoteSettingsStatus.Accepted:
                        LastStatus = SidecarStatus.Ok;
                        LastMessage = $"Using settings received from the server for seed '{world.m_seedName}'.";
                        OpenWatersPlugin.Log.LogInfo(
                            $"[Settings] {LastMessage}; preset={GetDisplayName(remoteSettings)}, " +
                            $"radius={remoteSettings.WorldRadius}, biomes={remoteSettings.StartBiomeLimit}, " +
                            $"mountains={remoteSettings.ExcludeMountains}, " +
                            $"generatorVersion={remoteSettings.GeneratorVersionValue}.");
                        return remoteSettings;
                    case RemoteSettingsStatus.NoSnapshot:
                        LastStatus = SidecarStatus.Missing;
                        LastMessage = "Server does not use Open Waters; generation stays vanilla.";
                        break;
                    case RemoteSettingsStatus.SeedMismatch:
                        LastStatus = SidecarStatus.Invalid;
                        LastMessage = "Server world settings belong to a different seed; generation stays vanilla.";
                        break;
                    case RemoteSettingsStatus.InvalidPayload:
                        LastStatus = SidecarStatus.Invalid;
                        LastMessage = "Server world settings are incompatible; generation stays vanilla.";
                        break;
                    case RemoteSettingsStatus.IncompatibleGenerator:
                        LastStatus = SidecarStatus.Invalid;
                        LastMessage =
                            $"Server uses generator version {remoteSettings.GeneratorVersionValue}, " +
                            $"this mod supports {WorldSettings.SupportedVersionsText}.";
                        break;
                }
                OpenWatersPlugin.Log.LogInfo($"[Settings] {LastMessage}");
                return WorldSettings.Vanilla;
            }

            string path = FindSidecarPath(world);
            OpenWatersPlugin.Log.LogInfo($"[Settings] Sidecar path: '{path ?? "none"}'.");
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
            LastMessage = $"Loaded settings for '{world.m_worldName}'";
            OpenWatersPlugin.Log.LogInfo(
                $"[Settings] {LastMessage}: file='{path}', seed='{world.m_seedName}', " +
                $"preset={GetDisplayName(settings)}, radius={settings.WorldRadius}, " +
                $"biomes={settings.StartBiomeLimit}, mountains={settings.ExcludeMountains}, " +
                $"generatorVersion={settings.GeneratorVersionValue}, modVersion={settings.ModVersion}.");
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

        private static string FindSidecarPath(World world)
        {
            string byName = GetSidecarPath(world.m_worldName);
            if (byName != null)
            {
                return byName;
            }
            if (string.IsNullOrEmpty(world.m_seedName))
            {
                return null;
            }

            try
            {
                string root = SaveSystem.GetWorldsSaveRootPath(FileHelpers.FileSource.Local);
                string[] directories = _storage.GetDirectories(root);
                OpenWatersPlugin.Log.LogInfo(
                    $"[Settings] World name is empty; searching {directories.Length} local folders for seed '{world.m_seedName}'.");
                string found = SidecarLocator.FindBySeed(
                    directories,
                    world.m_seedName,
                    path => _storage.Exists(path) ? _storage.Read(path) : null,
                    directory => Path.Combine(directory, SidecarFileName));
                OpenWatersPlugin.Log.LogInfo($"[Settings] Seed search result: '{found ?? "none"}'.");
                return found;
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning($"[Settings] Could not search sidecars by seed: {exception.Message}");
                return null;
            }
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
