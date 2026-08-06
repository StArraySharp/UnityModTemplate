#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using ThunderKit.Core.Pipelines;
using UnityEditor;
using UnityEngine;

namespace ADOFAIUnityMod.Editor
{
    /// <summary>
    /// Runs the selected ThunderKit pipeline and deploys only this mod's outputs.
    /// ThunderKit remains responsible for importing game assemblies and building assets.
    /// </summary>
    public sealed class BuildMod : EditorWindow
    {
        private const string ModsPathPreference = "ADOFAIUnityMod.BuildMod.ModsPath";
        private const string PipelinePreference = "ADOFAIUnityMod.BuildMod.Pipeline";
        private const string ThunderKitSettingsPath = "Assets/ThunderKitSettings/ThunderKitSettings.asset";

        private string modsPath;
        private Pipeline selectedPipeline;
        private bool isBuilding;

        [MenuItem("Tools/Build Mod")]
        public static void ShowWindow()
        {
            GetWindow<BuildMod>("Build Mod");
        }

        private void OnEnable()
        {
            modsPath = EditorPrefs.GetString(ModsPathPreference, GetDefaultModsPath());
            string pipelinePath = EditorPrefs.GetString(PipelinePreference, string.Empty);
            if (!string.IsNullOrEmpty(pipelinePath))
            {
                selectedPipeline = AssetDatabase.LoadAssetAtPath<Pipeline>(pipelinePath);
            }
        }

        private void OnDisable()
        {
            SavePreferences();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Build and deploy the Mod through ThunderKit.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                modsPath = EditorGUILayout.TextField("Mods Directory", modsPath);
                if (GUILayout.Button("Browse", GUILayout.Width(70)))
                {
                    string chosenPath = EditorUtility.OpenFolderPanel("Select ADOFAI Mods Directory", modsPath, string.Empty);
                    if (!string.IsNullOrEmpty(chosenPath))
                    {
                        modsPath = chosenPath;
                        SavePreferences();
                    }
                }
            }

            selectedPipeline = (Pipeline)EditorGUILayout.ObjectField(
                "ThunderKit Pipeline",
                selectedPipeline,
                typeof(Pipeline),
                false);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "The pipeline imports the local game package and builds scenes.assets, resources.assets, and the mod assembly. This window does not copy game DLLs or start the game.",
                MessageType.Info);

            EditorGUI.BeginDisabledGroup(isBuilding || string.IsNullOrWhiteSpace(modsPath) || selectedPipeline == null);
            if (GUILayout.Button(isBuilding ? "Building..." : "Build Mod", GUILayout.Height(32)))
            {
                _ = BuildModFunction();
            }
            EditorGUI.EndDisabledGroup();

            if (!string.IsNullOrWhiteSpace(modsPath))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Output", modsPath);
            }

            if (selectedPipeline != null)
            {
                EditorGUILayout.LabelField("Pipeline", selectedPipeline.name);
            }
        }

        private async Task BuildModFunction()
        {
            if (isBuilding)
            {
                return;
            }

            isBuilding = true;
            try
            {
                SavePreferences();
                ValidateConfiguration();

                await selectedPipeline.Execute();

                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string thunderKitOutput = Path.Combine(projectRoot, "ThunderKit");
                string stagingPath = Path.Combine(thunderKitOutput, "AssetBundleStaging");
                string librariesPath = Path.Combine(thunderKitOutput, "Libraries");
                string modId = selectedPipeline.manifest.Identity.Name;

                string infoPath = Path.Combine(Application.dataPath, "Info.json");
                string assemblyPath = FindExactFile(librariesPath, modId + ".dll");
                string scenesBundlePath = FindExactFile(stagingPath, "scenes.assets");
                string resourcesBundlePath = FindExactFile(stagingPath, "resources.assets");

                RequireFile(infoPath, "Info.json");
                RequireFile(assemblyPath, modId + ".dll");
                RequireFile(scenesBundlePath, "scenes.assets");
                RequireFile(resourcesBundlePath, "resources.assets");

                string modOutputPath = Path.Combine(modsPath, modId);
                Directory.CreateDirectory(modOutputPath);

                File.Copy(infoPath, Path.Combine(modOutputPath, "Info.json"), true);
                File.Copy(assemblyPath, Path.Combine(modOutputPath, modId + ".dll"), true);
                File.Copy(scenesBundlePath, Path.Combine(modOutputPath, "scenes.assets"), true);
                File.Copy(resourcesBundlePath, Path.Combine(modOutputPath, "resources.assets"), true);

                ValidateOutput(modOutputPath, modId);
                Debug.Log("ADOFAI Mod build succeeded: " + modOutputPath);
                EditorUtility.DisplayDialog("Build Mod", "Build succeeded.\n\nOutput:\n" + modOutputPath, "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Build Mod Failed", exception.Message, "OK");
            }
            finally
            {
                isBuilding = false;
                Repaint();
            }
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(modsPath))
            {
                throw new InvalidOperationException("Choose an ADOFAI Mods directory before building.");
            }

            if (selectedPipeline == null)
            {
                throw new InvalidOperationException("Choose a ThunderKit Pipeline before building.");
            }

            if (selectedPipeline.manifest == null || selectedPipeline.manifest.Identity == null)
            {
                throw new InvalidOperationException("The selected ThunderKit Pipeline has no Manifest identity.");
            }

            string modId = selectedPipeline.manifest.Identity.Name;
            if (string.IsNullOrWhiteSpace(modId))
            {
                throw new InvalidOperationException("The ThunderKit Manifest name is empty.");
            }
        }

        private static string FindExactFile(string rootPath, string fileName)
        {
            if (!Directory.Exists(rootPath))
            {
                return null;
            }

            string[] matches = Directory.GetFiles(rootPath, fileName, SearchOption.AllDirectories);
            return matches.Length == 1 ? matches[0] : null;
        }

        private static void RequireFile(string path, string description)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                throw new FileNotFoundException(
                    "ThunderKit did not produce the required output: " + description +
                    ". Run the ThunderKit import/build steps and try again.",
                    path);
            }
        }

        private static void ValidateOutput(string outputPath, string modId)
        {
            RequireFile(Path.Combine(outputPath, modId + ".dll"), modId + ".dll");
            RequireFile(Path.Combine(outputPath, "Info.json"), "Info.json");
            RequireFile(Path.Combine(outputPath, "scenes.assets"), "scenes.assets");
            RequireFile(Path.Combine(outputPath, "resources.assets"), "resources.assets");
        }

        private static string GetDefaultModsPath()
        {
            UnityEngine.Object settingsAsset = AssetDatabase.LoadMainAssetAtPath(ThunderKitSettingsPath);
            if (settingsAsset == null)
            {
                return string.Empty;
            }

            SerializedObject settings = new SerializedObject(settingsAsset);
            SerializedProperty gamePath = settings.FindProperty("GamePath");
            if (gamePath == null || string.IsNullOrWhiteSpace(gamePath.stringValue))
            {
                return string.Empty;
            }

            return Path.Combine(gamePath.stringValue, "Mods");
        }

        private void SavePreferences()
        {
            EditorPrefs.SetString(ModsPathPreference, modsPath ?? string.Empty);
            if (selectedPipeline == null)
            {
                EditorPrefs.DeleteKey(PipelinePreference);
                return;
            }

            EditorPrefs.SetString(PipelinePreference, AssetDatabase.GetAssetPath(selectedPipeline));
        }
    }
}
#endif
