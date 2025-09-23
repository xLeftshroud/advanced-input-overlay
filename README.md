# Advanced Input Overlay

A professional Windows desktop application for displaying real-time keyboard and mouse input overlays, designed for content creators, streamers, and gamers. The application features a modern WPF interface for configuration and a high-performance C++ core engine for input detection and rendering.

## Quick Start

### Pre-built Release (Recommended)
1. Download the latest release from the [Releases](../../releases) page
2. Extract the archive to your desired location
3. Run `start_overlay.bat` to launch both components
4. Use the UI to add and configure overlays

### Building from Source

#### Prerequisites
Ensure you have Visual Studio 2022 with the required workloads installed.

#### Build Steps
```powershell
# Clone the repository
git clone https://github.com/yourusername/advanced-input-overlay.git
cd advanced-input-overlay

# Option 1: Quick build and run
update_and_start.bat

# Option 2: Manual build
# Using .NET CLI (if available)
dotnet clean advanced-input-overlay.sln --configuration Release
dotnet build advanced-input-overlay.sln --configuration Release

# Using MSBuild
msbuild advanced-input-overlay.sln /p:Configuration=Release /p:Platform=x64
```

#### Starting the Application
```powershell
# Start both components
start_overlay.bat

# Or start individually
# Core engine (run first)
cd InputOverlayCore\x64\Release
InputOverlayCore.exe

# UI application (run second)
cd InputOverlayUI\bin\Release\net8.0-windows
InputOverlayUI.exe
```

## Usage Guide

### Adding Your First Overlay

1. **Launch the Application**: Run `start_overlay.bat` or start both components manually
2. **Open the UI**: The Input Overlay Manager window will appear
3. **Add New Overlay**: Click the "Add Overlay" button
4. **Choose a Preset**: Select from available presets (WASD, Full Gaming, etc.)
5. **Configure Position**: Set the overlay position and appearance
6. **Apply**: Click "Apply" to activate the overlay

## Architecture

### Core Components

- **InputOverlayUI** (.NET 8.0 WPF): Modern Material Design interface for overlay configuration and management
- **InputOverlayCore** (C++17): High-performance engine for input detection and overlay rendering
- **Presets System**: JSON-based configuration files for quick overlay setup

### Technology Stack

- **Frontend**: WPF with Material Design themes, MVVM architecture
- **Backend**: C++17 with DirectInput, SFML for graphics
- **IPC**: Named pipes for real-time communication between components
- **Configuration**: JSON-based preset system with schema validation

## Requirements

### System Requirements

- **OS**: Windows 10/11 (x64)
- **Framework**: .NET 8.0 Runtime
- **Visual C++**: Redistributable 2022 or later
- **DirectX**: DirectX 9.0c or later

### Development Requirements

- **IDE**: Visual Studio 2022 (Community or higher)
- **Workloads**:
  - .NET Desktop Development
  - Desktop Development with C++
- **SDK**: Windows 10/11 SDK (latest)
- **Platform Toolset**: v143 (Visual Studio 2022)

## Overlay Presets

The preset configuration is a JSON file that describes how input elements (keyboard keys, mouse buttons, etc.) are rendered on top of a texture atlas (sprite sheet). Each config file pairs with a `.png` image that contains the button graphics.

### Structure

- **version**: Schema version of the config (currently `1`).
- **texture**: Path to the image atlas used for rendering (`file`).
- **canvas**: Defines the overlay canvas.
  - `size`: `[width, height]` in pixels.
  - `background`: RGBA color (usually transparent).
- **defaults**: Shared settings applied to elements.
  - `pressed_offset`: `[x, y]` pixel offset applied when an element is in its pressed state.
- **elements**: List of input elements.
  - `id`: Identifier for the element (e.g., `"w"`, `"space"`).
  - `codes`: Mappings for key input.
    - `hid`: HID usage ID.
    - `winvk`: Windows virtual-key code.
  - `pos`: `[x, y]` position of the element on the overlay canvas.
  - `sprite`: Defines how the element looks.
    - `normal`: `[x, y, w, h]` rectangle inside the atlas for the idle state.
    - `pressed`: `[x, y, w, h]` rectangle for the pressed state (optional).
      - If omitted, the pressed state can instead be derived automatically using the global `pressed_offset`.
  - `z`: Z-index (render order).

### Example Element

```
{
  "id": "w",
  "codes": { "hid": 26, "winvk": 87 },
  "pos": [274, 0],
  "sprite": {
    "normal": [161, 1, 157, 128],
    "pressed": [161, 263, 157, 128]
  },
  "z": 1
}
```

This defines the **W key**:

- It listens to HID code `26` / WinVK code `87`.
- It is drawn at `(274, 0)` on the overlay.
- It uses a `157 × 128` sprite from the atlas:
  - Idle frame from `(161, 1)`.
  - Pressed frame from `(161, 263)`.
- Alternatively, if `pressed` is not defined (without the line `"pressed": [161, 263, 157, 128]`), the system will calculate the pressed frame by applying the global `pressed_offset`.
- It renders above elements with lower `z`.

### Example of preset config

```json
{
  "version": 1,
  "texture": { "file": "image_file.png" },
  "canvas": {
    "size": [width, height],
    "background": [r, g, b, a]
  },
  "defaults": { "pressed_offset": [x, y] },
  "elements": [
    {
      "id": "key_name",
      "codes": { "hid": 123, "winvk": 456 },
      "pos": [x, y],
      "sprite": {
        "normal": [sx, sy, w, h],
        "pressed": [sx, sy, w, h]
      },
      "z": 1
    }
  ]
}
```

### Key Mapping Reference

| Key | HID Code | VK Code | Key | HID Code | VK Code |
|-----|----------|---------|-----|----------|---------|
| W | 26 | 87 | Space | 44 | 32 |
| A | 4 | 65 | Shift | 225 | 16 |
| S | 22 | 83 | Ctrl | 224 | 17 |
| D | 7 | 68 | Enter | 40 | 13 |

