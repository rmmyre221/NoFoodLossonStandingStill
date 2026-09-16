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
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
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
        internal static ConfigEntry<float> BaseHungerLoss;
        internal static ConfigEntry<float> MinimumHungerLoss;
        internal static ConfigEntry<bool> CheckMovement;
        internal static ConfigEntry<bool> CheckHealth;
        internal static ConfigEntry<bool> CheckStamina;
        internal static ConfigEntry<bool> CheckEitr;
        internal static ConfigEntry<bool> PartialLossMode;
        internal static ConfigEntry<bool> RequireStandingStillInPartialMode;
        internal static ConfigEntry<int> MovementWeight;
        internal static ConfigEntry<int> HealthWeight;
        internal static ConfigEntry<int> StaminaWeight;
        internal static ConfigEntry<int> EitrWeight;
        internal static ConfigEntry<bool> UseRestedBonus;
        internal static ConfigEntry<float> RestedMultiplier;
        // cached reciprocal of the total weights; updated when relevant config entries change
        internal static float CachedInverseTotalWeight { get; private set; }
        private static bool _totalWeightDirty = false;

        private static void UpdateCachedInverseTotalWeight()
        {
            int total = ((!RequireStandingStillInPartialMode.Value) ? MovementWeight.Value : 0)
                        + HealthWeight.Value + StaminaWeight.Value + EitrWeight.Value;
            CachedInverseTotalWeight = total <= 0 ? 0f : 1f / (float)total;
        }
        public void Awake()
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            harmony = new Harmony("VedaIanni.NoFoodLoSS.dev");
            harmony.PatchAll(executingAssembly);
            ServerConfigLocked = config("1 - General", "Lock Configuration", value: true, "If on, the configuration is locked and can be changed by server admins only.");
            configSync.AddLockingConfigEntry(ServerConfigLocked);
            UseMod = config("1 - General", "Use Mod", value: true, "Should the mod be used");
            BaseHungerLoss = config("1 - General", "Base Hunger Loss", 1f, "The base amount of hunger lost per tick");
            MinimumHungerLoss = config("1 - General", "Minimum Hunger Loss", 0f, "The minimum amount of hunger lost per tick");
            CheckMovement = config("2 - Conditions", "Check Movement", value: true, "Should the mod check for movement?");
            CheckHealth = config("2 - Conditions", "Check Health", value: true, "Should the mod check for health?");
            CheckStamina = config("2 - Conditions", "Check Stamina", value: true, "Should the mod check for stamina?");
            CheckEitr = config("2 - Conditions", "Check Eitr", value: true, "Should the mod check for eitr?");
            PartialLossMode = config("3 - Partial Loss Mode", "Partial Loss Mode", value: false, "If on, only partial hunger loss will occur, depending on the weights assigned; Disables checks in section 2");
            RequireStandingStillInPartialMode = config("3 - Partial Loss Mode", "Require Standing Still In Partial Mode", value: true, "If on, the player must be standing still for hunger loss to be prevented, in partial hunger loss mode");
            MovementWeight = config("3 - Partial Loss Mode", "Movement Weight", 5, "The weight of movement in determining hunger loss, in partial hunger loss mode");
            HealthWeight = config("3 - Partial Loss Mode", "Health Weight", 5, "The weight of health in determining hunger loss, in partial hunger loss mode");
            StaminaWeight = config("3 - Partial Loss Mode", "Stamina Weight", 5, "The weight of stamina in determining hunger loss, in partial hunger loss mode");
            EitrWeight = config("3 - Partial Loss Mode", "Eitr Weight", 5, "The weight of eitr in determining hunger loss, in partial hunger loss mode");
            UseRestedBonus = config("4 - Rested Bonus", "Use Rested Bonus", value: false, "Should the mod check for the rested bonus?");
            RestedMultiplier = config("4 - Rested Bonus", "Rested Multiplier", 0.8f, "The multiplier to multiply to hunger loss when the player has the rested bonus(0 to 1, higher means less reduction)");
            UpdateCachedInverseTotalWeight();

            MovementWeight.SettingChanged += (_, _) => _totalWeightDirty = true;
            HealthWeight.SettingChanged += (_, _) => _totalWeightDirty = true;
            StaminaWeight.SettingChanged += (_, _) => _totalWeightDirty = true;
            EitrWeight.SettingChanged += (_, _) => _totalWeightDirty = true;
            RequireStandingStillInPartialMode.SettingChanged += (_, _) => _totalWeightDirty = true;
        }

        private void Update()
        {
            if (_totalWeightDirty)
            {
                _totalWeightDirty = false;
                UpdateCachedInverseTotalWeight();
            }
        }
    }
}