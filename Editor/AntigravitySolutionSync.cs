using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using PackageSource = UnityEditor.PackageManager.PackageSource;

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
            int generatedProjectCount = GenerateCompleteSolution();

            EditorUtility.DisplayDialog(
                "Antigravity",
                generatedProjectCount > 0
                    ? $"Solution generated with {generatedProjectCount} projects.\n\nReopen the project in Antigravity to see the changes."
                    : "No projects were generated. Check the Console for details.",
                "OK");
        }

        /// <summary>
        /// Generate solution silently (for first-time setup, no dialog)
        /// </summary>
        public static void GenerateSolutionSilent()
        {
            GenerateCompleteSolution();
        }

        [MenuItem("Antigravity/Preview Project Selection")]
        public static void PreviewProjectSelection()
        {
            string report = BuildProjectSelectionReport(out int includedCount, out int skippedCount);
            Debug.Log(report);

            EditorUtility.DisplayDialog(
                "Antigravity",
                $"Project selection preview written to the Console.\n\nIncluded: {includedCount}\nSkipped: {skippedCount}",
                "OK"
            );
        }

        /// <summary>
        /// Generate a complete .sln file containing ALL .csproj files in the project
        /// </summary>
        private static int GenerateCompleteSolution()
        {
            try
            {
                string[] allCsprojFiles = Directory.GetFiles(ProjectPath, "*.csproj", SearchOption.TopDirectoryOnly);

                if (allCsprojFiles.Length == 0)
                {
                    Debug.LogWarning("[Antigravity] No .csproj files found. Please compile scripts first.");
                    return 0;
                }

                var settings = AntigravitySettings.Instance;
                var filteredCsprojFiles = new System.Collections.Generic.List<string>();
                int skippedProjectCount = 0;

                foreach (string csprojPath in allCsprojFiles)
                {
                    ProjectClassification classification = ClassifyProject(csprojPath);

                    if (ShouldIncludePackage(classification.PackageType, settings))
                    {
                        filteredCsprojFiles.Add(csprojPath);
                    }
                    else
                    {
                        skippedProjectCount++;
                        Debug.Log($"[Antigravity] Skipping {classification.ProjectName} ({classification.PackageType}) - {classification.Detail}");
                    }
                }

                if (filteredCsprojFiles.Count == 0)
                {
                    Debug.LogWarning("[Antigravity] No projects match your filter settings. Check Preferences > Antigravity.");
                    return 0;
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
                Debug.Log($"[Antigravity] Solution generated with {csprojFiles.Length} projects, skipped {skippedProjectCount}: {SolutionPath}");
                return csprojFiles.Length;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to generate solution: {ex.Message}");
                return 0;
            }
        }

        private static string BuildProjectSelectionReport(out int includedCount, out int skippedCount)
        {
            includedCount = 0;
            skippedCount = 0;

            string[] allCsprojFiles = Directory.GetFiles(ProjectPath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (allCsprojFiles.Length == 0)
            {
                return "[Antigravity] No .csproj files found. Please compile scripts first.";
            }

            var settings = AntigravitySettings.Instance;
            var includedBuilder = new StringBuilder();
            var skippedBuilder = new StringBuilder();

            foreach (string csprojPath in allCsprojFiles)
            {
                ProjectClassification classification = ClassifyProject(csprojPath);
                bool shouldInclude = ShouldIncludePackage(classification.PackageType, settings);

                if (shouldInclude)
                {
                    includedCount++;
                    includedBuilder.AppendLine($"- {classification.ProjectName} [{classification.PackageType}] - {classification.Detail}");
                }
                else
                {
                    skippedCount++;
                    skippedBuilder.AppendLine($"- {classification.ProjectName} [{classification.PackageType}] - {classification.Detail}");
                }
            }

            var reportBuilder = new StringBuilder();
            reportBuilder.AppendLine("[Antigravity] Project selection preview");
            reportBuilder.AppendLine($"Included: {includedCount}");
            reportBuilder.AppendLine($"Skipped: {skippedCount}");
            reportBuilder.AppendLine();
            reportBuilder.AppendLine("Included projects:");
            reportBuilder.Append(includedCount > 0 ? includedBuilder.ToString() : "- None");
            reportBuilder.AppendLine();
            reportBuilder.AppendLine("Skipped projects:");
            reportBuilder.Append(skippedCount > 0 ? skippedBuilder.ToString() : "- None");

            return reportBuilder.ToString();
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

        private static readonly string[] IgnoredPackageNameTokens =
        {
            "com",
            "unity",
            "editor",
            "runtime",
            "core",
            "tests",
            "test",
            "package",
            "packages",
            "tool",
            "tools",
            "module",
            "modules",
            "sdk",
            "library",
            "plugin",
            "plugins",
            "installer",
            "sample",
            "samples",
            "thirdparty",
            "third-party",
            "nuget"
        };

        private readonly struct ProjectClassification
        {
            public ProjectClassification(string projectName, PackageType packageType, string detail)
            {
                ProjectName = projectName;
                PackageType = packageType;
                Detail = detail;
            }

            public string ProjectName { get; }
            public PackageType PackageType { get; }
            public string Detail { get; }
        }

        /// <summary>
        /// Classify a project and capture how the package type was detected.
        /// </summary>
        private static ProjectClassification ClassifyProject(string csprojPath)
        {
            string projectName = Path.GetFileNameWithoutExtension(csprojPath);

            if (projectName.StartsWith("Assembly-CSharp"))
            {
                return new ProjectClassification(projectName, PackageType.PlayerProject, "Unity player project");
            }

            if (!projectName.Contains("."))
            {
                return new ProjectClassification(projectName, PackageType.PlayerProject, "Assembly name does not look like a package name");
            }

            if (TryGetPackageInfo(csprojPath, projectName, out PackageInfo packageInfo, out string detail))
            {
                return new ProjectClassification(
                    projectName,
                    ConvertPackageSource(packageInfo.source),
                    $"{detail}; package={packageInfo.name}; source={packageInfo.source}"
                );
            }

            return new ProjectClassification(
                projectName,
                PackageType.UnknownPackage,
                "Could not map any source file in the .csproj to a registered package, and name heuristics found no confident match"
            );
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

        private static bool TryGetPackageInfo(string csprojPath, string projectName, out PackageInfo bestMatch, out string detail)
        {
            if (TryGetPackageInfoFromCsproj(csprojPath, out bestMatch, out string assetPath))
            {
                detail = $"Matched source file {assetPath}";
                return true;
            }

            if (TryGetPackageInfoByName(projectName, out bestMatch, out string matchedTerm))
            {
                detail = $"Matched by name heuristic via '{matchedTerm}'";
                return true;
            }

            detail = null;
            return false;
        }

        private static bool TryGetPackageInfoFromCsproj(string csprojPath, out PackageInfo packageInfo, out string matchedAssetPath)
        {
            packageInfo = null;
            matchedAssetPath = null;

            try
            {
                XDocument document = XDocument.Load(csprojPath);
                XNamespace ns = document.Root?.Name.Namespace ?? XNamespace.None;

                foreach (var compileElement in document.Descendants(ns + "Compile"))
                {
                    string includePath = compileElement.Attribute("Include")?.Value;
                    if (string.IsNullOrEmpty(includePath))
                    {
                        continue;
                    }

                    string assetPath = ToUnityAssetPath(includePath);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        continue;
                    }

                    PackageInfo foundPackage = PackageInfo.FindForAssetPath(assetPath);
                    if (foundPackage != null)
                    {
                        packageInfo = foundPackage;
                        matchedAssetPath = assetPath;
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static string ToUnityAssetPath(string includePath)
        {
            if (string.IsNullOrEmpty(includePath))
            {
                return null;
            }

            string normalizedPath = includePath.Replace('\\', '/');

            if (normalizedPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath;
            }

            const string packageCacheMarker = "Library/PackageCache/";
            int packageCacheIndex = normalizedPath.IndexOf(packageCacheMarker, StringComparison.OrdinalIgnoreCase);
            if (packageCacheIndex >= 0)
            {
                string packageRelativePath = normalizedPath.Substring(packageCacheIndex + packageCacheMarker.Length);
                int firstSlashIndex = packageRelativePath.IndexOf('/');

                if (firstSlashIndex > 0)
                {
                    string packageFolder = packageRelativePath.Substring(0, firstSlashIndex);
                    int versionSeparatorIndex = packageFolder.LastIndexOf('@');
                    string packageName = versionSeparatorIndex > 0
                        ? packageFolder.Substring(0, versionSeparatorIndex)
                        : packageFolder;

                    return $"Packages/{packageName}/{packageRelativePath.Substring(firstSlashIndex + 1)}";
                }
            }

            return null;
        }

        private static bool TryGetPackageInfoByName(string projectName, out PackageInfo bestMatch, out string matchedTerm)
        {
            string normalizedProjectName = Normalize(projectName);
            int bestScore = -1;
            bestMatch = null;
            matchedTerm = null;

            foreach (var packageInfo in PackageInfo.GetAllRegisteredPackages())
            {
                int score = GetPackageMatchScore(normalizedProjectName, packageInfo.name, out string currentMatchedTerm);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = packageInfo;
                    matchedTerm = currentMatchedTerm;
                }
            }

            return bestMatch != null;
        }

        private static int GetPackageMatchScore(string normalizedProjectName, string packageName, out string matchedTerm)
        {
            string normalizedPackageName = Normalize(packageName);
            matchedTerm = null;
            if (string.IsNullOrEmpty(normalizedProjectName) || string.IsNullOrEmpty(normalizedPackageName))
            {
                return -1;
            }

            if (normalizedProjectName.Equals(normalizedPackageName, StringComparison.OrdinalIgnoreCase))
            {
                matchedTerm = packageName;
                return normalizedPackageName.Length + 1000;
            }

            if (normalizedProjectName.StartsWith(normalizedPackageName, StringComparison.OrdinalIgnoreCase))
            {
                matchedTerm = packageName;
                return normalizedPackageName.Length;
            }

            int bestTokenScore = -1;
            string bestToken = null;
            foreach (string token in packageName.Split('.'))
            {
                string trimmedToken = token.Trim();
                if (trimmedToken.Length < 4)
                {
                    continue;
                }

                bool isIgnoredToken = Array.Exists(
                    IgnoredPackageNameTokens,
                    ignoredToken => ignoredToken.Equals(trimmedToken, StringComparison.OrdinalIgnoreCase)
                );

                if (isIgnoredToken)
                {
                    continue;
                }

                string normalizedToken = Normalize(trimmedToken);
                if (normalizedProjectName.Contains(normalizedToken, StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedToken.Length > bestTokenScore)
                    {
                        bestTokenScore = normalizedToken.Length;
                        bestToken = trimmedToken;
                    }
                }
            }

            matchedTerm = bestToken;
            return bestTokenScore;
        }

        private static string Normalize(string value)
        {
            return value.Replace(".", "").Replace("-", "").ToLowerInvariant();
        }

        private static PackageType ConvertPackageSource(PackageSource source)
        {
            switch (source)
            {
                case PackageSource.Embedded:
                    return PackageType.EmbeddedPackage;
                case PackageSource.Local:
                    return PackageType.LocalPackage;
                case PackageSource.Registry:
                    return PackageType.RegistryPackage;
                case PackageSource.Git:
                    return PackageType.GitPackage;
                case PackageSource.BuiltIn:
                    return PackageType.BuiltinPackage;
                case PackageSource.LocalTarball:
                    return PackageType.LocalTarball;
                default:
                    return PackageType.UnknownPackage;
            }
        }
    }
}
