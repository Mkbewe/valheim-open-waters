using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

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

        private static ISidecarStorage _localStorage = new FileSidecarStorage();
        private static ISidecarStorage _cloudStorage = new CloudSidecarStorage();

        public static SidecarStatus LastStatus { get; private set; } = SidecarStatus.Missing;
        public static string LastMessage { get; private set; } = string.Empty;
        private static readonly RemoteSettingsState _remote = new RemoteSettingsState();

        internal static void SetStorage(ISidecarStorage storage)
        {
            _localStorage = storage ?? new FileSidecarStorage();
            _cloudStorage = new CloudSidecarStorage();
        }

        private static FileHelpers.FileSource NormalizeSource(FileHelpers.FileSource source)
        {
            if (source == FileHelpers.FileSource.Local)
            {
                return FileHelpers.FileSource.Local;
            }
            return FileHelpers.CloudStorageSupportedAndEnabled
                ? FileHelpers.FileSource.Cloud
                : FileHelpers.FileSource.Local;
        }

        private static ISidecarStorage GetStorage(FileHelpers.FileSource source)
        {
            return NormalizeSource(source) == FileHelpers.FileSource.Cloud
                ? _cloudStorage
                : _localStorage;
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

        public static bool TryWriteForNewWorld(
            string name, string seed, long uid, FileHelpers.FileSource source, out string error)
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

            source = NormalizeSource(source);
            string path = GetSidecarPath(name, source);
            if (path == null)
            {
                error = "Could not resolve the save directory.";
                return false;
            }

            string uidText = uid.ToString(CultureInfo.InvariantCulture);
            string text = WorldSettingsCodec.Serialize(FromCurrentSettings(seed, uidText), seed);

            if (source == FileHelpers.FileSource.Cloud &&
                (!FileHelpers.CloudStorageSupportedAndEnabled ||
                 FileHelpers.OperationExceedsCloudCapacity(
                     FileHelpers.FileSource.Cloud,
                     (ulong)Encoding.UTF8.GetByteCount(text), 1)))
            {
                OpenWatersPlugin.Log.LogWarning(
                    "[Settings] Cloud storage unavailable or full; writing the sidecar locally.");
                source = FileHelpers.FileSource.Local;
                path = GetSidecarPath(name, source);
                if (path == null)
                {
                    error = "Could not resolve the local save directory.";
                    return false;
                }
            }

            try
            {
                WriteSidecar(path, source, text);
                OpenWatersPlugin.Log.LogInfo(
                    $"[Settings] Saved sidecar '{path}' with uid '{uidText}' in {source} storage.");
                return true;
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning(
                    $"[Settings] Could not write sidecar to {source} storage: {exception.Message}");
                if (source == FileHelpers.FileSource.Cloud)
                {
                    string localPath = GetSidecarPath(name, FileHelpers.FileSource.Local);
                    if (localPath != null)
                    {
                        try
                        {
                            WriteSidecar(localPath, FileHelpers.FileSource.Local, text);
                            OpenWatersPlugin.Log.LogWarning(
                                $"[Settings] Sidecar written locally instead: '{localPath}'.");
                            path = localPath;
                            return true;
                        }
                        catch (Exception localException)
                        {
                            OpenWatersPlugin.Log.LogError(
                                $"[Settings] Local sidecar fallback failed: {localException.Message}");
                            error = localException.Message;
                            return false;
                        }
                    }
                }
                error = exception.Message;
                return false;
            }
        }

        private static void WriteSidecar(string path, FileHelpers.FileSource source, string text)
        {
            ISidecarStorage storage = GetStorage(source);
            if (source == FileHelpers.FileSource.Cloud)
            {
                storage.Write(path, text);
                return;
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string temporaryPath = path + ".tmp";
            storage.Write(temporaryPath, text);
            if (storage.Exists(path))
            {
                storage.Delete(path);
            }
            storage.Move(temporaryPath, path);
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

        private static void TryMigrateSidecar(
            string worldName, string path, FileHelpers.FileSource from,
            FileHelpers.FileSource to, string uid)
        {
            to = NormalizeSource(to);
            try
            {
                string text = GetStorage(from).Read(path);
                if (to == FileHelpers.FileSource.Cloud &&
                    (!FileHelpers.CloudStorageSupportedAndEnabled ||
                     FileHelpers.OperationExceedsCloudCapacity(
                         FileHelpers.FileSource.Cloud,
                         (ulong)Encoding.UTF8.GetByteCount(text), 1)))
                {
                    OpenWatersPlugin.Log.LogWarning(
                        "[Settings] Cloud unavailable or full during sidecar migration; using local storage.");
                    to = FileHelpers.FileSource.Local;
                }

                string destination = GetSidecarPath(worldName, to);
                if (string.IsNullOrEmpty(destination))
                {
                    return;
                }
                if (to == FileHelpers.FileSource.Local)
                {
                    string directory = Path.GetDirectoryName(destination);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                bool copied = SidecarCopier.TryCopy(
                    GetStorage(from), path, GetStorage(to), destination, uid);
                if (copied)
                {
                    OpenWatersPlugin.Log.LogInfo(
                        $"[Settings] Copied sidecar from {from} to {to} storage for uid '{uid}'.");
                }
                else
                {
                    OpenWatersPlugin.Log.LogWarning(
                        $"[Settings] Sidecar copy from {from} to {to} failed for uid '{uid}'.");
                }
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning(
                    $"[Settings] Could not copy sidecar to {to} storage: {exception.Message}");
            }
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

            string path = FindSidecarPath(world, uid, out FileHelpers.FileSource foundSource);
            if (path == null)
            {
                return Vanilla(remote, prefix + "No sidecar found.");
            }

            FileHelpers.FileSource primary = NormalizeSource(world.m_fileSource);
            if (foundSource != primary)
            {
                TryMigrateSidecar(world.m_worldName, path, foundSource, primary, uid);
            }

            try
            {
                if (!WorldSettingsCodec.TryParseByUid(
                        GetStorage(foundSource).Read(path), uid, out WorldSettings settings))
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

        private static string FindSidecarPath(
            World world, string uid, out FileHelpers.FileSource foundSource)
        {
            FileHelpers.FileSource primary = NormalizeSource(
                world != null ? world.m_fileSource : FileHelpers.FileSource.Local);
            FileHelpers.FileSource fallback = primary == FileHelpers.FileSource.Local
                ? FileHelpers.FileSource.Cloud
                : FileHelpers.FileSource.Local;
            foundSource = primary;

            HashSet<string> searchedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (FileHelpers.FileSource source in new[] { primary, fallback })
            {
                FileHelpers.FileSource normalized = NormalizeSource(source);
                if (normalized == FileHelpers.FileSource.Cloud &&
                    !FileHelpers.CloudStorageSupportedAndEnabled)
                {
                    continue;
                }
                string root = SaveSystem.GetWorldsSaveRootPath(normalized);
                if (string.IsNullOrEmpty(root) || !searchedRoots.Add(root))
                {
                    continue;
                }
                try
                {
                    ISidecarStorage storage = GetStorage(normalized);
                    string[] directories = storage.GetDirectories(root);
                    Func<string, string> read = path =>
                        storage.Exists(path) ? storage.Read(path) : null;
                    Func<string, string> toPath = directory => Path.Combine(directory, SidecarFileName);
                    string found = SidecarLocator.FindByUid(directories, uid, read, toPath);
                    if (found != null)
                    {
                        foundSource = normalized;
                        OpenWatersPlugin.Log.LogInfo(
                            $"[Settings] Sidecar found in {normalized} save root: '{found}'.");
                        return found;
                    }
                }
                catch (Exception exception)
                {
                    OpenWatersPlugin.Log.LogWarning(
                        $"[Settings] Sidecar search failed in {normalized} root: {exception.Message}");
                }
            }
            return null;
        }

        private static string GetSidecarPath(string worldName, FileHelpers.FileSource source)
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
                string root = SaveSystem.GetWorldsSaveRootPath(source);
                if (string.IsNullOrEmpty(root))
                {
                    return null;
                }
                return Path.Combine(root, worldName, SidecarFileName);
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning(
                    $"[Settings] Failed to resolve {source} save directory for '{worldName}': {exception.Message}");
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
