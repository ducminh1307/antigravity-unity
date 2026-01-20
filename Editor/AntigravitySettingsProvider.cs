using UnityEditor;
using UnityEngine;

namespace Antigravity.Editor
{
    /// <summary>
    /// Settings Provider for Antigravity IDE in Unity Preferences window
    /// </summary>
    public static class AntigravitySettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateAntigravitySettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/Antigravity", SettingsScope.User)
            {
                label = "Antigravity",

                guiHandler = (searchContext) =>
                {
                    var settings = AntigravitySettings.Instance;

                    EditorGUILayout.Space(10);

                    EditorGUILayout.LabelField("External Script Editor", EditorStyles.boldLabel);
                    settings.executablePath = EditorGUILayout.TextField(
                        new GUIContent("Executable Path", "Path to Antigravity executable"),
                        settings.executablePath
                    );

                    settings.argumentsFormat = EditorGUILayout.TextField(
                        new GUIContent("Arguments Format", "Arguments format. Variables: $(File), $(Line), $(Column)"),
                        settings.argumentsFormat
                    );

                    EditorGUILayout.Space(15);

                    EditorGUILayout.LabelField("Generate .csproj files for:", EditorStyles.boldLabel);

                    EditorGUILayout.HelpBox(
                        "Customize which package types to include in solution generation. " +
                        "Disabling package types can improve IDE performance but may affect Intellisense for those packages.",
                        MessageType.Info
                    );

                    EditorGUILayout.Space(5);

                    settings.includeEmbeddedPackages = EditorGUILayout.Toggle(
                        new GUIContent("Embedded packages", "Packages located in Packages/ folder within project"),
                        settings.includeEmbeddedPackages
                    );

                    settings.includeLocalPackages = EditorGUILayout.Toggle(
                        new GUIContent("Local packages", "Packages referenced by file path or local disk location"),
                        settings.includeLocalPackages
                    );

                    settings.includeRegistryPackages = EditorGUILayout.Toggle(
                        new GUIContent("Registry packages", "Packages from Unity Registry or scoped registries"),
                        settings.includeRegistryPackages
                    );

                    settings.includeGitPackages = EditorGUILayout.Toggle(
                        new GUIContent("Git packages", "Packages installed from Git repositories"),
                        settings.includeGitPackages
                    );

                    settings.includeBuiltinPackages = EditorGUILayout.Toggle(
                        new GUIContent("Built-in packages", "Unity built-in packages (com.unity.*)"),
                        settings.includeBuiltinPackages
                    );

                    settings.includeLocalTarball = EditorGUILayout.Toggle(
                        new GUIContent("Local tarball", "Packages installed from local .tgz files"),
                        settings.includeLocalTarball
                    );

                    settings.includeUnknownPackages = EditorGUILayout.Toggle(
                        new GUIContent("Packages from unknown sources", "Packages that don't match other categories"),
                        settings.includeUnknownPackages
                    );

                    settings.includePlayerProjects = EditorGUILayout.Toggle(
                        new GUIContent("Player projects", "Assembly-CSharp and other player assembly projects"),
                        settings.includePlayerProjects
                    );

                    EditorGUILayout.Space(15);

                    if (GUILayout.Button("Regenerate Project Files", GUILayout.Height(30)))
                    {
                        AntigravitySolutionSync.RegenerateSolution();
                    }

                    EditorGUILayout.Space(5);

                    if (GUILayout.Button("Reset to Defaults"))
                    {
                        if (EditorUtility.DisplayDialog(
                            "Reset Settings",
                            "Are you sure you want to reset all Antigravity settings to defaults?",
                            "Yes", "No"))
                        {
                            settings.ResetToDefaults();
                        }
                    }

                    if (GUI.changed)
                    {
                        settings.Save();
                    }
                },

                keywords = new[] { "Antigravity", "IDE", "External Editor", "csproj", "solution", "packages" }
            };

            return provider;
        }
    }
}
