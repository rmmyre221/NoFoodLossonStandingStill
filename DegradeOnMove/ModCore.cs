using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using System.Reflection;

namespace NoFoodLoSS
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class NoFoodLoSSMod : BaseUnityPlugin
    {
        private const string ModName = "No Food Loss on Standing Still";
        private const string ModVersion = "0.1.0";
        private const string ModGUID = "VedaIanni.NoFoodLoSS.dev";
        private static Harmony harmony = null!;
        public static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        ConfigSync configSync = new(ModGUID) 
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion};
        internal static ConfigEntry<bool> ServerConfigLocked = null!;
        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }
        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

        internal static ConfigEntry<bool> UseMod;
        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony = new(ModGUID);
            harmony.PatchAll(assembly);
            ServerConfigLocked = config("1 - General", "Lock Configuration", true, "If on, the configuration is locked and can be changed by server admins only.");
            configSync.AddLockingConfigEntry(ServerConfigLocked);

            UseMod = config("1 - General", "Use Mod", true, "Should the mod be used");
        }
    }
}
