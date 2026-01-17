# Antigravity IDE Integration for Unity

Unity package to integrate [Antigravity IDE](https://antigravity.google) as the default code editor.

## Features

- ✅ **Double-click to open files** - Opens scripts in Antigravity at the correct line
- ✅ **Solution Generation** - Automatically generates `.sln` and `.csproj` files
- ✅ **Workspace Setup** - Hides unnecessary Unity files/folders from IDE explorer
- ✅ **Settings Sync** - Saves settings to ScriptableObject (shareable with team via VCS)

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

### 2. Setup Workspace (Recommended)
- Go to **Antigravity > Setup Workspace**
- Creates settings to hide unnecessary Unity files in IDE

### 3. Regenerate Solution (Optional)
- Go to **Antigravity > Regenerate Solution**
- Creates/updates `.sln` and `.csproj` files

### 4. Settings
- Go to **Antigravity > Settings**
- Configure executable path and other options

## Menu Items

| Menu | Description |
|------|-------------|
| Antigravity > Settings | Open settings in Inspector |
| Antigravity > Setup Workspace | Create `.vscode/settings.json` to hide folders |
| Antigravity > Regenerate Solution | Create/update `.sln` file |

## Requirements

- Unity 2020.3 or later
- Antigravity IDE installed

## License

MIT License
