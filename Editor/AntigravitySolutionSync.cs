using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace Antigravity.Editor
{
    /// <summary>
    /// Handles generation and synchronization of .csproj and .sln files for Antigravity IDE.
    /// </summary>
    public static class AntigravitySolutionSync
    {
        private static string ProjectPath => Directory.GetParent(Application.dataPath).FullName;
        private static string SolutionName => Path.GetFileName(ProjectPath);
        private static string SolutionPath => Path.Combine(ProjectPath, $"{SolutionName}.sln");

        /// <summary>
        /// Regenerate solution and project files
        /// </summary>
        [MenuItem("Antigravity/Regenerate Solution")]
        public static void RegenerateSolution()
        {
            try
            {
                // Use Unity's built-in synchronization first
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();

                // Force project file generation
                var syncVS = Type.GetType("UnityEditor.SyncVS, UnityEditor");
                if (syncVS != null)
                {
                    var syncSolution = syncVS.GetMethod("SyncSolution",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    syncSolution?.Invoke(null, null);
                }

                // Ensure solution file exists
                EnsureSolutionExists();

                Debug.Log("[Antigravity] Solution regenerated successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to regenerate solution: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensure a .sln file exists for the project
        /// </summary>
        private static void EnsureSolutionExists()
        {
            if (File.Exists(SolutionPath))
                return;

            // Find all .csproj files
            string[] csprojFiles = Directory.GetFiles(ProjectPath, "*.csproj", SearchOption.TopDirectoryOnly);

            if (csprojFiles.Length == 0)
            {
                Debug.LogWarning("[Antigravity] No .csproj files found. Open a script to generate them.");
                return;
            }

            GenerateSolutionFile(csprojFiles);
        }

        /// <summary>
        /// Generate a .sln file containing all project references
        /// </summary>
        private static void GenerateSolutionFile(string[] csprojFiles)
        {
            var sb = new StringBuilder();

            // Solution header
            sb.AppendLine();
            sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            sb.AppendLine("# Visual Studio Version 17");
            sb.AppendLine("VisualStudioVersion = 17.0.31903.59");
            sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

            // Add each project
            foreach (string csproj in csprojFiles)
            {
                string projectName = Path.GetFileNameWithoutExtension(csproj);
                string projectGuid = GenerateGuid(projectName);
                string relativePath = Path.GetFileName(csproj);

                sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projectName}\", \"{relativePath}\", \"{{{projectGuid}}}\"");
                sb.AppendLine("EndProject");
            }

            // Global section
            sb.AppendLine("Global");
            sb.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            sb.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
            sb.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
            sb.AppendLine("\tEndGlobalSection");

            sb.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (string csproj in csprojFiles)
            {
                string projectName = Path.GetFileNameWithoutExtension(csproj);
                string projectGuid = GenerateGuid(projectName);

                sb.AppendLine($"\t\t{{{projectGuid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
                sb.AppendLine($"\t\t{{{projectGuid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
                sb.AppendLine($"\t\t{{{projectGuid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
                sb.AppendLine($"\t\t{{{projectGuid}}}.Release|Any CPU.Build.0 = Release|Any CPU");
            }
            sb.AppendLine("\tEndGlobalSection");

            sb.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
            sb.AppendLine("\t\tHideSolutionNode = FALSE");
            sb.AppendLine("\tEndGlobalSection");

            sb.AppendLine("EndGlobal");

            File.WriteAllText(SolutionPath, sb.ToString());
        }

        /// <summary>
        /// Generate a deterministic GUID based on project name
        /// </summary>
        private static string GenerateGuid(string name)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(name));
                return new Guid(hash).ToString().ToUpperInvariant();
            }
        }

        /// <summary>
        /// Called when scripts are recompiled
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnScriptsRecompiled()
        {
            if (AntigravitySettings.Instance.autoRegenerateSolution)
            {
                // Delay to ensure Unity has finished its work
                EditorApplication.delayCall += () =>
                {
                    EnsureSolutionExists();
                };
            }
        }
    }
}
