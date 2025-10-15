# Avalonia Video Player - Cross-Platform Build

This project is a cross-platform video player application built with Avalonia UI and LibVLCSharp. It is compiled inside a Docker container on Linux/macOS but produces a Windows executable that runs on Windows hosts.

## Features

- Modern dark-themed video player UI
- Supports multiple video formats: MP4, AVI, MKV, MOV, WMV, FLV, WebM
- Play, Pause, and Stop controls
- Powered by LibVLC for robust video playback
- Built with Avalonia UI for cross-platform compatibility

## Project Structure

```
.
├── src/
│   ├── Program.cs          # Application entry point
│   ├── App.axaml           # Application resources
│   ├── App.axaml.cs        # Application initialization
│   ├── MainWindow.axaml    # Video player UI layout
│   ├── MainWindow.axaml.cs # Video player logic
│   └── HelloWorld.csproj   # Project configuration
├── Dockerfile              # Docker build instructions
├── docker-compose.yml      # Docker Compose configuration
├── Makefile               # Build automation
├── CLAUDE.md              # Detailed technical documentation
└── bin/                   # Output directory (created after build)
```

## Prerequisites

- Docker installed and running
- Make (optional, but recommended)

## Usage

### Using Make (Recommended)

```bash
# Build the application
make build

# Deploy the binary to ./bin directory
make deploy

# Build and deploy in one step
make all

# Clean up artifacts
make clean

# View all available commands
make help
```

### Using Docker Directly

```bash
# Build the Docker image
docker build -t helloworld:latest .

# Create a temporary container and copy the binary
docker create --name temp-builder helloworld:latest
mkdir -p bin
docker cp temp-builder:/app/publish/. ./bin/
docker rm temp-builder
```

### Using Docker Compose

```bash
docker-compose build
docker-compose up
```

## Running the Application

After building and deploying, transfer the entire `./bin/` directory to your Windows machine and run `HelloWorld.exe`.

The binary is self-contained and includes the .NET runtime and VLC libraries, so no additional dependencies are required on the Windows host.

### Using the Video Player

1. Launch `HelloWorld.exe` on Windows
2. Click "Open File" to select a video file
3. Use the control buttons:
   - **Play**: Start or resume playback
   - **Pause**: Pause the video
   - **Stop**: Stop playback and return to the start screen

## Configuration

The application is configured to build for Windows x64 (`win-x64`) as specified in the Dockerfile (line 20).

To target a different runtime, modify the `-r` parameter in `Dockerfile:20`. Available options include:
- `win-x64` - Windows 64-bit (default)
- `win-x86` - Windows 32-bit
- `win-arm64` - Windows ARM 64-bit
- `linux-x64` - Linux 64-bit
- `osx-x64` - macOS Intel
- `osx-arm64` - macOS Apple Silicon

## Technology Stack

- **Framework**: .NET 8.0
- **UI Framework**: Avalonia UI 11.0.10
- **Video Engine**: LibVLCSharp 3.8.5
- **Theme**: Fluent Design with dark color scheme
- **Build Environment**: Docker with .NET SDK 8.0

## Documentation

For detailed technical documentation, architecture details, and enhancement opportunities, see [CLAUDE.md](CLAUDE.md).
