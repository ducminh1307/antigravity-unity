using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace Antigravity.Editor
{
    /// <summary>
    /// Generates complete .sln solution files for Antigravity IDE.
    /// Solution generation is MANUAL ONLY via the Antigravity > Regenerate Solution menu.
    /// </summary>
    public static class AntigravitySolutionSync
    {
        private static string ProjectPath => Directory.GetParent(Application.dataPath).FullName;
        private static string SolutionName => Path.GetFileName(ProjectPath);
        private static string SolutionPath => Path.Combine(ProjectPath, $"{SolutionName}.sln");


        /// <summary>
        /// Regenerate complete solution file
        /// </summary>
        [MenuItem("Antigravity/Regenerate Solution")]
        public static void RegenerateSolution()
        {
            GenerateCompleteSolution();

            EditorUtility.DisplayDialog(
                "Antigravity",
                $"Solution generated with all {CountCsprojFiles()} projects!\n\nReopen the project in Antigravity to see the changes.",
                "OK");
        }

        /// <summary>
        /// Generate solution silently (for first-time setup, no dialog)
        /// </summary>
        public static void GenerateSolutionSilent()
        {
            GenerateCompleteSolution();
        }

        private static int CountCsprojFiles()
        {
            return Directory.GetFiles(ProjectPath, "*.csproj", SearchOption.TopDirectoryOnly).Length;
        }

        /// <summary>
        /// Generate a complete .sln file containing ALL .csproj files in the project
        /// </summary>
        private static void GenerateCompleteSolution()
        {
            try
            {
                string[] csprojFiles = Directory.GetFiles(ProjectPath, "*.csproj", SearchOption.TopDirectoryOnly);

                if (csprojFiles.Length == 0)
                {
                    Debug.LogWarning("[Antigravity] No .csproj files found. Please compile scripts first.");
                    return;
                }

                var sb = new StringBuilder();

                // Solution header (Visual Studio 2022 format)
                sb.AppendLine();
                sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
                sb.AppendLine("# Visual Studio Version 17");
                sb.AppendLine("VisualStudioVersion = 17.0.31903.59");
                sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

                // Add each project with its GUID from the .csproj file
                foreach (string csprojPath in csprojFiles)
                {
                    string projectName = Path.GetFileNameWithoutExtension(csprojPath);
                    string projectGuid = GetProjectGuid(csprojPath);
                    string relativePath = Path.GetFileName(csprojPath);

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
                foreach (string csprojPath in csprojFiles)
                {
                    string projectGuid = GetProjectGuid(csprojPath);

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
                Debug.Log($"[Antigravity] Solution generated with {csprojFiles.Length} projects: {SolutionPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to generate solution: {ex.Message}");
            }
        }

        /// <summary>
        /// Extract the project GUID from a .csproj file
        /// </summary>
        private static string GetProjectGuid(string csprojPath)
        {
            try
            {
                XDocument doc = XDocument.Load(csprojPath);
                XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;

                // Try to find ProjectGuid element
                var guidElement = doc.Root?.Element(ns + "PropertyGroup")?.Element(ns + "ProjectGuid");
                if (guidElement != null)
                {
                    string guid = guidElement.Value.Trim('{', '}');
                    return guid.ToUpperInvariant();
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            // Generate deterministic GUID from project name
            string projectName = Path.GetFileNameWithoutExtension(csprojPath);
            return GenerateDeterministicGuid(projectName);
        }

        /// <summary>
        /// Generate a deterministic GUID based on project name
        /// </summary>
        private static string GenerateDeterministicGuid(string name)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(name));
                return new Guid(hash).ToString().ToUpperInvariant();
            }
        }
    }
}
