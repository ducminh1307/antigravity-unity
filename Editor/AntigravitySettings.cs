using System.IO;
using UnityEditor;
using UnityEngine;

namespace Antigravity.Editor
{
    /// <summary>
    /// ScriptableObject to store Antigravity IDE settings.
    /// Can be shared across team via version control.
    /// </summary>
    public class AntigravitySettings : ScriptableObject
    {
        private const string SETTINGS_PATH = "Assets/Editor/AntigravitySettings.asset";
        private const string SETTINGS_FOLDER = "Assets/Editor";

        private static AntigravitySettings _instance;

        [Header("Executable")]
        [Tooltip("Path to Antigravity executable")]
        public string executablePath = "";

        [Header("Command Format")]
        [Tooltip("Arguments format. Variables: $(File), $(Line), $(Column)")]
        public string argumentsFormat = "$(File):$(Line)";

        [HideInInspector]
        [Tooltip("Has the first-time setup been completed?")]
        public bool hasInitialized = false;

        /// <summary>
        /// Get or create the settings instance
        /// </summary>
        public static AntigravitySettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadOrCreate();
                }
                return _instance;
            }
        }

        private static AntigravitySettings LoadOrCreate()
        {
            // Try to load existing settings
            var settings = AssetDatabase.LoadAssetAtPath<AntigravitySettings>(SETTINGS_PATH);

            if (settings == null)
            {
                // Try to find in project
                string[] guids = AssetDatabase.FindAssets("t:AntigravitySettings");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    settings = AssetDatabase.LoadAssetAtPath<AntigravitySettings>(path);
                }
            }

            if (settings == null)
            {
                // Create new settings
                settings = CreateInstance<AntigravitySettings>();

                // Migrate from EditorPrefs if available
                MigrateFromEditorPrefs(settings);

                // Ensure folder exists
                if (!Directory.Exists(SETTINGS_FOLDER))
                {
                    Directory.CreateDirectory(SETTINGS_FOLDER);
                }

                AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        private static void MigrateFromEditorPrefs(AntigravitySettings settings)
        {
            // Migrate old EditorPrefs settings
            if (EditorPrefs.HasKey("AntigravityEditor_Path"))
            {
                settings.executablePath = EditorPrefs.GetString("AntigravityEditor_Path", "");
            }

            if (EditorPrefs.HasKey("AntigravityEditor_Arguments"))
            {
                settings.argumentsFormat = EditorPrefs.GetString("AntigravityEditor_Arguments", "$(File):$(Line)");
            }
        }

        /// <summary>
        /// Save changes to the settings asset
        /// </summary>
        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Reset settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            executablePath = "";
            argumentsFormat = "$(File):$(Line)";
            hasInitialized = false;
            Save();
        }

        /// <summary>
        /// Perform first-time setup when user selects Antigravity for the first time
        /// </summary>
        public void PerformFirstTimeSetup()
        {
            if (hasInitialized)
                return;

            hasInitialized = true;
            Save();

            // Delay the heavy operations to avoid asset database conflicts
            EditorApplication.delayCall += () =>
            {
                // Run solution regeneration (without dialog for first-time)
                AntigravitySolutionSync.GenerateSolutionSilent();
                Debug.Log("[Antigravity] First-time setup completed. Solution and workspace configured.");
            };
        }

        /// <summary>
        /// Open settings in Inspector
        /// </summary>
        [MenuItem("Antigravity/Settings")]
        public static void OpenSettings()
        {
            Selection.activeObject = Instance;
            EditorGUIUtility.PingObject(Instance);
        }
    }
}
