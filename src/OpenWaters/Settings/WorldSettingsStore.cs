using System;
using System.Globalization;
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

        public static WorldSettings FromCurrentSettings(string seed, string uid = null)
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
                OpenWatersSettings.MediumDetailDamping,
                uid: uid);
        }

        public static bool TryWriteForNewWorld(string name, string seed, long uid, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(name))
            {
                error = "World name is empty.";
                return false;
            }
            if (seed == null)
            {
                error = "World seed is missing.";
                return false;
            }
            if (uid == 0L)
            {
                error = "World UID is missing.";
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

                string uidText = uid.ToString(CultureInfo.InvariantCulture);
                string temporaryPath = path + ".tmp";
                _storage.Write(temporaryPath, WorldSettingsCodec.Serialize(FromCurrentSettings(seed, uidText), seed));
                if (_storage.Exists(path))
                {
                    _storage.Delete(path);
                }
                _storage.Move(temporaryPath, path);
                OpenWatersPlugin.Log.LogInfo($"[Settings] Saved sidecar '{path}' with uid '{uidText}'.");
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
                LastMessage = "Menu or null world; generation stays vanilla.";
                OpenWatersPlugin.Log.LogInfo($"[Settings] {LastMessage}");
                return WorldSettings.Vanilla;
            }

            if (!OpenWatersSettings.Enabled)
            {
                LastMessage = "Open Waters is disabled; generation stays vanilla.";
                OpenWatersPlugin.Log.LogInfo($"[Settings] {LastMessage}");
                return WorldSettings.Vanilla;
            }

            OpenWatersPlugin.Log.LogInfo(
                $"[Settings] World '{world.m_worldName}' uid={WorldUid(world) ?? "none"}.");

            Resolution resolution = Resolve(world);
            LastStatus = resolution.Status;
            LastMessage = resolution.Message;
            OpenWatersPlugin.Log.LogInfo($"[Settings] {LastMessage}");
            return resolution.Settings;
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

        private static Resolution Resolve(World world)
        {
            if (world.m_worldName == OpenWatersSettings.PreviewWorldName)
            {
                WorldSettings preview = FromCurrentSettings(world.m_seedName);
                return Ok(preview, $"Preview world: preset={GetDisplayName(preview)}.");
            }

            RemoteSettingsStatus remote = _remote.Resolve(world.m_seedName, out WorldSettings remoteSettings);
            if (remote == RemoteSettingsStatus.Accepted)
            {
                return Ok(
                    remoteSettings,
                    $"Using settings received from the server for seed '{world.m_seedName}': " +
                    $"preset={GetDisplayName(remoteSettings)}.");
            }

            if (remote != RemoteSettingsStatus.NotReceived && !OpenWatersSettings.LocalSidecarFallback)
            {
                return Vanilla(remote, DescribeRemote(remote, remoteSettings));
            }

            return ResolveLocal(world, remote, remoteSettings);
        }

        private static Resolution ResolveLocal(
            World world, RemoteSettingsStatus remote, WorldSettings remoteSettings)
        {
            string prefix = remote == RemoteSettingsStatus.NotReceived
                ? string.Empty
                : DescribeRemote(remote, remoteSettings) + " ";

            string uid = WorldUid(world);
            if (uid == null)
            {
                return Vanilla(remote, prefix + "World UID is unavailable.");
            }

            string path = FindSidecarPath(uid);
            if (path == null)
            {
                return Vanilla(remote, prefix + "No local sidecar found.");
            }

            try
            {
                if (!WorldSettingsCodec.TryParseByUid(_storage.Read(path), uid, out WorldSettings settings))
                {
                    return Invalid(prefix + $"Sidecar '{path}' has no matching UID.");
                }
                if (!settings.IsGeneratorCompatible)
                {
                    return Invalid(
                        prefix + $"Sidecar '{path}' uses generator version {settings.GeneratorVersionValue}; " +
                        $"this mod supports {WorldSettings.SupportedVersionsText}.");
                }
                return Ok(
                    settings,
                    prefix + $"Loaded local sidecar '{path}': uid={settings.Uid}, " +
                    $"preset={GetDisplayName(settings)}.");
            }
            catch (Exception exception)
            {
                return Invalid(prefix + $"Failed to read sidecar '{path}': {exception.Message}");
            }
        }

        private static string DescribeRemote(RemoteSettingsStatus status, WorldSettings settings)
        {
            return status switch
            {
                RemoteSettingsStatus.NoSnapshot => "Server does not use Open Waters.",
                RemoteSettingsStatus.SeedMismatch => "Server world settings belong to a different seed.",
                RemoteSettingsStatus.InvalidPayload => "Server world settings are incompatible.",
                RemoteSettingsStatus.IncompatibleGenerator =>
                    $"Server uses generator version {settings.GeneratorVersionValue}; " +
                    $"this mod supports {WorldSettings.SupportedVersionsText}.",
                _ => "Server settings are unavailable."
            };
        }

        private static Resolution Ok(WorldSettings settings, string message) =>
            new Resolution(settings, SidecarStatus.Ok, message);

        private static Resolution Vanilla(RemoteSettingsStatus remote, string message) =>
            new Resolution(
                WorldSettings.Vanilla,
                remote == RemoteSettingsStatus.NoSnapshot || remote == RemoteSettingsStatus.NotReceived
                    ? SidecarStatus.Missing
                    : SidecarStatus.Invalid,
                message);

        private static Resolution Invalid(string message) =>
            new Resolution(WorldSettings.Vanilla, SidecarStatus.Invalid, message);

        private static string WorldUid(World world)
        {
            return world != null && world.m_uid != 0L
                ? world.m_uid.ToString(CultureInfo.InvariantCulture)
                : null;
        }

        private static string FindSidecarPath(string uid)
        {
            try
            {
                string root = SaveSystem.GetWorldsSaveRootPath(FileHelpers.FileSource.Local);
                string[] directories = _storage.GetDirectories(root);
                Func<string, string> read = path =>
                    _storage.Exists(path) ? _storage.Read(path) : null;
                Func<string, string> toPath = directory => Path.Combine(directory, SidecarFileName);
                return SidecarLocator.FindByUid(directories, uid, read, toPath);
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning($"[Settings] Sidecar search failed: {exception.Message}");
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

        private readonly struct Resolution
        {
            public Resolution(WorldSettings settings, SidecarStatus status, string message)
            {
                Settings = settings;
                Status = status;
                Message = message;
            }

            public WorldSettings Settings { get; }
            public SidecarStatus Status { get; }
            public string Message { get; }
        }
    }
}
