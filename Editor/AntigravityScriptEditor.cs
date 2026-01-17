using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.CodeEditor;

namespace Antigravity.Editor
{
    [InitializeOnLoad]
    public class AntigravityScriptEditor : IExternalCodeEditor
    {
        private static AntigravityScriptEditor _instance;

        private static readonly string[] SupportedExtensions = { ".cs", ".txt", ".json", ".xml", ".shader", ".compute", ".cginc", ".hlsl", ".glsl", ".yaml", ".yml", ".md" };

        static AntigravityScriptEditor()
        {
            // Register this editor with Unity's Code Editor system
            CodeEditor.Register(new AntigravityScriptEditor());
        }

        /// <summary>
        /// Default path for Antigravity IDE executable
        /// </summary>
        private static string DefaultEditorPath
        {
            get
            {
#if UNITY_EDITOR_WIN
                // Try common Windows installation paths
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string defaultPath = Path.Combine(localAppData, "Programs", "Antigravity", "Antigravity.exe");
                
                if (File.Exists(defaultPath))
                    return defaultPath;
                
                // Try Program Files
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                defaultPath = Path.Combine(programFiles, "Antigravity", "Antigravity.exe");
                
                if (File.Exists(defaultPath))
                    return defaultPath;
                
                return "antigravity"; // Fallback to CLI command
#elif UNITY_EDITOR_OSX
                string defaultPath = "/Applications/Antigravity.app/Contents/MacOS/Antigravity";
                
                if (File.Exists(defaultPath))
                    return defaultPath;
                
                return "antigravity"; // Fallback to CLI command
#else
                return "antigravity"; // Linux - use CLI command
#endif
            }
        }

        /// <summary>
        /// Get or set the path to Antigravity executable
        /// </summary>
        public static string EditorPath
        {
            get
            {
                string path = AntigravitySettings.Instance.executablePath;
                return string.IsNullOrEmpty(path) ? DefaultEditorPath : path;
            }
            set
            {
                AntigravitySettings.Instance.executablePath = value;
                AntigravitySettings.Instance.Save();
            }
        }

        /// <summary>
        /// Get or set custom arguments format
        /// </summary>
        public static string ArgumentsFormat
        {
            get => AntigravitySettings.Instance.argumentsFormat;
            set
            {
                AntigravitySettings.Instance.argumentsFormat = value;
                AntigravitySettings.Instance.Save();
            }
        }

        /// <summary>
        /// Public method to open a file externally (used by console integration)
        /// </summary>
        public static bool OpenFileExternal(string filePath, int line, int column)
        {
            if (_instance == null)
            {
                _instance = new AntigravityScriptEditor();
            }

            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            return _instance.OpenFileInProject(projectPath, filePath, line, column);
        }

        #region IExternalCodeEditor Implementation

        public CodeEditor.Installation[] Installations => new CodeEditor.Installation[]
        {
            new CodeEditor.Installation
            {
                Name = "Antigravity IDE",
                Path = EditorPath
            }
        };

        public void Initialize(string editorInstallationPath)
        {
            // Store the installation path if provided
            if (!string.IsNullOrEmpty(editorInstallationPath) && File.Exists(editorInstallationPath))
            {
                EditorPath = editorInstallationPath;
            }

            // Perform first-time setup when Antigravity is selected
            AntigravitySettings.Instance.PerformFirstTimeSetup();
        }

        public bool OpenProject(string filePath = "", int line = -1, int column = -1)
        {
            // Get the Unity project root folder
            string projectPath = Directory.GetParent(Application.dataPath).FullName;

            if (!string.IsNullOrEmpty(filePath))
            {
                // Open project folder AND the specific file at line
                return OpenFileInProject(projectPath, filePath, line, column);
            }

            // Just open the project folder
            return LaunchAntigravity($"\"{projectPath}\"");
        }

        private bool OpenFileInProject(string projectPath, string filePath, int line, int column)
        {
            // Validate file exists
            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogWarning($"[Antigravity] File not found: {filePath}");
                return false;
            }

            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            bool isSupported = Array.Exists(SupportedExtensions, ext => ext == extension);

            if (!isSupported)
            {
                // Let Unity handle unsupported file types
                return false;
            }

            // Normalize paths
            projectPath = projectPath.Replace("/", Path.DirectorySeparatorChar.ToString());
            filePath = filePath.Replace("/", Path.DirectorySeparatorChar.ToString());

            // Quote paths if they contain spaces
            string quotedProject = projectPath.Contains(" ") ? $"\"{projectPath}\"" : projectPath;
            string quotedFile = filePath.Contains(" ") ? $"\"{filePath}\"" : filePath;

            // Try format: antigravity "project" --goto "file:line:column"
            // This is similar to VS Code format: code "project" --goto "file:line:column"
            string gotoArg = $"{quotedFile}:{Math.Max(1, line)}:{Math.Max(1, column)}";
            string arguments = $"{quotedProject} --goto {gotoArg}";

            return LaunchAntigravity(arguments);
        }

