# CLAUDE.md - Advanced Input Overlay Project

## 📋 Project Overview

**Advanced Input Overlay** is a Windows desktop application for displaying real-time keyboard and mouse input overlays. The project consists of two main components: a high-performance C++ core engine for input detection and a modern WPF user interface for configuration and management.

## 🏗️ Architecture

### Core Components
- **InputOverlayCore** (C++17): Native engine for input detection and overlay rendering
- **InputOverlayUI** (.NET 8.0 WPF): Material Design interface for overlay management
- **IPC Communication**: Named pipes for real-time communication between components
- **Preset System**: JSON-based configuration files for quick overlay setup

### Technology Stack
- **C++ Core**: DirectInput, SFML 2.6.2, Visual Studio 2022 (v143 toolset)
- **C# UI**: WPF, Material Design, MVVM pattern, .NET 8.0
- **Build System**: MSBuild (Visual Studio solution)
- **Configuration**: JSON with schema validation

## 📁 Project Structure

```
advanced-input-overlay/
├── InputOverlayCore/              # C++ Engine
│   ├── include/                   # Header files
│   │   ├── Common.h              # Shared definitions
│   │   ├── InputDetection.h      # Input capture system
│   │   ├── ConfigParser.h        # JSON configuration parser
│   │   ├── IPCManager.h          # Inter-process communication
│   │   ├── OverlayRenderer.h     # Full renderer (unused)
│   │   └── SFML/                 # SFML headers
│   ├── src/                      # Source files
│   │   ├── main_simple.cpp       # Active main file (simplified)
│   │   ├── main.cpp              # Full implementation (unused)
│   │   ├── InputDetection.cpp    # Input detection logic
│   │   ├── ConfigParser.cpp      # JSON parsing
│   │   ├── IPCManager.cpp        # IPC implementation
│   │   ├── Common.cpp            # Shared utilities
│   │   └── OverlayRenderer.cpp   # Full renderer (unused)
│   ├── lib/                      # SFML libraries
│   ├── *.dll                     # SFML runtime libraries
│   └── InputOverlayCore.vcxproj  # C++ project file
├── InputOverlayUI/               # WPF Interface
│   ├── MainWindow.xaml/.cs       # Main application window
│   ├── AddOverlayDialog.xaml/.cs # Overlay creation dialog
│   ├── OverlayWindow.xaml/.cs    # Overlay display window
│   ├── Models/                   # Data models
│   │   ├── OverlayConfig.cs      # Overlay configuration
│   │   ├── OverlayItem.cs        # UI overlay items
│   │   └── AppSettings.cs        # Application settings
│   ├── ViewModels/               # MVVM view models
│   │   └── MainViewModel.cs      # Main window logic
│   ├── Services/                 # Application services
│   │   └── SettingsService.cs    # Settings management
│   ├── Converters/               # UI converters
│   │   └── BoolToVisibilityColorConverter.cs
│   ├── Styles/                   # UI styling
│   │   └── CompactStyles.xaml    # Compact UI styles
│   └── InputOverlayUI.csproj     # C# project file
├── Presets/                      # Configuration files
│   ├── wasd.json                 # WASD keyboard layout
│   ├── wasd.png                  # WASD texture file
│   └── README.md                 # Preset documentation
├── external/                     # Third-party dependencies
│   └── SFML-2.6.2/              # SFML graphics library
├── advanced-input-overlay.sln   # Main Visual Studio solution
├── start_overlay.bat             # Quick start script
├── update_and_start.bat          # Build and start script
└── README.md                     # Project documentation
```

## 🛠️ Development Commands

### Build Commands
```bash
# Clean and build solution
dotnet clean advanced-input-overlay.sln --configuration Release
dotnet build advanced-input-overlay.sln --configuration Release

# Alternative: MSBuild approach
msbuild advanced-input-overlay.sln /p:Configuration=Release /p:Platform=x64
```

### Run Commands
```bash
# Quick start (uses pre-built binaries)
start_overlay.bat

# Build and start
update_and_start.bat

# Manual start
# 1. Start core engine first
cd InputOverlayCore\x64\Release
InputOverlayCore.exe

# 2. Start UI second
cd InputOverlayUI\bin\Release\net8.0-windows
InputOverlayUI.exe
```

## 🔧 Key Features Implemented

### Input Detection (C++)
- DirectInput integration for low-level keyboard/mouse capture
- HID and Windows Virtual Key code support
- Real-time input state tracking
- Minimal system performance impact

### Overlay Management (C#)
- Material Design WPF interface
- Dynamic overlay creation and configuration
- Preset-based quick setup system
- Real-time overlay control

### IPC Communication
- Named pipes for UI ↔ Core communication
- Message types: ADD_OVERLAY, SHOW_OVERLAY, CLOSE_OVERLAY, etc.
- JSON-based configuration transfer
- Real-time status updates

### Configuration System
- JSON-based preset files
- Schema validation for overlay elements
- Key mapping with HID/VK codes
- Sprite positioning and animation support

## 📝 Code Quality Notes

### Active vs Unused Files
- **Active**: `main_simple.cpp` (simplified implementation)
- **Unused**: `main.cpp`, `OverlayRenderer.cpp` (full implementation)
- **Consider**: Removing unused files or clarifying purpose

### Dependencies
- SFML 2.6.2 (graphics library)
- Material Design themes (UI)
- DirectInput (input capture)
- Newtonsoft.Json (configuration parsing)

### Build Artifacts
- Build outputs (bin/, obj/, x64/) are excluded from source control
- Runtime DLLs are included for distribution
- Visual Studio cache (.vs/) is excluded

## 🐛 Known Issues & Improvements

### Version Mismatches
- Project references SFML 2.6.2 but some references may point to 3.0.2
- Ensure consistent version usage across all components

### Missing Preset Files
- `arrow_keys.json` and `gaming_full.json` referenced in README but not present
- Consider creating these files or updating documentation

### Code Organization
- Clarify purpose of unused renderer files
- Consider consolidating main.cpp implementations
- Add more comprehensive error handling

## 🧪 Testing Strategy

### Manual Testing
- Build both components successfully
- Verify IPC communication between UI and Core
- Test overlay creation with various presets
- Validate input detection accuracy

### Automated Testing
- Unit tests for configuration parsing
- Integration tests for IPC communication
- Performance tests for input detection latency

## 🚀 Common Tasks

### Adding New Preset
1. Create JSON file in `Presets/` directory
2. Follow schema in `Presets/README.md`
3. Include corresponding texture files
4. Test with UI application

### Modifying Input Detection
1. Edit `InputDetection.h` and `InputDetection.cpp`
2. Update key code mappings in `Common.h`
3. Rebuild C++ core component
4. Test with various input devices

### UI Enhancements
1. Modify XAML files for layout changes
2. Update ViewModels for logic changes
3. Follow Material Design guidelines
4. Test with different Windows themes

## 📚 External Resources

- [SFML Documentation](https://www.sfml-dev.org/documentation/)
- [Material Design in XAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- [DirectInput Reference](https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ee416842(v=vs.85))
- [.NET 8.0 WPF Guide](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)

---

*This CLAUDE.md file provides context for AI assistants working on the Advanced Input Overlay project. Last updated: 2024-09-22*