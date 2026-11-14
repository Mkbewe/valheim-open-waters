using BepInEx;
using BepInEx.Logging;

namespace OpenWaters
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    public class OpenWatersPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "valheim.openwaters";
        public const string PluginName = "Open Waters";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("Open Waters plugin loaded.");
        }
    }
}
