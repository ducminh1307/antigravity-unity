# Antigravity IDE Integration for Unity

Unity package to integrate [Antigravity IDE](https://antigravity.google) as the default code editor.

## Features

- ✅ **Seamless Integration** - Double-click to open scripts at the correct line
- ✅ **Auto Solution Generation** - Generates complete `.sln` file including all packages (Cinemachine, URP, etc.)
- ✅ **C# Intellisense Fix** - Automatically creates `omnisharp.json` to fix C# 9.0+ language version issues (CS8370)
- ✅ **Clean Workspace** - Generates configuration to hide irrelevant Unity files/folders (`.meta`, `Library`, etc.)
- ✅ **Zero Config Setup** - Automatically runs setup when selected as External Editor
- ✅ **Settings Sync** - Saves settings to ScriptableObject for easy team sharing via version control

## Installation

### From GitHub (Unity 2019.3+)

1. Open **Window > Package Manager**
2. Click **+** > **Add package from git URL...**
3. Enter: `https://github.com/ducminh1307/antigravity-unity.git`

### Manual Installation

1. Clone this repo into your Unity project's `Packages/` folder
2. Or download ZIP and extract to `Packages/com.ducminh.antigravity/`

## Usage

### 1. Set Antigravity as External Editor
- Go to **Edit > Preferences > External Tools**
- Select **Antigravity IDE** from the dropdown
- **Done!** The package will automatically:
  - Generate the complete Solution (`.sln`)
  - Create Workspace settings (`.vscode/settings.json`)
  - Configure OmniSharp (`omnisharp.json`)

### 2. Manual Actions (if needed)

| Menu Item | Description |
|-----------|-------------|
| **Antigravity > Regenerate Solution** | Force re-create `.sln` file. Use this if you add new packages or see missing references. |
| **Antigravity > Setup Workspace** | Re-create workspace settings and omnisharp.json. |
| **Antigravity > Settings** | Open configuration inspector (Path, Arguments). |

## Troubleshooting

- **C# Version Errors?** 
  - Ensure `omnisharp.json` exists in project root (Run **Antigravity > Setup Workspace**).
  - Restart Antigravity IDE.

- **Missing Packages in IDE?**
  - Run **Antigravity > Regenerate Solution**.
  - This custom generator ensures ALL projects (including Packages) are added to the solution.

## Requirements

- Unity 2020.3 or later
- Antigravity IDE installed

## License

MIT License
