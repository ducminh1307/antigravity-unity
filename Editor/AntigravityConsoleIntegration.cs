using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Antigravity.Editor
{
    /// <summary>
    /// Integrates with Unity Console to handle double-click on errors/logs.
    /// Opens the file in Antigravity IDE at the correct line.
    /// </summary>
    [InitializeOnLoad]
    public static class AntigravityConsoleIntegration
    {
        // Regex to match file paths and line numbers in stack traces
        // Matches patterns like: "at ClassName.Method() in C:\path\file.cs:line 42"
        // Or: "(at Assets/Scripts/MyScript.cs:42)"
        private static readonly Regex StackTraceRegex = new Regex(
            @"(?:at\s+.+\s+in\s+)?(?<path>[A-Za-z]:[\\/][^\s:]+\.cs|Assets[\\/][^\s:]+\.cs):(?:line\s+)?(?<line>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static readonly Regex SimplePathRegex = new Regex(
            @"\(at\s+(?<path>[^:]+):(?<line>\d+)\)",
            RegexOptions.Compiled
        );

        static AntigravityConsoleIntegration()
        {
            // Hook into Unity's console window click callback
            RegisterConsoleCallback();
        }

        private static void RegisterConsoleCallback()
        {
            try
            {
                // Get the ConsoleWindow type
                var consoleWindowType = Type.GetType("UnityEditor.ConsoleWindow, UnityEditor");
                if (consoleWindowType == null) return;

                // Try to find and hook the double-click callback
                // This varies by Unity version, so we use reflection
                var editorWindow = EditorWindow.GetWindow(consoleWindowType, false, null, false);

                // Unity 2019.3+ uses a different approach through EditorGUI
                // We'll rely on OnOpenAsset callback instead
            }
            catch (Exception)
            {
                // Silently fail if we can't hook the console
            }
        }

        // NOTE: We don't use OnOpenAsset here because IExternalCodeEditor.OpenProject 
        // already handles file opening from Project window. 
        // OnOpenAsset would cause duplicate opens.
        // This class is kept for utility methods like TryParseStackTrace that could be
        // used for future console-specific features.

        /// <summary>
        /// Parse a stack trace line to extract file path and line number
        /// </summary>
        public static bool TryParseStackTrace(string stackTrace, out string filePath, out int lineNumber)
        {
            filePath = null;
            lineNumber = 0;

            if (string.IsNullOrEmpty(stackTrace))
                return false;

            // Try simple path format first: (at Assets/Script.cs:42)
            var simpleMatch = SimplePathRegex.Match(stackTrace);
            if (simpleMatch.Success)
            {
                filePath = simpleMatch.Groups["path"].Value;
                lineNumber = int.Parse(simpleMatch.Groups["line"].Value);

                // Convert relative path to absolute
                if (filePath.StartsWith("Assets"))
                {
                    filePath = System.IO.Path.GetFullPath(filePath);
                }

                return true;
            }

            // Try full path format
            var match = StackTraceRegex.Match(stackTrace);
            if (match.Success)
            {
                filePath = match.Groups["path"].Value;
                lineNumber = int.Parse(match.Groups["line"].Value);
                return true;
            }

            return false;
        }
    }
}
