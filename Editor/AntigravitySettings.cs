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

        [Header("Generate .csproj files for:")]
        [Tooltip("Include embedded packages in solution")]
        public bool includeEmbeddedPackages = true;

        [Tooltip("Include local packages (file: or path references) in solution")]
        public bool includeLocalPackages = true;

        [Tooltip("Include registry packages (from Unity Registry or scoped registries) in solution")]
        public bool includeRegistryPackages = false;

        [Tooltip("Include Git packages in solution")]
        public bool includeGitPackages = false;

        [Tooltip("Include built-in Unity packages (com.unity.*) in solution")]
        public bool includeBuiltinPackages = false;

        [Tooltip("Include local tarball packages in solution")]
        public bool includeLocalTarball = false;

        [Tooltip("Include packages from unknown sources in solution")]
        public bool includeUnknownPackages = false;

        [Tooltip("Include player projects (Assembly-CSharp, etc.) in solution")]
        public bool includePlayerProjects = true;

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
            var settings = AssetDatabase.LoadAssetAtPath<AntigravitySettings>(SETTINGS_PATH);

            if (settings == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:AntigravitySettings");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    settings = AssetDatabase.LoadAssetAtPath<AntigravitySettings>(path);
                }
            }

            if (settings == null)
            {
                settings = CreateInstance<AntigravitySettings>();
                MigrateFromEditorPrefs(settings);
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
            includeEmbeddedPackages = true;
            includeLocalPackages = true;
            includeRegistryPackages = false;
            includeGitPackages = false;
            includeBuiltinPackages = false;
            includeLocalTarball = false;
            includeUnknownPackages = false;
            includePlayerProjects = true;
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
