# CLAUDE.md - Project Documentation

## Project Overview

This is an **Avalonia-based cross-platform video player application** built with C# and .NET 8.0. The project uses Docker for cross-platform compilation, allowing development on Linux/macOS while targeting Windows executables.

### Key Technologies
- **Avalonia UI 11.0.10**: Cross-platform UI framework (similar to WPF)
- **LibVLCSharp 3.8.5**: Video playback functionality via VLC media player
- **.NET 8.0**: Target framework
- **Docker**: Build environment for cross-compilation
- **Make**: Build automation

## Architecture

### Application Entry Point
- `src/Program.cs:9-13` - Main entry point that initializes Avalonia application
- `src/Program.cs:15-19` - Configures Avalonia with platform detection and Fluent theme

### Application Structure
```
src/
├── Program.cs              # Application entry point
├── App.axaml               # Application-level XAML resources
├── App.axaml.cs            # Application initialization logic
├── MainWindow.axaml        # Main UI layout (video player interface)
├── MainWindow.axaml.cs     # Video player logic and event handlers
└── VideoPlayer.csproj      # Project configuration
```

### Main Features

#### 1. Video Player Window (MainWindow.axaml)
- Dark theme UI (#1E1E1E background)
- Three-section layout: Header, Video View, Control Panel
- Responsive design (min: 800x600, default: 1200x720)
- Overlay message when no video is loaded

#### 2. Video Controls (MainWindow.axaml.cs)
- **Open File** (`src/MainWindow.axaml.cs:46-70`): File picker with format filtering
  - Supported formats: .mp4, .avi, .mkv, .mov, .wmv, .flv, .webm
- **Play** (`src/MainWindow.axaml.cs:96-99`): Resume/start playback
- **Pause** (`src/MainWindow.axaml.cs:101-104`): Pause video
- **Stop** (`src/MainWindow.axaml.cs:106-110`): Stop playback and show overlay

#### 3. VLC Integration (MainWindow.axaml.cs:23-44)
- LibVLC initialization with `--no-xlib` flag for headless environments
- MediaPlayer lifecycle management
- Proper resource disposal on window close

### Dependencies (VideoPlayer.csproj)

**Core Avalonia Packages:**
- `Avalonia` (11.0.10) - Core framework
- `Avalonia.Desktop` (11.0.10) - Desktop platform support
- `Avalonia.Themes.Fluent` (11.0.10) - Modern UI theme
- `Avalonia.Fonts.Inter` (11.0.10) - Inter font family
- `Avalonia.Diagnostics` (11.0.10, Debug only) - DevTools

**Video Playback:**
- `LibVLCSharp` (3.8.5) - VLC bindings for .NET
- `LibVLCSharp.Avalonia` (3.8.5) - Avalonia integration
- `VideoLAN.LibVLC.Windows` (3.0.20) - VLC runtime for Windows

## Build System

### Docker Build Process (Dockerfile)

The Dockerfile creates a multi-stage build:

1. **Base Image**: `mcr.microsoft.com/dotnet/sdk:8.0`
2. **Copy Source**: Copies `src/` directory to `/app`
3. **Restore**: Downloads NuGet dependencies
4. **Build**: Compiles in Release configuration
5. **Publish**: Creates self-contained Windows x64 executable at `/app/publish`

Build flags:
- `-r win-x64`: Target Windows 64-bit
- `--self-contained true`: Includes .NET runtime
- `-o /app/publish`: Output directory

### Make Targets (Makefile)

- `make build` - Builds Docker image and extracts binary to `./bin/`
- `make dev` - **Development mode** with auto-rebuild on file changes
- `make watch` - Alias for `make dev`
- `make deploy` - Alias for `make build` (backward compatibility)
- `make clean` - Removes build artifacts and Docker resources
- `make run` - Attempts to run executable (requires Wine on Linux)
- `make help` - Shows available commands
- `make all` - Equivalent to `make build`

### Docker Compose (docker-compose.yml)

Provides volume mounting strategy:
- Source: `./src` → `/app`
- Output: `./bin` → `/app/publish`

## Configuration

### Project Settings (VideoPlayer.csproj:3-9)
- `OutputType`: WinExe (Windows executable)
- `TargetFramework`: net8.0
- `Nullable`: Enabled for null-safety
- `BuiltInComInteropSupport`: true (for COM interop)
- `AvaloniaUseCompiledBindingsByDefault`: true (performance optimization)

### Runtime Target
Current: `win-x64` (Windows 64-bit)

Alternative targets (modify Dockerfile:20):
- `win-x86` - Windows 32-bit
- `win-arm64` - Windows ARM 64-bit
- `linux-x64` - Linux 64-bit
- `osx-x64` - macOS 64-bit (Intel)
- `osx-arm64` - macOS ARM64 (Apple Silicon)

## UI Design

### Theme
- Dark theme with VS Code-inspired colors
- Primary: #1E1E1E (background)
- Secondary: #252526 (panels)
- Accent: #007ACC (primary buttons)
- Borders: #3E3E42

### Button States (MainWindow.axaml:15-37)
- Normal: #2D2D30 background
- Hover: #3E3E42 background
- Pressed: #007ACC background
- Disabled: 50% opacity

## Error Handling

### VLC Initialization (MainWindow.axaml.cs:23-44)
- Try-catch around Core.Initialize() and LibVLC creation
- Errors logged to console

### Video Loading (MainWindow.axaml.cs:72-94)
- Null checks for _mediaPlayer and _libVLC
- Try-catch around media loading
- Errors logged to console

### Resource Cleanup (MainWindow.axaml.cs:112-117)
- Proper disposal of MediaPlayer and LibVLC on window close
- Prevents memory leaks

## Development Workflow

### Quick Start
```bash
# Build and extract binary
make build

# Output location
./bin/VideoPlayer.exe
```

### Development Mode (Recommended for Active Development)

For rapid development with automatic recompilation on file changes:

```bash
# Start watch mode
make dev
# or
make watch
```

**How it works:**
- Monitors all files in `src/` directory for changes
- Automatically triggers `make build` when you save `.cs`, `.axaml`, or `.csproj` files
- Shows compilation errors in real-time
- Displays timestamps for each rebuild
- Press `Ctrl+C` to stop watch mode

**Example workflow:**
1. **Terminal 1:** Run `make dev` (keeps running, watches for changes)
2. **Your IDE:** Edit files in `src/` (e.g., `MainWindow.axaml.cs`)
3. **Save the file:** Watch mode detects change and rebuilds automatically
4. **Terminal 1:** Shows build output and any errors
5. **Windows/Wine:** Run/restart `./bin/bin/VideoPlayer.exe` to test changes

**Note:**
- Watch mode only rebuilds the executable - you must manually restart the application to see changes
- For Windows desktop apps, there's no hot-reload capability (unlike web apps)
- Uses efficient file monitoring (`inotifywait` if available, otherwise polls every 2 seconds)
- Install `inotify-tools` for better performance: `sudo apt-get install inotify-tools`

### Manual Build Workflow (Production Builds)
1. Modify source files in `src/`
2. Run `make build` to recompile in Release mode
3. Test `./bin/VideoPlayer.exe` on Windows
4. Run `make clean` to remove artifacts

### Debugging
- Enable diagnostics: Set `Configuration` to `Debug` in csproj
- Avalonia DevTools will be included (F12 to open)
- Console output available for VLC errors

## Known Limitations

1. **Cross-platform Development**: Builds on Linux but targets Windows only
2. **VLC Dependencies**: Requires VLC libraries bundled with application
3. **No Progress Bar**: No seek functionality or playback progress display
4. **No Volume Control**: No audio level adjustment UI
5. **No Playlist**: Single file playback only
6. **Limited Error UI**: Errors logged to console only, not shown in UI

## Future Enhancement Opportunities

1. **Playback Controls**: Add seek bar, volume slider, playback speed
2. **Playlist Management**: Queue multiple videos
3. **Subtitles Support**: Load and display subtitle files
4. **Format Info**: Display video metadata (resolution, codec, duration)
5. **Error Dialogs**: Show user-friendly error messages
6. **Keyboard Shortcuts**: Space to play/pause, arrow keys to seek
7. **Cross-platform Builds**: Support Linux and macOS executables
8. **Settings**: Remember last directory, playback preferences

## Project Naming Note

The project has been renamed from "HelloWorld" to "**VideoPlayer**" to better reflect its functionality as a fully functional video player application. The project file is now `VideoPlayer.csproj` and the output executable is `VideoPlayer.exe`.

## Dependencies and Attribution

- **Avalonia**: MIT License - https://github.com/AvaloniaUI/Avalonia
- **LibVLCSharp**: LGPL v2.1+ - https://github.com/videolan/libvlcsharp
- **VLC**: GPL v2+ - https://www.videolan.org/vlc/

## Build Status

**Last successful build:** 2025-10-15
- Build time: ~35 seconds (including dependency restoration)
- Warnings: 0
- Errors: 0
- Output: `./bin/VideoPlayer.exe` (self-contained Windows x64)
- Docker image: `csharp-builder`

## Contact & Maintenance

This documentation was generated by Claude Code based on code analysis.
Last updated: 2025-10-15

**Important Notes:**
- Never run `dotnet` commands directly, always use the `make` command to build, run, etc.
- The project has been successfully built and tested with the Docker-based build system
- All dependencies are automatically managed through NuGet restore