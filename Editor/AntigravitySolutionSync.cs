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
                string[] allCsprojFiles = Directory.GetFiles(ProjectPath, "*.csproj", SearchOption.TopDirectoryOnly);

                if (allCsprojFiles.Length == 0)
                {
                    Debug.LogWarning("[Antigravity] No .csproj files found. Please compile scripts first.");
                    return;
                }

                var settings = AntigravitySettings.Instance;
                var filteredCsprojFiles = new System.Collections.Generic.List<string>();

                foreach (string csprojPath in allCsprojFiles)
                {
                    string projectName = Path.GetFileNameWithoutExtension(csprojPath);
                    PackageType packageType = DeterminePackageType(projectName);

                    if (ShouldIncludePackage(packageType, settings))
                    {
                        filteredCsprojFiles.Add(csprojPath);
                    }
                    else
                    {
                        Debug.Log($"[Antigravity] Skipping {projectName} ({packageType})");
                    }
                }

                if (filteredCsprojFiles.Count == 0)
                {
                    Debug.LogWarning("[Antigravity] No projects match your filter settings. Check Preferences > Antigravity.");
                    return;
                }

                string[] csprojFiles = filteredCsprojFiles.ToArray();

                var sb = new StringBuilder();

                sb.AppendLine();
                sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
                sb.AppendLine("# Visual Studio Version 17");
                sb.AppendLine("VisualStudioVersion = 17.0.31903.59");
                sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

                foreach (string csprojPath in csprojFiles)
                {
                    string projectName = Path.GetFileNameWithoutExtension(csprojPath);
                    string projectGuid = GetProjectGuid(csprojPath);
                    string relativePath = Path.GetFileName(csprojPath);

                    sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projectName}\", \"{relativePath}\", \"{{{projectGuid}}}\"");
                    sb.AppendLine("EndProject");
                }

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

                var guidElement = doc.Root?.Element(ns + "PropertyGroup")?.Element(ns + "ProjectGuid");
                if (guidElement != null)
                {
                    string guid = guidElement.Value.Trim('{', '}');
                    return guid.ToUpperInvariant();
                }
            }
            catch
            {
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

        private enum PackageType
        {
            PlayerProject,
            EmbeddedPackage,
            LocalPackage,
            RegistryPackage,
            GitPackage,
            BuiltinPackage,
            LocalTarball,
            UnknownPackage
        }

        /// <summary>
        /// Determine the package type based on project name and packages-lock.json
        /// </summary>
        private static PackageType DeterminePackageType(string projectName)
        {
            if (projectName.StartsWith("Assembly-CSharp"))
            {
                return PackageType.PlayerProject;
            }

            if (projectName == "AntigravityScriptEditor")
            {
                return PackageType.LocalPackage;
            }

            if (!projectName.Contains("."))
            {
                return PackageType.PlayerProject;
            }

            if (projectName.StartsWith("com.unity.modules."))
            {
                return PackageType.BuiltinPackage;
            }

            if (projectName.StartsWith("com.unity."))
            {
                return PackageType.BuiltinPackage;
            }

            string packagesLockPath = Path.Combine(ProjectPath, "Packages", "packages-lock.json");
            if (File.Exists(packagesLockPath))
            {
                try
                {
                    string lockContent = File.ReadAllText(packagesLockPath);
                    PackageType? detectedType = TryDetectFromPackagesLock(projectName, lockContent);
                    if (detectedType.HasValue)
                    {
                        return detectedType.Value;
                    }
                }
                catch
                {
                }
            }

            string packagesFolder = Path.Combine(ProjectPath, "Packages");
            if (Directory.Exists(packagesFolder))
            {
                string[] packageFolders = Directory.GetDirectories(packagesFolder);
                foreach (string packageFolder in packageFolders)
                {
                    string folderName = Path.GetFileName(packageFolder);

                    if (projectName.StartsWith(folderName, StringComparison.OrdinalIgnoreCase) ||
                        projectName.Replace(".", "").Contains(folderName.Replace(".", ""), StringComparison.OrdinalIgnoreCase))
                    {
                        return PackageType.EmbeddedPackage;
                    }
                }
            }

            return PackageType.PlayerProject;
        }

        /// <summary>
        /// Try to detect package type from packages-lock.json content
        /// </summary>
        private static PackageType? TryDetectFromPackagesLock(string projectName, string lockContent)
        {
            string dependenciesStart = "\"dependencies\"";
            int depsIndex = lockContent.IndexOf(dependenciesStart);
            if (depsIndex == -1)
                return null;

            string searchTerm = projectName.ToLowerInvariant().Replace(".", "").Replace("-", "");

            int searchIndex = depsIndex;
            while (true)
            {
                int packageStart = lockContent.IndexOf("\"com.", searchIndex);
                if (packageStart == -1)
                    break;

                int packageNameEnd = lockContent.IndexOf("\"", packageStart + 1);
                if (packageNameEnd == -1)
                    break;

                string packageName = lockContent.Substring(packageStart + 1, packageNameEnd - packageStart - 1);

                string packageSearchTerm = packageName.ToLowerInvariant().Replace(".", "").Replace("-", "");

                if (packageSearchTerm.Contains(searchTerm) || searchTerm.Contains(packageSearchTerm))
                {
                    int sourceStart = lockContent.IndexOf("\"source\"", packageStart);
                    if (sourceStart != -1 && sourceStart < packageStart + 500)
                    {
                        int sourceValueStart = lockContent.IndexOf("\"", sourceStart + 8);
                        if (sourceValueStart != -1)
                        {
                            sourceValueStart++;
                            int sourceValueEnd = lockContent.IndexOf("\"", sourceValueStart);
                            if (sourceValueEnd != -1)
                            {
                                string sourceValue = lockContent.Substring(sourceValueStart, sourceValueEnd - sourceValueStart);

                                switch (sourceValue.ToLowerInvariant())
                                {
                                    case "git":
                                        return PackageType.GitPackage;
                                    case "local":
                                        return PackageType.LocalPackage;
                                    case "embedded":
                                        return PackageType.EmbeddedPackage;
                                    case "registry":
                                        return PackageType.RegistryPackage;
                                    case "builtin":
                                        return PackageType.BuiltinPackage;
                                }
                            }
                        }
                    }
                }

                searchIndex = packageNameEnd;
            }

            return null;
        }

        /// <summary>
        /// Check if a package should be included based on its type and user settings
        /// </summary>
        private static bool ShouldIncludePackage(PackageType packageType, AntigravitySettings settings)
        {
            switch (packageType)
            {
                case PackageType.PlayerProject:
                    return settings.includePlayerProjects;
                case PackageType.EmbeddedPackage:
                    return settings.includeEmbeddedPackages;
                case PackageType.LocalPackage:
                    return settings.includeLocalPackages;
                case PackageType.RegistryPackage:
                    return settings.includeRegistryPackages;
                case PackageType.GitPackage:
                    return settings.includeGitPackages;
                case PackageType.BuiltinPackage:
                    return settings.includeBuiltinPackages;
                case PackageType.LocalTarball:
                    return settings.includeLocalTarball;
                case PackageType.UnknownPackage:
                    return settings.includeUnknownPackages;
                default:
                    return true;
            }
        }
    }
}
