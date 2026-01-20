using System.IO;
using UnityEditor;
using UnityEngine;

namespace Antigravity.Editor
{
    /// <summary>
    /// Generates workspace settings for Antigravity IDE to hide unnecessary Unity folders.
    /// </summary>
    [InitializeOnLoad]
    public static class AntigravityWorkspaceSetup
    {
        static AntigravityWorkspaceSetup()
        {
            EditorApplication.delayCall += AutoSetupWorkspace;
        }

        private static void AutoSetupWorkspace()
        {
            if (!IsWorkspaceSetup)
            {
                SetupWorkspaceSilent();
            }

            if (!IsOmnisharpSetup)
            {
                SetupOmnisharpSilent();
            }
        }

        private static void SetupWorkspaceSilent()
        {
            try
            {
                if (!Directory.Exists(VscodeFolderPath))
                {
                    Directory.CreateDirectory(VscodeFolderPath);
                }

                File.WriteAllText(SettingsPath, SETTINGS_CONTENT);
            }
            catch (System.Exception)
            {
            }
        }

        private static void SetupOmnisharpSilent()
        {
            try
            {
                File.WriteAllText(OmnisharpPath, OMNISHARP_CONTENT);
            }
            catch (System.Exception)
            {
            }
        }

        private static string ProjectPath => Directory.GetParent(Application.dataPath).FullName;
        private static string VscodeFolderPath => Path.Combine(ProjectPath, ".vscode");
        private static string SettingsPath => Path.Combine(VscodeFolderPath, "settings.json");
        private static string OmnisharpPath => Path.Combine(ProjectPath, "omnisharp.json");


        private const string SETTINGS_CONTENT = @"{
    ""files.exclude"": {
        ""**/.git"": true,
        ""**/.vscode"": true,
        ""**/.DS_Store"": true,
        ""**/Thumbs.db"": true,
        ""Library/"": true,
        ""Logs/"": true,
        ""obj/"": true,
        ""Temp/"": true,
        ""UserSettings/"": true,
        ""ProjectSettings/"": true,
        ""*.csproj"": true,
        ""*.sln"": true,
        ""**/*.meta"": true,
        ""**/*.prefab"": true,
        ""**/*.unity"": true,
        ""**/*.asset"": true,
        ""**/*.mat"": true,
        ""**/*.physicMaterial"": true,
        ""**/*.physicsMaterial2D"": true,
        ""**/*.anim"": true,
        ""**/*.controller"": true,
        ""**/*.overrideController"": true,
        ""**/*.mask"": true,
        ""**/*.lighting"": true,
        ""**/*.giparams"": true,
        ""**/*.renderTexture"": true,
        ""**/*.cubemap"": true,
        ""**/*.flare"": true,
        ""**/*.mixer"": true,
        ""**/*.shadervariants"": true,
        ""**/*.fontsettings"": true,
        ""**/*.guiskin"": true,
        ""**/*.spriteatlasv2"": true,
        ""**/*.spriteatlas"": true,
        ""**/*.terrainlayer"": true,
        ""**/*.brush"": true,
        ""**/*.preset"": true,
        ""**/*.signal"": true,
        ""**/*.playable"": true
    },
    ""files.watcherExclude"": {
        ""**/Library/**"": true,
        ""**/Logs/**"": true,
        ""**/obj/**"": true,
        ""**/Temp/**"": true
    },
    ""search.exclude"": {
        ""**/Library"": true,
        ""**/Logs"": true,
        ""**/obj"": true,
        ""**/Temp"": true,
        ""**/*.prefab"": true,
        ""**/*.unity"": true,
        ""**/*.asset"": true,
        ""**/*.meta"": true
    }
}";

        /// <summary>
        /// Generate workspace settings for Antigravity IDE
        /// </summary>
        [MenuItem("Antigravity/Setup Workspace")]
        public static void SetupWorkspace()
        {
            if (!Directory.Exists(VscodeFolderPath))
            {
                Directory.CreateDirectory(VscodeFolderPath);
            }

            if (File.Exists(SettingsPath))
            {
                if (!EditorUtility.DisplayDialog(
                    "Antigravity Workspace Setup",
                    "settings.json already exists. Do you want to overwrite it with Unity-optimized settings?",
                    "Yes", "No"))
                {
                    return;
                }
            }

            File.WriteAllText(SettingsPath, SETTINGS_CONTENT);

            AssetDatabase.Refresh();

            Debug.Log("[Antigravity] Workspace settings created at: " + SettingsPath);
            EditorUtility.DisplayDialog(
                "Antigravity Workspace Setup",
                "Workspace settings created successfully!\n\nClose and reopen the project in Antigravity to apply changes.",
                "OK");
        }

        /// <summary>
        /// Check if workspace is already set up
        /// </summary>
        public static bool IsWorkspaceSetup => File.Exists(SettingsPath);

        /// <summary>
        /// Check if omnisharp.json is already set up
        /// </summary>
        public static bool IsOmnisharpSetup => File.Exists(OmnisharpPath);

        private const string OMNISHARP_CONTENT = @"{
    ""RoslynExtensionsOptions"": {
        ""enableAnalyzersSupport"": true,
        ""enableImportCompletion"": true
    },
    ""MsBuild"": {
        ""LoadProjectsOnDemand"": false
    },
    ""FormattingOptions"": {
        ""enableEditorConfigSupport"": true,
        ""organizeImports"": true
    },
    ""FileOptions"": {
        ""SystemExcludeSearchPatterns"": [
            ""**/node_modules/**/*"",
            ""**/bin/**/*"",
            ""**/obj/**/*"",
            ""**/.git/**/*"",
            ""**/Library/**/*"",
            ""**/Temp/**/*""
        ]
    },
    ""Sdk"": {
        ""IncludePrereleases"": true
    }
}";
    }
}
