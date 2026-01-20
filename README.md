# Antigravity IDE Integration for Unity

Unity package to integrate [Antigravity IDE](https://antigravity.google) as the default code editor.

## Features

- ✅ **Seamless Integration** - Double-click to open scripts at the correct line
- ✅ **Auto Solution Generation** - Generates complete `.sln` file including all packages
- ✅ **Package Filter Settings** - Customize which package types to include in solution (Embedded, Local, Registry, Git, etc.)
- ✅ **Full Intellisense Support** - "Go to Definition" works immediately for all enabled packages
- ✅ **C# 9.0+ Support** - Automatically creates `omnisharp.json` to fix language version issues (CS8370)
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

### 3. Package Filter Settings

Control which package types are included in solution generation:

1. Go to **Edit > Preferences > Antigravity**
2. Customize "Generate .csproj files for:" section:
   - ✅ **Embedded packages** - Packages in `Packages/` folder within project
   - ✅ **Local packages** - Packages referenced by file path (recommended for custom packages)
   - ⬜ **Registry packages** - Packages from Unity Registry (disable to reduce noise)
   - ⬜ **Git packages** - Packages from Git repositories
   - ⬜ **Built-in packages** - Unity built-in packages (`com.unity.*`)
   - ⬜ **Local tarball** - Packages from `.tgz` files
   - ⬜ **Unknown sources** - Other packages
   - ✅ **Player projects** - `Assembly-CSharp` and project assemblies
3. Click **"Regenerate Project Files"** to apply changes
4. Reopen project in Antigravity IDE

**Benefits:**
- 🚀 Faster IDE startup (fewer projects to load)
- ✅ Full Intellisense for enabled packages
- 🎯 "Go to Definition" works immediately

## Troubleshooting

- **"Go to Definition" Not Working?**
  - Go to **Edit > Preferences > Antigravity**
  - Ensure your package type is enabled (e.g., enable "Local packages" for custom packages)
  - Click **"Regenerate Project Files"**
  - Restart Antigravity IDE

- **C# Version Errors?** 
  - Ensure `omnisharp.json` exists in project root (Run **Antigravity > Setup Workspace**)
  - Restart Antigravity IDE

- **Missing Packages in IDE?**
  - Check **Edit > Preferences > Antigravity** - Enable the package types you need
  - Run **Antigravity > Regenerate Solution**
  - Solution generator includes all enabled package types

## Requirements

- Unity 2020.3 or later
- Antigravity IDE installed

## License

MIT License
