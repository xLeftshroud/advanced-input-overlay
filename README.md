# Advanced Input Overlay

A professional Windows desktop application for displaying real-time keyboard and mouse input overlays, designed for content creators, streamers, and gamers. The application features a modern WPF interface for configuration and a high-performance C++ core engine for input detection and rendering.

## 🎯 Features

- **Real-time Input Visualization**: Display keyboard presses and mouse clicks with customizable visual feedback
- **Flexible Overlay System**: Support for multiple simultaneous overlays with independent configurations
- **Preset Management**: Pre-built configurations for common gaming layouts (WASD, arrow keys, full gaming setups)
- **Custom Key Mapping**: Configure any keyboard or mouse input with HID and Windows Virtual Key support
- **Modern UI**: Material Design-based WPF interface for intuitive overlay management
- **High Performance**: Optimized C++ core engine with DirectInput integration for minimal system impact
- **Cross-Process Communication**: Seamless IPC between UI and core engine for real-time updates

## 🏗️ Architecture

### Core Components

- **InputOverlayUI** (.NET 8.0 WPF): Modern Material Design interface for overlay configuration and management
- **InputOverlayCore** (C++17): High-performance engine for input detection and overlay rendering
- **Presets System**: JSON-based configuration files for quick overlay setup

### Technology Stack

- **Frontend**: WPF with Material Design themes, MVVM architecture
- **Backend**: C++17 with DirectInput, SFML for graphics
- **IPC**: Named pipes for real-time communication between components
- **Configuration**: JSON-based preset system with schema validation

## 📋 Requirements

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

## 🚀 Quick Start

### Pre-built Release (Recommended)
1. Download the latest release from the [Releases](../../releases) page
2. Extract the archive to your desired location
3. Run `start_overlay.bat` to launch both components
4. Use the UI to add and configure overlays

### Building from Source

#### Prerequisites
Ensure you have Visual Studio 2022 with the required workloads installed.

#### Build Steps
```bash
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
```bash
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

## 📖 Usage Guide

### Adding Your First Overlay

1. **Launch the Application**: Run `start_overlay.bat` or start both components manually
2. **Open the UI**: The Input Overlay Manager window will appear
3. **Add New Overlay**: Click the "Add Overlay" button
4. **Choose a Preset**: Select from available presets (WASD, Full Gaming, etc.)
5. **Configure Position**: Set the overlay position and appearance
6. **Apply**: Click "Apply" to activate the overlay

### Available Presets

| Preset | Description | Use Case |
|--------|-------------|----------|
| **WASD** | Basic movement keys (W, A, S, D) with modifiers | FPS games, action games |
| **Arrow Keys** | Directional navigation | Platformers, puzzle games |
| **Gaming Full** | Comprehensive layout with WASD, Space, Shift, Ctrl, Mouse | Complex games requiring multiple inputs |

### Custom Configuration

Create custom overlays by:
1. Modifying existing preset JSON files in the `Presets/` directory
2. Creating new preset files following the schema documentation
3. Using the UI to adjust key positions and visual properties

## 🔧 Configuration

### Preset Configuration Schema

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

## 🐛 Troubleshooting

### Common Issues

**Application won't start**
- Ensure .NET 8.0 Runtime is installed
- Check that both executables are in their expected directories
- Run as Administrator if experiencing permission issues

**Input not detected**
- Verify DirectInput drivers are properly installed
- Check that the application has necessary permissions
- Ensure no antivirus software is blocking input detection

**Overlays not displaying**
- Confirm the core engine started successfully
- Check IPC communication between components
- Verify overlay configuration files are valid JSON

### Performance Optimization

- Close unnecessary applications to reduce input latency
- Use Release builds for optimal performance
- Consider disabling Windows Game Mode if experiencing conflicts

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Workflow
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes following the coding standards
4. Test thoroughly on Windows 10/11
5. Submit a pull request

### Coding Standards
- **C++**: Follow Modern C++ guidelines (C++17)
- **C#**: Follow Microsoft's .NET coding conventions
- **XAML**: Use Material Design principles
- **Documentation**: Update relevant documentation for new features

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Material Design in XAML**: UI theming and components
- **SFML**: Graphics and multimedia framework
- **DirectInput**: Low-level input detection
- **Newtonsoft.Json**: JSON configuration parsing

## 📞 Support

- **Issues**: Report bugs and request features via [GitHub Issues](../../issues)
- **Discussions**: Join community discussions in [GitHub Discussions](../../discussions)
- **Documentation**: Comprehensive guides available in the [Wiki](../../wiki)

---

*Built with ❤️ for the Windows desktop development community*