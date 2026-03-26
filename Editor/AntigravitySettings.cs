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
        private const string ExecutablePathKey = "AntigravityEditor.Path";
        private const string ArgumentsFormatKey = "AntigravityEditor.ArgumentsFormat";
        private const string HasInitializedKey = "AntigravityEditor.HasInitialized";
        private const string LegacyExecutablePathKey = "AntigravityEditor_Path";
        private const string LegacyArgumentsKey = "AntigravityEditor_Arguments";
        private const string OldDefaultArgumentsFormat = "$(File):$(Line)";
        private const string DefaultArgumentsFormatValue = "$(ProjectPath) --goto $(File):$(Line):$(Column)";

        private static AntigravitySettings _instance;

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
                if (!Directory.Exists(SETTINGS_FOLDER))
                {
                    Directory.CreateDirectory(SETTINGS_FOLDER);
                }

                AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
                AssetDatabase.SaveAssets();
            }

            MigrateUserPreferences();
            return settings;
        }

        private static void MigrateUserPreferences()
        {
            if (!EditorPrefs.HasKey(ExecutablePathKey) && EditorPrefs.HasKey(LegacyExecutablePathKey))
            {
                EditorPrefs.SetString(ExecutablePathKey, EditorPrefs.GetString(LegacyExecutablePathKey, ""));
            }

            if (!EditorPrefs.HasKey(ArgumentsFormatKey))
            {
                string legacyValue = EditorPrefs.HasKey(LegacyArgumentsKey)
                    ? EditorPrefs.GetString(LegacyArgumentsKey, OldDefaultArgumentsFormat)
                    : DefaultArgumentsFormatValue;

                EditorPrefs.SetString(
                    ArgumentsFormatKey,
                    legacyValue == OldDefaultArgumentsFormat ? DefaultArgumentsFormatValue : legacyValue
                );
            }
        }

        public string executablePath
        {
            get => EditorPrefs.GetString(ExecutablePathKey, "");
            set => EditorPrefs.SetString(ExecutablePathKey, value ?? "");
        }

        public string argumentsFormat
        {
            get => EditorPrefs.GetString(ArgumentsFormatKey, DefaultArgumentsFormatValue);
            set
            {
                string normalizedValue = string.IsNullOrWhiteSpace(value) ? DefaultArgumentsFormatValue : value.Trim();
                EditorPrefs.SetString(ArgumentsFormatKey, normalizedValue);
            }
        }

        public bool hasInitialized
        {
            get => EditorPrefs.GetBool(HasInitializedKey, false);
            set => EditorPrefs.SetBool(HasInitializedKey, value);
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
            argumentsFormat = DefaultArgumentsFormatValue;
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
                AntigravityWorkspaceSetup.EnsureWorkspaceSetup();
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
