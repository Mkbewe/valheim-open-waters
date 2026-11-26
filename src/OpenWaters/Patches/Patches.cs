using System;
using HarmonyLib;

namespace OpenWaters
{
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnWorldStart))]
    internal static class FejdStartupOnWorldStartPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(FejdStartup __instance)
        {
            WorldSettingsStore.ClearRemoteSnapshot();
            if (!OpenWatersSettings.Enabled)
            {
                return true;
            }

            try
            {
                World world = Traverse.Create(__instance).Field("m_world").GetValue<World>();
                if (world == null || world.m_menu)
                {
                    return true;
                }
                if (WorldSettingsStore.ValidateForLoad(world) == SidecarStatus.Invalid)
                {
                    string message = WorldSettingsStore.LastMessage;
                    OpenWatersPlugin.Log.LogError($"[Settings] Blocking world load: {message}");
                    UnifiedPopup.Push(new WarningPopup(
                        "Open Waters: incompatible world",
                        message,
                        UnifiedPopup.Pop,
                        localizeText: false));
                    return false;
                }
            }
            catch (Exception exception)
            {
                string message = $"Could not validate the world settings: {exception.Message}";
                OpenWatersPlugin.Log.LogError($"[Settings] {message}");
                UnifiedPopup.Push(new WarningPopup(
                    "Open Waters: could not validate world",
                    message,
                    UnifiedPopup.Pop,
                    localizeText: false));
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnNewWorldDone))]
    internal static class FejdStartupOnNewWorldDonePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref bool __state)
        {
            __state = false;
            WorldSettingsStore.ClearRemoteSnapshot();
            if (!OpenWatersSettings.Enabled)
            {
                return true;
            }

            FejdStartup startup = FejdStartup.instance;
            if (startup != null && startup.m_newWorldName != null && startup.m_newWorldSeed != null)
            {
                string name = startup.m_newWorldName.text;
                __state = !string.IsNullOrEmpty(name) && !World.HaveWorld(name);
            }
            return true;
        }

        [HarmonyPostfix]
        private static void Postfix(bool __state)
        {
            if (!__state || !OpenWatersSettings.Enabled)
            {
                return;
            }

            FejdStartup startup = FejdStartup.instance;
            if (startup?.m_newWorldName == null || startup.m_newWorldSeed == null)
            {
                return;
            }

            string name = startup.m_newWorldName.text;
            World world = Traverse.Create(startup).Field("m_world").GetValue<World>();
            if (world == null || world.m_uid == 0L ||
                !string.Equals(world.m_worldName, name, StringComparison.Ordinal))
            {
                return;
            }

            string seed = startup.m_newWorldSeed.text;
            if (!WorldSettingsStore.TryWriteForNewWorld(
                    name, seed, world.m_uid, world.m_fileSource, out string error))
            {
                string message =
                    $"Could not save Open Waters settings for world '{name}': {error}. " +
                    "The world will generate vanilla terrain; delete it and try again.";
                OpenWatersPlugin.Log.LogError($"[Settings] {message}");
                UnifiedPopup.Push(new WarningPopup(
                    "Open Waters: could not save world settings",
                    message,
                    UnifiedPopup.Pop,
                    localizeText: false));
            }
        }
    }
}
