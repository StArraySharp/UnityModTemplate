using UnityModManagerNet;

namespace ADOFAIUnityMod
{
    /// <summary>
    /// Empty settings extension point. Add mod-specific settings here when needed.
    /// </summary>
    public sealed class Settings : UnityModManager.ModSettings
    {
        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            // Intentionally empty: the template does not impose a settings UI.
        }

        public void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Save(modEntry);
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public static Settings Load(UnityModManager.ModEntry modEntry)
        {
            return Load<Settings>(modEntry);
        }
    }
}
