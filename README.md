# Antigravity IDE Integration for Unity

Unity package to integrate [Antigravity IDE](https://antigravity.google) as the default code editor.

## Features

- Seamless integration: double-click scripts to open the correct file and line.
- Complete solution generation: builds a `.sln` that includes the Unity projects and selected package sources.
- Package filters: choose whether to include embedded, local, registry, git, built-in, tarball, or unknown packages.
- Better IntelliSense: generated solutions improve navigation and "Go to Definition" for enabled packages.
- Workspace setup: creates `.vscode/settings.json` and `omnisharp.json` when Antigravity is selected as the external editor.
- Team-friendly settings: shared solution filters live in a project asset, while local executable settings stay machine-specific.

## Installation

### From GitHub

1. Open **Window > Package Manager**
2. Click **+** > **Add package from git URL...**
3. Enter `https://github.com/ducminh1307/antigravity-unity.git`

### Manual Installation

1. Clone this repository into your Unity project's `Packages/` folder
2. Or extract it to `Packages/com.ducminh.antigravity/`

## Usage

### 1. Select Antigravity as the external editor

1. Go to **Edit > Preferences > External Tools**
2. Select **Antigravity IDE**
3. On first selection, the package will:
   - Generate the solution file
   - Create `.vscode/settings.json`
   - Create `omnisharp.json`

### 2. Configure preferences

Go to **Edit > Preferences > Antigravity**.

- `Executable Path` and `Arguments Format` are local-machine settings.
- Package inclusion toggles are shared project settings stored in the repo.

Supported argument variables:

- `$(ProjectPath)`
- `$(File)`
- `$(Line)`
- `$(Column)`

Default format:

```text
$(ProjectPath) --goto $(File):$(Line):$(Column)
```

### 3. Manual menu actions

| Menu Item | Description |
|-----------|-------------|
| **Antigravity > Preview Project Selection** | Print which projects will be included or skipped, with detection reasons, to the Console. |
| **Antigravity > Regenerate Solution** | Rebuild the `.sln` file using the current package filters. |
| **Antigravity > Setup Workspace** | Recreate `.vscode/settings.json` and `omnisharp.json`. |
| **Antigravity > Settings** | Select the shared settings asset in the Inspector. |

## Troubleshooting

- "Go to Definition" is missing:
  Go to **Edit > Preferences > Antigravity**, enable the package source you need, regenerate project files, then reopen Antigravity.

- C# language/version errors appear:
  Run **Antigravity > Setup Workspace** and reopen Antigravity so it reloads `omnisharp.json`.

- A package is missing from the solution:
  Check whether its package source is enabled in **Preferences > Antigravity**, then regenerate the solution.

## Requirements

- Unity 2020.3 or later
- Antigravity IDE installed

## License

MIT License
