using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using HarmonyLib;

namespace OpenWaters
{
    internal static class WorldSettingsStore
    {
        private const string SidecarFileName = "OpenWaters.settings";
        private const int SidecarVersion = 1;

        private sealed class Snapshot
        {
            public int IslandAmount;
            public int WorldRadius;
            public int BiomeLimit;
            public int StartBiomeLimit;
            public bool ExcludeMountains;
        }

        private static readonly Dictionary<string, Snapshot> Pending = new Dictionary<string, Snapshot>();

        public static void Capture(string name, string seed)
        {
            if (!OpenWatersSettings.Enabled || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(seed))
            {
                return;
            }

            Pending[GetKey(name, seed)] = new Snapshot
            {
                IslandAmount = OpenWatersSettings.IslandAmount,
                WorldRadius = OpenWatersSettings.WorldRadius,
                BiomeLimit = OpenWatersSettings.BiomeLimit,
                StartBiomeLimit = OpenWatersSettings.StartBiomeLimit,
                ExcludeMountains = OpenWatersSettings.ExcludeMountains
            };

            OpenWatersPlugin.Log.LogInfo(
                $"[Settings] Captured for '{name}' seed='{seed}': preset={OpenWatersSettings.IslandAmountName}, radius={OpenWatersSettings.WorldRadius}");
        }

        public static void Persist(World world)
        {
            if (!OpenWatersSettings.Enabled || !CanUseWorldSettings(world))
            {
                return;
            }

            if (!Pending.TryGetValue(GetKey(world.m_worldName, world.m_seedName), out Snapshot snapshot))
            {
                return;
            }

            WriteSidecar(world, snapshot);
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
                if (sidecarPath != null && File.Exists(sidecarPath))
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
            if (Pending.TryGetValue(key, out Snapshot pending))
            {
                ApplySnapshot(pending);
                WriteSidecar(world, pending);
                OpenWatersPlugin.Log.LogInfo(
                    $"[Settings] Applied pending settings for '{world.m_worldName}' seed='{world.m_seedName}': " +
                    $"preset={OpenWatersSettings.IslandAmountName}, radius={OpenWatersSettings.WorldRadius}");
                return;
            }

            if (TryReadSidecar(world, out Snapshot saved))
            {
                ApplySnapshot(saved);
                OpenWatersPlugin.Log.LogInfo(
                    $"[Settings] Loaded sidecar for '{world.m_worldName}' seed='{world.m_seedName}': " +
                    $"preset={OpenWatersSettings.IslandAmountName}, radius={OpenWatersSettings.WorldRadius}");
                return;
            }

            ResetToVanilla();
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

        private static void ApplySnapshot(Snapshot snapshot)
        {
            OpenWatersSettings.IslandAmount = snapshot.IslandAmount;
            OpenWatersSettings.WorldRadius = snapshot.WorldRadius;
            OpenWatersSettings.BiomeLimit = snapshot.BiomeLimit;
            OpenWatersSettings.StartBiomeLimit = snapshot.StartBiomeLimit;
            OpenWatersSettings.ExcludeMountains = snapshot.ExcludeMountains;
        }

        private static void ResetToVanilla()
        {
            OpenWatersSettings.IslandAmount = OpenWatersSettings.VanillaIslandAmount;
            OpenWatersSettings.WorldRadius = OpenWatersSettings.DefaultWorldRadius;
            OpenWatersSettings.BiomeLimit = 0;
            OpenWatersSettings.StartBiomeLimit = 0;
            OpenWatersSettings.ExcludeMountains = true;
        }

        private static bool TryReadSidecar(World world, out Snapshot snapshot)
        {
            snapshot = null;
            string path = GetSidecarPath(world);
            if (path == null || !File.Exists(path))
            {
                return false;
            }

            try
            {
                Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string rawLine in File.ReadAllLines(path))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
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

                if (!TryGetInt(values, "version", out int version) || version != SidecarVersion ||
                    !TryGetString(values, "seed", out string seed) || seed != world.m_seedName ||
                    !TryGetInt(values, "islandAmount", out int islandAmount) ||
                    !TryGetInt(values, "worldRadius", out int worldRadius) ||
                    !TryGetInt(values, "biomeLimit", out int biomeLimit) ||
                    !TryGetInt(values, "startBiomeLimit", out int startBiomeLimit) ||
                    !TryGetBool(values, "excludeMountains", out bool excludeMountains))
                {
                    return false;
                }

                if (islandAmount < OpenWatersSettings.FewIslands ||
                    islandAmount > OpenWatersSettings.VanillaIslandAmount ||
                    worldRadius < OpenWatersSettings.MinWorldRadius ||
                    worldRadius > OpenWatersSettings.MaxWorldRadius)
                {
                    return false;
                }

                snapshot = new Snapshot
                {
                    IslandAmount = islandAmount,
                    WorldRadius = worldRadius,
                    BiomeLimit = biomeLimit,
                    StartBiomeLimit = startBiomeLimit,
                    ExcludeMountains = excludeMountains
                };
                return true;
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning($"[Settings] Failed to read '{path}': {exception.Message}");
                return false;
            }
        }

        private static void WriteSidecar(World world, Snapshot snapshot)
        {
            string path = GetSidecarPath(world);
            if (path == null)
            {
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                StringBuilder text = new StringBuilder();
                text.AppendLine("# Open Waters world settings");
                text.AppendLine($"version={SidecarVersion}");
                text.AppendLine($"seed={world.m_seedName}");
                text.AppendLine($"islandAmount={snapshot.IslandAmount}");
                text.AppendLine($"worldRadius={snapshot.WorldRadius}");
                text.AppendLine($"biomeLimit={snapshot.BiomeLimit}");
                text.AppendLine($"startBiomeLimit={snapshot.StartBiomeLimit}");
                text.AppendLine($"excludeMountains={snapshot.ExcludeMountains.ToString().ToLowerInvariant()}");

                string temporaryPath = path + ".tmp";
                File.WriteAllText(temporaryPath, text.ToString());
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(temporaryPath, path);

                OpenWatersPlugin.Log.LogInfo($"[Settings] Saved sidecar '{path}'.");
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning($"[Settings] Failed to write sidecar for '{world.m_worldName}': {exception.Message}");
            }
        }

        private static string GetSidecarPath(World world)
        {
            try
            {
                string directory = world.GetSaveDirectory(world.m_fileSource);
                return Path.Combine(directory, SidecarFileName);
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning($"[Settings] Failed to resolve save directory for '{world.m_worldName}': {exception.Message}");
                return null;
            }
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

        private static string GetKey(string name, string seed)
        {
            return name + "|" + seed;
        }
    }
}