        public void OnGUI()
        {
            // Settings UI in Preferences window
            EditorGUILayout.LabelField("Antigravity IDE Settings", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // Editor Path
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Executable Path");
            string newPath = EditorGUILayout.TextField(EditorPath);
            if (newPath != EditorPath)
            {
                EditorPath = newPath;
            }

            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                string selectedPath = EditorUtility.OpenFilePanel(
                    "Select Antigravity Executable",
                    Path.GetDirectoryName(EditorPath),
#if UNITY_EDITOR_WIN
                    "exe"
#elif UNITY_EDITOR_OSX
                    "app"
#else
                    ""
#endif
                );

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    EditorPath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Arguments Format
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Arguments Format");
            string newArgs = EditorGUILayout.TextField(ArgumentsFormat);
            if (newArgs != ArgumentsFormat)
            {
                ArgumentsFormat = newArgs;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "Variables: $(File) = file path, $(Line) = line number, $(Column) = column number\n" +
                "Example: \"$(File):$(Line)\" or \"--goto $(Line) $(File)\"",
                MessageType.Info
            );

            // Show current path
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Path:", EditorPath);

            // Test button - just launch Antigravity without a file
            if (GUILayout.Button("Test Open Antigravity"))
            {
                // Test launch without any file
                LaunchAntigravity("");
            }

            // Detect button
            if (GUILayout.Button("Auto-Detect Installation"))
            {
                string detected = DetectAntigravityPath();
                if (!string.IsNullOrEmpty(detected))
                {
                    EditorPath = detected;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[Antigravity] Could not auto-detect Antigravity installation.");
                }
            }
        }

        public void SyncAll()
        {
            // IMPORTANT: Do NOT call SyncVS here!
            // SyncVS.SyncSolution() calls IExternalCodeEditor.SyncAll() which causes infinite recursion
            // Solution generation is handled separately via "Antigravity > Regenerate Solution" menu
        }

        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            // IMPORTANT: Do NOT call SyncVS here!
            // Solution generation is handled separately via "Antigravity > Regenerate Solution" menu
        }



        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            if (editorPath.ToLowerInvariant().Contains("antigravity"))
            {
                installation = new CodeEditor.Installation
                {
                    Name = "Antigravity IDE",
                    Path = editorPath
                };
                return true;
            }

            installation = default;
            return false;
        }

        #endregion

        #region Process Launching

        private bool LaunchAntigravity(string arguments)
        {
            try
            {
                string editorPath = EditorPath;

                // Check if file exists
                if (!File.Exists(editorPath) && !IsCommandAvailable(editorPath))
                {
                    UnityEngine.Debug.LogError($"[Antigravity] Executable not found: {editorPath}");
                    return false;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo();

                // Directly use the executable path - UseShellExecute handles spaces properly
                startInfo.FileName = editorPath;
                startInfo.Arguments = arguments;
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Path.GetDirectoryName(editorPath) ?? "";

                Process process = Process.Start(startInfo);

                if (process == null)
                {
                    UnityEngine.Debug.LogError("[Antigravity] Process.Start returned null");
                    return false;
                }

                return true;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                UnityEngine.Debug.LogError($"[Antigravity] Win32 Error ({ex.NativeErrorCode}): {ex.Message}\nPath: {EditorPath}");
                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Antigravity] Failed to launch Antigravity: {ex.GetType().Name} - {ex.Message}\nPath: {EditorPath}");
                return false;
            }
        }

        private bool IsCommandAvailable(string command)
        {
            // Check if it's a CLI command (no extension, no path separator)
            if (command.Contains(Path.DirectorySeparatorChar.ToString()) ||
                command.Contains(Path.AltDirectorySeparatorChar.ToString()))
            {
                return false;
            }

            // It might be a CLI command like "antigravity" or "agy"
            return true; // Assume CLI commands might be available
        }

        #endregion

        #region Detection

        private string DetectAntigravityPath()
        {
#if UNITY_EDITOR_WIN
            // Check common Windows paths
            string[] possiblePaths = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Antigravity", "Antigravity.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Antigravity", "Antigravity.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Antigravity", "Antigravity.exe"),
            };
            
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }
            
            // Try to find via 'where' command
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "antigravity",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd().Trim();
                    if (!string.IsNullOrEmpty(output) && File.Exists(output.Split('\n')[0]))
                    {
                        return output.Split('\n')[0].Trim();
                    }
                }
            }
            catch { }
            
#elif UNITY_EDITOR_OSX
            // Check macOS application path
            string macPath = "/Applications/Antigravity.app/Contents/MacOS/Antigravity";
            if (File.Exists(macPath))
                return macPath;
            
            // Try 'which' command
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "antigravity",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd().Trim();
                    if (!string.IsNullOrEmpty(output) && File.Exists(output))
                    {
                        return output;
                    }
                }
            }
            catch { }
#else
            // Linux - try 'which' command
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "antigravity",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd().Trim();
                    if (!string.IsNullOrEmpty(output) && File.Exists(output))
                    {
                        return output;
                    }
                }
            }
            catch { }
#endif

            return null;
        }

        #endregion
    }
}