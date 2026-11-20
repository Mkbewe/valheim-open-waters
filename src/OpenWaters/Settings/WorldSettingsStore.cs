using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;

namespace OpenWaters
{
    internal static class WorldSettingsStore
    {
        private const string SidecarFileName = "OpenWaters.worldSettings";

        private static readonly Dictionary<string, WorldSettings> Pending =
            new Dictionary<string, WorldSettings>();

        private static ISidecarStorage _storage = new FileSidecarStorage();

        internal static void SetStorage(ISidecarStorage storage)
        {
            _storage = storage ?? new FileSidecarStorage();
        }

        public static void Capture(string name, string seed)
        {
            if (!OpenWatersSettings.Enabled || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(seed))
            {
                return;
            }

            Pending[GetKey(name, seed)] = new WorldSettings(
                OpenWatersSettings.IslandAmount,
                OpenWatersSettings.WorldRadius,
                OpenWatersSettings.BiomeLimit,
                OpenWatersSettings.StartBiomeLimit,
                OpenWatersSettings.ExcludeMountains);

            OpenWatersPlugin.Log.LogInfo(
                $"[Settings] Captured for '{name}' seed='{seed}': preset={OpenWatersSettings.IslandAmountName}, radius={OpenWatersSettings.WorldRadius}");
        }

        public static void Persist(World world)
        {
            if (!OpenWatersSettings.Enabled || !CanUseWorldSettings(world))
            {
                return;
            }

            string key = GetKey(world.m_worldName, world.m_seedName);
            if (!Pending.TryGetValue(key, out WorldSettings snapshot))
            {
                return;
            }

            if (WriteSidecar(world, snapshot))
            {
                Pending.Remove(key);
            }
        }

        public static void Apply(World world)
        {
            if (world == null || world.m_menu || world.m_worldName == OpenWatersSettings.PreviewWorldName)
            {
                return;
            }

            if (!OpenWatersSettings.Enabled)
            {
                string sidecarPath = GetSidecarPath(world);
                if (sidecarPath != null && _storage.Exists(sidecarPath))
                {
                    OpenWatersPlugin.Log.LogWarning(
                        $"[Settings] Open Waters disabled; '{world.m_worldName}' has a sidecar, but generation changes are skipped.");
                }
                else
                {
                    OpenWatersPlugin.Log.LogInfo(
                        $"[Settings] Open Waters disabled; skipping world settings for '{world.m_worldName}'.");
                }
                return;
            }

            string key = GetKey(world.m_worldName, world.m_seedName);
            if (Pending.TryGetValue(key, out WorldSettings pending))
            {
                ApplySnapshot(pending);
                if (WriteSidecar(world, pending))
                {
                    Pending.Remove(key);
                }
                OpenWatersPlugin.Log.LogInfo(
                    $"[Settings] Applied pending settings for '{world.m_worldName}' seed='{world.m_seedName}': " +
                    $"preset={OpenWatersSettings.IslandAmountName}, radius={OpenWatersSettings.WorldRadius}");
                return;
            }

            if (TryReadSidecar(world, out WorldSettings saved))
            {
                ApplySnapshot(saved);
                OpenWatersPlugin.Log.LogInfo(
                    $"[Settings] Loaded sidecar for '{world.m_worldName}' seed='{world.m_seedName}': " +
                    $"preset={OpenWatersSettings.IslandAmountName}, radius={OpenWatersSettings.WorldRadius}");
                return;
            }

            ApplySnapshot(WorldSettings.Vanilla);
            OpenWatersPlugin.Log.LogWarning(
                $"[Settings] No usable sidecar for '{world.m_worldName}' seed='{world.m_seedName}'; using Vanilla to avoid applying another world's preset.");
        }

        private static bool CanUseWorldSettings(World world)
        {
            return world != null &&
                   !world.m_menu &&
                   world.m_worldName != OpenWatersSettings.PreviewWorldName &&
                   !string.IsNullOrEmpty(world.m_worldName);
        }

        private static void ApplySnapshot(WorldSettings snapshot)
        {
            OpenWatersSettings.IslandAmount = snapshot.IslandAmount;
            OpenWatersSettings.WorldRadius = snapshot.WorldRadius;
            OpenWatersSettings.BiomeLimit = snapshot.BiomeLimit;
            OpenWatersSettings.StartBiomeLimit = snapshot.StartBiomeLimit;
            OpenWatersSettings.ExcludeMountains = snapshot.ExcludeMountains;
        }

        private static bool TryReadSidecar(World world, out WorldSettings settings)
        {
            settings = null;
            string path = GetSidecarPath(world);
            if (path == null || !_storage.Exists(path))
            {
                return false;
            }

            try
            {
                return WorldSettingsCodec.TryParse(_storage.Read(path), world.m_seedName, out settings);
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning($"[Settings] Failed to read '{path}': {exception.Message}");
                return false;
            }
        }

        private static bool WriteSidecar(World world, WorldSettings settings)
        {
            string path = GetSidecarPath(world);
            if (path == null)
            {
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
                _storage.Write(temporaryPath, WorldSettingsCodec.Serialize(settings, world.m_seedName));
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
                OpenWatersPlugin.Log.LogWarning(
                    $"[Settings] Failed to write sidecar for '{world.m_worldName}': {exception.Message}");
                return false;
            }
        }

        private static string GetSidecarPath(World world)
        {
            try
            {
                string directory = world.GetSaveDirectory(FileHelpers.FileSource.Local);
                return Path.Combine(directory, SidecarFileName);
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning(
                    $"[Settings] Failed to resolve save directory for '{world.m_worldName}': {exception.Message}");
                return null;
            }
        }

        private static string GetKey(string name, string seed)
        {
            return name + "|" + seed;
        }
    }
}
