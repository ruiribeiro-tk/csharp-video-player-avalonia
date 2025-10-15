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
├── Program.cs                  # Application entry point
├── App.axaml                   # Application-level XAML resources
├── App.axaml.cs                # Application initialization logic
├── MainWindow.axaml            # Main UI layout (video player interface)
├── MainWindow.axaml.cs         # Video player logic and event handlers
├── LoggerWindow.axaml          # Logger window UI layout
├── LoggerWindow.axaml.cs       # Logger window logic
├── Logger.cs                   # Logging system (Debug, Info, Warning, Error)
├── VideoMetadataExtractor.cs   # Metadata extraction and codec recognition
└── VideoPlayer.csproj          # Project configuration
```

### Main Features

#### 1. Video Player Window (MainWindow.axaml)
- Dark theme UI (#1E1E1E background)
- Four-section layout: Menu Bar, Video View with Timeline, Control Panel, Metadata Panel
- Responsive design (min: 800x600, default: 1200x720)
- Overlay message when no video is loaded
- Right-side metadata panel (300px width) displaying comprehensive video information

#### 2. Video Controls (MainWindow.axaml.cs)
- **File Menu**:
  - Open (Ctrl+O): File picker with format filtering (.mp4, .avi, .mkv, .mov, .wmv, .flv, .webm, .ts)
  - Exit (Alt+F4): Close application
- **Playback Controls**:
  - Play/Pause: Toggle playback with visual feedback (button updates automatically)
  - Previous/Next Frame: Frame-by-frame navigation
  - Loop: Toggle loop playback between markers
  - Playback Rate: Variable speed control (1x, 2x, 4x) with visual highlighting
- **Timeline Features** (`src/MainWindow.axaml.cs:190-416`):
  - Interactive slider for video scrubbing with millisecond precision
  - Visual start/end markers (green/red)
  - Real-time position display (HH:MM:SS.mmm format with milliseconds)
  - Set Start/End markers at current position
  - Reset markers functionality
  - Pointer event handling with `handledEventsToo` for slider interaction
- **Seek Controls**:
  - Direct time input (HH:MM:SS, MM:SS, or SS format)
  - Precise seeking to any position
- **Audio Controls** (`src/MainWindow.axaml.cs:746-791`):
  - Volume slider (0-100%) with real-time adjustment
  - Mute/Unmute button with emoji visual feedback (🔊/🔇)
  - Volume state preservation when toggling mute
- **Keyboard Shortcuts** (`src/MainWindow.axaml.cs:58-67`):
  - F12: Open logger window
  - All shortcuts attached programmatically (not in XAML)

#### 3. Metadata Display System (`src/VideoMetadataExtractor.cs` + `src/MainWindow.axaml.cs:537-708`)
- **Comprehensive Metadata Extraction**:
  - Async parsing using LibVLCSharp Media.Parse()
  - FourCC codec identifier conversion
  - Support for 40+ video and audio codecs
  - Automatic track type detection (video, audio, subtitle)

- **Video Track Information**:
  - Codec name and FourCC code (e.g., "H.264/AVC (h264)")
  - Resolution (width × height)
  - Frame rate (calculated from numerator/denominator)
  - Bitrate (in Mbps)
  - Profile and level information
  - Orientation and projection type

- **Audio Track Information**:
  - Codec name and FourCC code (e.g., "AAC (mp4a)")
  - Sample rate (in kHz)
  - Channel configuration (Mono, Stereo, 5.1, 7.1)
  - Bitrate (in kbps)
  - Language and description

- **File Information**:
  - Filename and format/extension
  - Duration (HH:MM:SS format)
  - Total stream count (video + audio + subtitle tracks)

- **Supported Codecs** (via `VideoMetadataExtractor.GetCodecName()`):
  - **Video**: H.264/AVC, H.265/HEVC, VP8, VP9, AV1, MPEG-4, Xvid, DivX, WMV, VC-1, Theora, Motion JPEG
  - **Audio**: AAC, MP3, MP2, Dolby Digital (AC-3, E-AC-3), DTS, Vorbis, Opus, FLAC, ALAC, PCM, WMA

#### 4. Logging System (`src/Logger.cs` + `src/LoggerWindow.axaml.cs`)
- **Logger Class** (`src/Logger.cs`):
  - Static logging methods: `Debug()`, `Info()`, `Warning()`, `Error()`
  - ObservableCollection for real-time UI updates
  - Log level filtering with MinimumLevel property
  - LogEntry class with INotifyPropertyChanged for data binding
  - Color-coded log levels (Debug: gray, Info: white, Warning: orange, Error: red)
  - Console output mirroring for debugging
- **Logger Window** (`src/LoggerWindow.axaml.cs`):
  - Separate window for viewing logs
  - Log level ComboBox (Debug, Info, Warning, Error) with default set to Debug
  - Auto-scroll to newest entries
  - Clear logs button
  - Dark-themed UI matching main window
  - Proper cleanup on close (event handler detachment)

#### 5. VLC Integration (MainWindow.axaml.cs:89-139)
- LibVLC initialization with `--no-xlib` flag for headless environments
- MediaPlayer lifecycle management
- Proper resource disposal on window close
- Media parsing with timeout for metadata extraction
- **VLC Logging Integration** (`src/MainWindow.axaml.cs:118-139`):
  - `_libVLC.Log` event handler attached to capture VLC internal logs
  - Automatic routing to Logger window based on VLC log level
  - Log messages prefixed with `[VLC/{module}]` for identification
  - Event handler cleanup on application close to prevent memory leaks

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
3. **Single Track Display**: Shows only first video/audio track (multi-track files not fully supported)
4. **No Playlist**: Single file playback only
5. **Bitrate Accuracy**: Some formats don't embed bitrate in metadata; VBR content may show 0 or inaccurate values
6. **Limited Playback Rates**: Only 1x, 2x, and 4x speeds (no slow motion or reverse playback)

## Future Enhancement Opportunities

1. **Extended Playback Speed**: Add slow motion (0.25x, 0.5x) and reverse playback (-1x, -2x)
2. **Playlist Management**: Queue multiple videos with drag-and-drop support
3. **Subtitles Support**: Load and display subtitle files with track selection
4. **Multi-Track Support**: Display and switch between multiple audio/video tracks
5. **Metadata Export**: Save metadata to JSON/XML file
6. **Advanced Metadata**: HDR detection, chapter information, thumbnail extraction
7. **Error Dialogs**: Show user-friendly error messages in UI (not just logger)
8. **Additional Keyboard Shortcuts**: Space for play/pause, arrow keys for seek, M for mute
9. **Cross-platform Builds**: Support Linux and macOS executables
10. **Settings Panel**: Remember last directory, playback preferences, default loop mode, volume level
11. **Real-time Statistics**: Display live bitrate, buffer status, dropped frames during playback
12. **Logger Enhancements**: Export logs to file, search/filter by text, timestamps in milliseconds

## Project Naming Note

The project has been renamed from "HelloWorld" to "**VideoPlayer**" to better reflect its functionality as a fully functional video player application. The project file is now `VideoPlayer.csproj` and the output executable is `VideoPlayer.exe`.

## Dependencies and Attribution

- **Avalonia**: MIT License - https://github.com/AvaloniaUI/Avalonia
- **LibVLCSharp**: LGPL v2.1+ - https://github.com/videolan/libvlcsharp
- **VLC**: GPL v2+ - https://www.videolan.org/vlc/

## Build Status

**Last successful build:** 2025-10-15
- Build time: ~3 seconds (incremental), ~35 seconds (clean build with dependency restoration)
- Warnings: 0
- Errors: 0
- Output: `./bin/VideoPlayer.exe` (148 KB self-contained Windows x64)
- Docker image: `csharp-builder`

## Recent Updates

### 2025-10-15: Audio Controls, Logger System, and Playback Rate

**Audio Controls:**
- Added volume slider (0-100%) with real-time adjustment
- Implemented mute/unmute toggle button with visual feedback (🔊/🔇)
- Volume state preservation when toggling mute
- All volume controls properly enabled/disabled with video state

**Logger System:**
- Created `Logger.cs` - Static logging class with Debug, Info, Warning, Error methods
- Built `LoggerWindow.axaml/.cs` - Dedicated logger window with dark theme
- Implemented log level filtering (Debug, Info, Warning, Error)
- Color-coded log entries for easy visual scanning
- Auto-scroll functionality for newest entries
- Integrated VLC internal logging via `_libVLC.Log` event
- VLC logs prefixed with `[VLC/{module}]` for identification
- F12 keyboard shortcut for quick logger access
- Proper event handler cleanup to prevent memory leaks

**Playback Rate Controls:**
- Added playback rate buttons (1x, 2x, 4x)
- Visual highlighting of active playback rate
- VLC's `SetRate()` method integration
- Logging for rate changes and failures

**Timeline Improvements:**
- Fixed timeline slider pointer events with `handledEventsToo` parameter
- Timeline events now properly attached programmatically
- Improved slider interaction handling
- Millisecond precision in time display (HH:MM:SS.mmm format)

**Code Quality:**
- All event handlers now attached programmatically (not in XAML)
- Consistent use of Logger throughout the codebase
- Comprehensive logging for debugging and monitoring
- Replaced all Console.WriteLine with Logger calls

### 2025-10-15: Metadata Display System (Earlier)
- Added `VideoMetadataExtractor.cs` - Comprehensive metadata extraction helper
- Enhanced `UpdateMetadata()` method with async parsing
- Implemented FourCC codec recognition for 40+ codecs
- Added data classes: `MediaInfo`, `VideoTrackInfo`, `AudioTrackInfo`, `SubtitleTrackInfo`
- Right-side metadata panel now displays:
  - Video codec, resolution, frame rate, bitrate
  - Audio codec, channels, sample rate, bitrate
  - File information and stream counts
- Created comprehensive documentation: `@METADATA_DECODER_IMPLEMENTATION.md`

### Previous Features
- Timeline scrubbing with visual markers
- Frame-by-frame navigation
- Loop playback between custom markers
- Direct time seeking (HH:MM:SS format)
- Menu bar with keyboard shortcuts
- Dark-themed professional UI

## Contact & Maintenance

This documentation was generated by Claude Code based on code analysis.
Last updated: 2025-10-15

**Important Notes:**
- Never run `dotnet` commands directly, always use the `make` command to build, run, etc.
- The project has been successfully built and tested with the Docker-based build system
- All dependencies are automatically managed through NuGet restore
- For metadata extraction details, see `@METADATA_DECODER_IMPLEMENTATION.md`
- avoid using event handlers defined on axaml files, all event handlers should be defined programatically on the corresponding .cs file.
- don't do "make build" unless a big change has occured.