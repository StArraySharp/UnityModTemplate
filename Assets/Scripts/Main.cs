using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace ADOFAIUnityMod
{
    /// <summary>
    /// The UnityModManager entry point and the mod lifecycle coordinator.
    /// Put game patches in Patches.cs and load custom assets through ResourceLoader.
    /// </summary>
    public static class Main
    {
        public static UnityModManager.ModEntry Mod { get; private set; }
        public static Harmony Harmony { get; private set; }
        public static Settings Settings { get; private set; }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;
            Settings = Settings.Load(modEntry);

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = Settings.OnGUI;
            modEntry.OnSaveGUI = Settings.OnSaveGUI;

            Harmony = new Harmony(modEntry.Info.Id);
            modEntry.Logger.Log("Mod loaded.");
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                Harmony.PatchAll(Assembly.GetExecutingAssembly());
                if (!ResourceLoader.LoadAll(modEntry.Path))
                {
                    Harmony.UnpatchAll(Harmony.Id);
                    modEntry.Logger.Error("Mod could not be enabled because one or more AssetBundles failed to load.");
                    return false;
                }

                modEntry.Logger.Log("Mod enabled.");
                return true;
            }

            Harmony.UnpatchAll(Harmony.Id);
            ResourceLoader.UnloadAll();
            modEntry.Logger.Log("Mod disabled.");
            return true;
        }
    }
}
