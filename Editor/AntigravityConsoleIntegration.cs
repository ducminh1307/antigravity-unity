using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Antigravity.Editor
{
    /// <summary>
    /// Integrates Unity Console double-click behavior with Antigravity.
    /// </summary>
    public static class AntigravityConsoleIntegration
    {
        private static readonly Type ConsoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
        private static readonly FieldInfo ActiveTextField = ConsoleWindowType?.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Regex UnityStackTraceRegex = new Regex(
            @"\(at\s+(?<path>.+):(?<line>\d+)\)",
            RegexOptions.Compiled
        );

        private static readonly Regex ManagedStackTraceRegex = new Regex(
            @"\sin\s+(?<path>.+\.cs):line\s+(?<line>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static string ProjectPath => Directory.GetParent(Application.dataPath).FullName;

        [OnOpenAsset(0)]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            if (!IsAntigravitySelected() || !IsConsoleFocused())
            {
                return false;
            }

            if (TryGetConsoleFileLocation(out string filePath, out int lineNumber))
            {
                return AntigravityScriptEditor.OpenFileExternal(filePath, lineNumber, 1);
            }

            string assetPath = AssetDatabase.GetAssetPath(instanceID);
            if (TryNormalizePath(assetPath, out filePath))
            {
                return AntigravityScriptEditor.OpenFileExternal(filePath, Math.Max(1, line), 1);
            }

            return false;
        }

        /// <summary>
        /// Parse a stack trace line to extract file path and line number.
        /// </summary>
        public static bool TryParseStackTrace(string stackTrace, out string filePath, out int lineNumber)
        {
            filePath = null;
            lineNumber = 0;

            if (string.IsNullOrEmpty(stackTrace))
            {
                return false;
            }

            return TryMatchStackTrace(UnityStackTraceRegex, stackTrace, out filePath, out lineNumber) ||
                   TryMatchStackTrace(ManagedStackTraceRegex, stackTrace, out filePath, out lineNumber);
        }

        private static bool IsAntigravitySelected()
        {
            string externalEditorPath = EditorPrefs.GetString("kScriptsDefaultApp", string.Empty);
            return !string.IsNullOrEmpty(externalEditorPath) &&
                   externalEditorPath.IndexOf("antigravity", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsConsoleFocused()
        {
            return ConsoleWindowType != null &&
                   EditorWindow.focusedWindow != null &&
                   EditorWindow.focusedWindow.GetType() == ConsoleWindowType;
        }

        private static bool TryGetConsoleFileLocation(out string filePath, out int lineNumber)
        {
            filePath = null;
            lineNumber = 0;

            if (ActiveTextField == null || EditorWindow.focusedWindow == null)
            {
                return false;
            }

            string activeText = ActiveTextField.GetValue(EditorWindow.focusedWindow) as string;
            return TryParseStackTrace(activeText, out filePath, out lineNumber);
        }

        private static bool TryMatchStackTrace(Regex regex, string stackTrace, out string filePath, out int lineNumber)
        {
            filePath = null;
            lineNumber = 0;

            Match match = regex.Match(stackTrace);
            if (!match.Success || !int.TryParse(match.Groups["line"].Value, out lineNumber))
            {
                return false;
            }

            return TryNormalizePath(match.Groups["path"].Value, out filePath);
        }

        private static bool TryNormalizePath(string rawPath, out string filePath)
        {
            filePath = null;

            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return false;
            }

            string normalizedPath = rawPath.Trim().Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

            if (Path.IsPathRooted(normalizedPath))
            {
                string absolutePath = Path.GetFullPath(normalizedPath);
                if (File.Exists(absolutePath))
                {
                    filePath = absolutePath;
                    return true;
                }

                return false;
            }

            if (normalizedPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith("Packages", StringComparison.OrdinalIgnoreCase))
            {
                string absolutePath = Path.GetFullPath(Path.Combine(ProjectPath, normalizedPath));
                if (File.Exists(absolutePath))
                {
                    filePath = absolutePath;
                    return true;
                }
            }

            return false;
        }
    }
}
