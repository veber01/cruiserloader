using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace CruiserLoader
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class CruiserLoader : BaseUnityPlugin
    {
        public static CruiserLoader Instance { get; private set; } = null!;
        internal static ManualLogSource Log { get; private set; } = null!;
        internal static ConfigFile ItemZoneConfig { get; private set; } = null!;
        internal static BepInEx.Configuration.ConfigEntry<bool> ExperimentalClientUse { get; private set; } = null!;
        internal static BepInEx.Configuration.ConfigEntry<bool> AutoRestockEnabled { get; private set; } = null!;

        private void Awake()
        {
            Instance = this;
            Log = base.Logger;
            ItemZoneConfig = new ConfigFile(Paths.ConfigPath + "/CruiserLoader/Items.cfg", true);

            ExperimentalClientUse = Config.Bind(
                "Experimental",
                "ClientSideUse",
                false,
                new ConfigDescription("If true, clients may execute .cload commands locally when the host does not have the mod installed. EXPERIMENTAL.")
            );

            AutoRestockEnabled = Config.Bind(
                "Restock",
                "Enabled",
                false,
                new ConfigDescription("If true, the host will automatically restock the cruiser after the end of each day. (host only)")
            );

            new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();
            Log.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} loaded successfully!");
        }

        public static void DisplayTip(string title, string msg, bool isWarning = false)
        {
            HUDManager.Instance.DisplayTip(title, msg, isWarning, false, "LC_Tip1");
        }
    }
}