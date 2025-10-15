# Video Metadata Decoder Implementation Guide

## Overview

This document explains how to use **LibVLCSharp 3.8.5** to retrieve comprehensive metadata from video streams, including:
- **Channels**: Audio, video, subtitle, and data tracks
- **Codecs**: H.264, H.265/HEVC, VP9, AV1, etc.
- **Bitrate**: Input, demux, and send bitrates (bits per second)
- **Resolution**: Width x Height in pixels
- **Additional metadata**: Frame rate, audio channels, sample rate, etc.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Core API Overview](#core-api-overview)
3. [Basic Implementation](#basic-implementation)
4. [Retrieving Track Information](#retrieving-track-information)
5. [Video Track Metadata](#video-track-metadata)
6. [Audio Track Metadata](#audio-track-metadata)
7. [Codec Information](#codec-information)
8. [Bitrate and Statistics](#bitrate-and-statistics)
9. [Complete Example](#complete-example)
10. [Common Codec FourCC Codes](#common-codec-fourcc-codes)
11. [Troubleshooting](#troubleshooting)

---

## Prerequisites

The project uses:
- **LibVLCSharp**: 3.8.5
- **VideoLAN.LibVLC.Windows**: 3.0.20
- **.NET**: 8.0

Dependencies are already configured in `src/VideoPlayer.csproj`.

---

## Core API Overview

### Key Classes and Methods

| Class/Property | Purpose |
|---------------|---------|
| `Media` | Represents a media file/stream |
| `Media.Parse()` | Parses media to extract metadata |
| `Media.ParseAsync()` | Async version of Parse() |
| `Media.Tracks` | Returns `IEnumerable<MediaTrack>` with all tracks |
| `Media.Statistics` | Returns `MediaStats` struct with bitrate info |
| `Media.Meta(MetadataType)` | Gets general metadata (title, artist, etc.) |
| `MediaTrack` | Struct containing track-specific information |
| `TrackType` | Enum: Audio, Video, Text, Unknown |

---

## Basic Implementation

### Step 1: Initialize LibVLC and Media

```csharp
using LibVLCSharp.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;

// Initialize LibVLC
Core.Initialize();
using var libVLC = new LibVLC();

// Create Media from file or URI
using var media = new Media(libVLC, "/path/to/video.mp4", FromType.FromPath);
// OR for network streams:
// using var media = new Media(libVLC, new Uri("http://example.com/video.mp4"));
```

### Step 2: Parse Media

**Synchronous parsing:**
```csharp
// Parse media to load metadata
media.Parse();

// Check if parsing succeeded
if (media.IsParsed)
{
    Console.WriteLine("Media parsed successfully");
}
```

**Asynchronous parsing (recommended for network streams):**
```csharp
// Parse with options
await media.Parse(MediaParseOptions.ParseNetwork);

// Check parsing status
Console.WriteLine($"Parse Status: {media.ParsedStatus}");
```

**Parse Options:**
- `MediaParseOptions.ParseNetwork`: For network streams
- `MediaParseOptions.ParseLocal`: For local files
- `MediaParseOptions.FetchLocal`: Fetch additional local metadata
- `MediaParseOptions.FetchNetwork`: Fetch additional network metadata

---

## Retrieving Track Information

### Accessing All Tracks

```csharp
// Get all tracks
var tracks = media.Tracks;

Console.WriteLine($"Total tracks found: {tracks.Count()}");

foreach (var track in tracks)
{
    Console.WriteLine($"Track ID: {track.Id}");
    Console.WriteLine($"Track Type: {track.TrackType}");
    Console.WriteLine($"Codec FourCC: {track.Codec}");
    Console.WriteLine($"Original FourCC: {track.OriginalFourcc}");
    Console.WriteLine($"Bitrate: {track.Bitrate} bps");
    Console.WriteLine($"Language: {track.Language ?? "Unknown"}");
    Console.WriteLine($"Description: {track.Description}");
    Console.WriteLine("---");
}
```

### Filter by Track Type

```csharp
var videoTracks = tracks.Where(t => t.TrackType == TrackType.Video);
var audioTracks = tracks.Where(t => t.TrackType == TrackType.Audio);
var subtitleTracks = tracks.Where(t => t.TrackType == TrackType.Text);
```

---

## Video Track Metadata

### Extracting Video Information

```csharp
foreach (var track in media.Tracks)
{
    if (track.TrackType == TrackType.Video)
    {
        var video = track.Data.Video;

        // Resolution
        int width = (int)video.Width;
        int height = (int)video.Height;
        Console.WriteLine($"Resolution: {width}x{height}");

        // Aspect Ratio
        float aspectRatio = (float)width / height;
        Console.WriteLine($"Aspect Ratio: {aspectRatio:F2}");

        // Frame Rate
        int frameRateNum = (int)video.FrameRateNum;
        int frameRateDen = (int)video.FrameRateDen;
        float frameRate = (float)frameRateNum / frameRateDen;
        Console.WriteLine($"Frame Rate: {frameRate:F2} fps");

        // Orientation
        Console.WriteLine($"Orientation: {video.Orientation}");

        // Projection
        Console.WriteLine($"Projection: {video.Projection}");

        // Codec
        Console.WriteLine($"Video Codec: {track.Codec}");
        Console.WriteLine($"Bitrate: {track.Bitrate} bps ({track.Bitrate / 1000000.0:F2} Mbps)");
    }
}
```

### Video Track Properties

| Property | Type | Description |
|----------|------|-------------|
| `Width` | uint | Video width in pixels |
| `Height` | uint | Video height in pixels |
| `FrameRateNum` | uint | Frame rate numerator |
| `FrameRateDen` | uint | Frame rate denominator |
| `Orientation` | VideoOrientation | Video orientation (0°, 90°, 180°, 270°) |
| `Projection` | VideoProjection | Video projection type (rectangular, 360°, etc.) |

---

## Audio Track Metadata

### Extracting Audio Information

```csharp
foreach (var track in media.Tracks)
{
    if (track.TrackType == TrackType.Audio)
    {
        var audio = track.Data.Audio;

        // Audio Channels
        int channels = (int)audio.Channels;
        Console.WriteLine($"Audio Channels: {channels}");

        // Sample Rate
        int rate = (int)audio.Rate;
        Console.WriteLine($"Sample Rate: {rate} Hz ({rate / 1000.0} kHz)");

        // Codec
        Console.WriteLine($"Audio Codec: {track.Codec}");
        Console.WriteLine($"Bitrate: {track.Bitrate} bps ({track.Bitrate / 1000.0:F2} kbps)");

        // Language
        Console.WriteLine($"Language: {track.Language ?? "Unknown"}");
    }
}
```

### Audio Track Properties

| Property | Type | Description |
|----------|------|-------------|
| `Channels` | uint | Number of audio channels (1=mono, 2=stereo, 6=5.1, etc.) |
| `Rate` | uint | Sample rate in Hz (e.g., 44100, 48000) |

---

## Codec Information

### Understanding FourCC Codes

A **FourCC** (Four Character Code) is a 4-byte identifier that uniquely identifies a codec. Each codec has its own FourCC code, typically using ASCII characters.

### Accessing Codec Information

```csharp
foreach (var track in media.Tracks)
{
    // Primary codec identifier
    uint codecFourCC = track.Codec;

    // Original codec fourcc (may differ after transcoding)
    uint originalFourCC = track.OriginalFourcc;

    // Codec profile and level (for advanced codecs)
    int profile = track.Profile;
    int level = track.Level;

    // Convert FourCC to string (little-endian)
    string codecString = FourCCToString(track.Codec);
    Console.WriteLine($"Codec: {codecString}");
}

// Helper method to convert FourCC to readable string
static string FourCCToString(uint fourcc)
{
    return new string(new[]
    {
        (char)(fourcc & 0xFF),
        (char)((fourcc >> 8) & 0xFF),
        (char)((fourcc >> 16) & 0xFF),
        (char)((fourcc >> 24) & 0xFF)
    });
}
```

### Codec Identification Example

```csharp
public class CodecInfo
{
    public static string GetCodecName(uint fourcc)
    {
        string code = FourCCToString(fourcc);

        return code switch
        {
            "h264" => "H.264/AVC",
            "H264" => "H.264/AVC",
            "avc1" => "H.264/AVC",
            "h265" => "H.265/HEVC",
            "hevc" => "H.265/HEVC",
            "hev1" => "H.265/HEVC",
            "hvc1" => "H.265/HEVC",
            "vp09" => "VP9",
            "vp90" => "VP9",
            "VP90" => "VP9",
            "av01" => "AV1",
            "mp4a" => "AAC Audio",
            "mp4v" => "MPEG-4 Video",
            "mpga" => "MPEG Audio",
            "mp3 " => "MP3 Audio",
            _ => $"Unknown ({code})"
        };
    }

    static string FourCCToString(uint fourcc)
    {
        return new string(new[]
        {
            (char)(fourcc & 0xFF),
            (char)((fourcc >> 8) & 0xFF),
            (char)((fourcc >> 16) & 0xFF),
            (char)((fourcc >> 24) & 0xFF)
        });
    }
}
```

---

## Bitrate and Statistics

### Media Statistics API

```csharp
// Get media statistics
var stats = media.Statistics;

// Input statistics
Console.WriteLine($"Input Bitrate: {stats.InputBitrate} bps ({stats.InputBitrate / 1000000.0:F2} Mbps)");
Console.WriteLine($"Input Bytes Read: {stats.ReadBytes}");

// Demux statistics
Console.WriteLine($"Demux Bitrate: {stats.DemuxBitrate} bps ({stats.DemuxBitrate / 1000000.0:F2} Mbps)");
Console.WriteLine($"Demux Bytes Read: {stats.DemuxReadBytes}");
Console.WriteLine($"Demux Corrupted: {stats.DemuxCorrupted}");
Console.WriteLine($"Demux Discontinuity: {stats.DemuxDiscontinuity}");

// Decoded statistics
Console.WriteLine($"Decoded Video: {stats.DecodedVideo} frames");
Console.WriteLine($"Decoded Audio: {stats.DecodedAudio} blocks");

// Display statistics
Console.WriteLine($"Displayed Pictures: {stats.DisplayedPictures}");
Console.WriteLine($"Lost Pictures: {stats.LostPictures}");

// Audio buffer
Console.WriteLine($"Played Audio Buffers: {stats.PlayedAbuffers}");
Console.WriteLine($"Lost Audio Buffers: {stats.LostAbuffers}");

// Output statistics
Console.WriteLine($"Send Bitrate: {stats.SendBitrate} bps");
Console.WriteLine($"Sent Bytes: {stats.SentBytes}");
Console.WriteLine($"Sent Packets: {stats.SentPackets}");
```

### MediaStats Properties

| Property | Description |
|----------|-------------|
| `InputBitrate` | Input bitrate (bits per second) |
| `ReadBytes` | Total bytes read from input |
| `DemuxBitrate` | Demuxer bitrate (bits per second) |
| `DemuxReadBytes` | Bytes read by demuxer |
| `DemuxCorrupted` | Number of corrupted packets |
| `DemuxDiscontinuity` | Number of discontinuities |
| `DecodedVideo` | Number of decoded video frames |
| `DecodedAudio` | Number of decoded audio blocks |
| `DisplayedPictures` | Pictures displayed |
| `LostPictures` | Pictures lost |
| `PlayedAbuffers` | Audio buffers played |
| `LostAbuffers` | Audio buffers lost |
| `SendBitrate` | Output bitrate (for streaming) |
| `SentBytes` | Bytes sent (for streaming) |
| `SentPackets` | Packets sent (for streaming) |

---

## Complete Example

### Full Metadata Extraction Class

```csharp
using LibVLCSharp.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace VideoPlayer
{
    public class VideoMetadataExtractor
    {
        private LibVLC _libVLC;

        public VideoMetadataExtractor()
        {
            Core.Initialize();
            _libVLC = new LibVLC();
        }

        public async Task<MediaInfo> ExtractMetadata(string filePath)
        {
            using var media = new Media(_libVLC, filePath, FromType.FromPath);

            // Parse media
            await media.Parse(MediaParseOptions.ParseLocal);

            if (media.ParsedStatus != MediaParsedStatus.Done)
            {
                throw new Exception($"Failed to parse media: {media.ParsedStatus}");
            }

            var info = new MediaInfo
            {
                Duration = media.Duration,
                Title = media.Meta(MetadataType.Title),
                Artist = media.Meta(MetadataType.Artist)
            };

            // Extract track information
            foreach (var track in media.Tracks)
            {
                switch (track.TrackType)
                {
                    case TrackType.Video:
                        info.VideoTracks.Add(ExtractVideoTrack(track));
                        break;

                    case TrackType.Audio:
                        info.AudioTracks.Add(ExtractAudioTrack(track));
                        break;

                    case TrackType.Text:
                        info.SubtitleTracks.Add(new SubtitleTrackInfo
                        {
                            Id = track.Id,
                            Language = track.Language,
                            Description = track.Description
                        });
                        break;
                }
            }

            // Extract statistics
            var stats = media.Statistics;
            info.InputBitrate = stats.InputBitrate;
            info.DemuxBitrate = stats.DemuxBitrate;

            return info;
        }

        private VideoTrackInfo ExtractVideoTrack(MediaTrack track)
        {
            var video = track.Data.Video;

            return new VideoTrackInfo
            {
                Id = track.Id,
                Codec = GetCodecName(track.Codec),
                CodecFourCC = FourCCToString(track.Codec),
                Width = (int)video.Width,
                Height = (int)video.Height,
                FrameRate = (float)video.FrameRateNum / video.FrameRateDen,
                Bitrate = track.Bitrate,
                Profile = track.Profile,
                Level = track.Level,
                Orientation = video.Orientation.ToString(),
                Projection = video.Projection.ToString()
            };
        }

        private AudioTrackInfo ExtractAudioTrack(MediaTrack track)
        {
            var audio = track.Data.Audio;

            return new AudioTrackInfo
            {
                Id = track.Id,
                Codec = GetCodecName(track.Codec),
                CodecFourCC = FourCCToString(track.Codec),
                Channels = (int)audio.Channels,
                SampleRate = (int)audio.Rate,
                Bitrate = track.Bitrate,
                Language = track.Language,
                Description = track.Description
            };
        }

        private string FourCCToString(uint fourcc)
        {
            return new string(new[]
            {
                (char)(fourcc & 0xFF),
                (char)((fourcc >> 8) & 0xFF),
                (char)((fourcc >> 16) & 0xFF),
                (char)((fourcc >> 24) & 0xFF)
            });
        }

        private string GetCodecName(uint fourcc)
        {
            string code = FourCCToString(fourcc);

            return code switch
            {
                "h264" or "H264" or "avc1" => "H.264/AVC",
                "h265" or "hevc" or "hev1" or "hvc1" => "H.265/HEVC",
                "vp09" or "vp90" or "VP90" => "VP9",
                "av01" => "AV1",
                "mp4a" => "AAC",
                "mp4v" => "MPEG-4 Video",
                "mpga" or "mp3 " => "MP3",
                _ => code
            };
        }

        public void Dispose()
        {
            _libVLC?.Dispose();
        }
    }

    // Data classes
    public class MediaInfo
    {
        public long Duration { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public List<VideoTrackInfo> VideoTracks { get; set; } = new();
        public List<AudioTrackInfo> AudioTracks { get; set; } = new();
        public List<SubtitleTrackInfo> SubtitleTracks { get; set; } = new();
        public float InputBitrate { get; set; }
        public float DemuxBitrate { get; set; }
    }

    public class VideoTrackInfo
    {
        public int Id { get; set; }
        public string Codec { get; set; }
        public string CodecFourCC { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float FrameRate { get; set; }
        public int Bitrate { get; set; }
        public int Profile { get; set; }
        public int Level { get; set; }
        public string Orientation { get; set; }
        public string Projection { get; set; }

        public override string ToString()
        {
            return $"{Width}x{Height} {FrameRate:F2}fps {Codec} @ {Bitrate / 1000000.0:F2} Mbps";
        }
    }

    public class AudioTrackInfo
    {
        public int Id { get; set; }
        public string Codec { get; set; }
        public string CodecFourCC { get; set; }
        public int Channels { get; set; }
        public int SampleRate { get; set; }
        public int Bitrate { get; set; }
        public string Language { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            string channelDesc = Channels switch
            {
                1 => "Mono",
                2 => "Stereo",
                6 => "5.1",
                8 => "7.1",
                _ => $"{Channels}ch"
            };

            return $"{channelDesc} {SampleRate / 1000.0}kHz {Codec} @ {Bitrate / 1000.0:F2} kbps";
        }
    }

    public class SubtitleTrackInfo
    {
        public int Id { get; set; }
        public string Language { get; set; }
        public string Description { get; set; }
    }
}
```

### Usage Example

```csharp
// Usage in your application
var extractor = new VideoMetadataExtractor();

try
{
    var mediaInfo = await extractor.ExtractMetadata("C:\\Videos\\sample.mp4");

    Console.WriteLine($"Title: {mediaInfo.Title}");
    Console.WriteLine($"Duration: {TimeSpan.FromMilliseconds(mediaInfo.Duration)}");
    Console.WriteLine($"Overall Bitrate: {mediaInfo.InputBitrate / 1000000.0:F2} Mbps");
    Console.WriteLine();

    Console.WriteLine("Video Tracks:");
    foreach (var video in mediaInfo.VideoTracks)
    {
        Console.WriteLine($"  - {video}");
    }

    Console.WriteLine("\nAudio Tracks:");
    foreach (var audio in mediaInfo.AudioTracks)
    {
        Console.WriteLine($"  - {audio}");
    }

    Console.WriteLine("\nSubtitle Tracks:");
    foreach (var subtitle in mediaInfo.SubtitleTracks)
    {
        Console.WriteLine($"  - {subtitle.Language ?? "Unknown"}: {subtitle.Description}");
    }
}
finally
{
    extractor.Dispose();
}
```

---

## Common Codec FourCC Codes

### Video Codecs

| FourCC | Codec Name | Description |
|--------|-----------|-------------|
| `h264`, `H264`, `avc1`, `AVC1` | H.264/AVC | Most common modern video codec |
| `h265`, `hevc`, `hev1`, `hvc1` | H.265/HEVC | High Efficiency Video Coding |
| `vp09`, `vp90`, `VP90` | VP9 | Google's royalty-free codec |
| `av01`, `AV01` | AV1 | Next-gen royalty-free codec |
| `mp4v`, `MP4V` | MPEG-4 Video | MPEG-4 Part 2 |
| `xvid`, `XVID` | Xvid | MPEG-4 ASP |
| `divx`, `DIVX`, `DX50` | DivX | MPEG-4 ASP variant |
| `WMV3` | WMV9 | Windows Media Video 9 |
| `VC-1` | VC-1 | Microsoft advanced codec |
| `theo` | Theora | Ogg Theora |
| `mjpg`, `MJPG` | Motion JPEG | JPEG sequence |

### Audio Codecs

| FourCC/Code | Codec Name | Description |
|-------------|-----------|-------------|
| `mp4a` | AAC | Advanced Audio Coding |
| `mpga`, `mp3 ` | MP3 | MPEG-1/2 Layer 3 |
| `ac-3` | AC-3 | Dolby Digital |
| `eac3` | E-AC-3 | Dolby Digital Plus |
| `dts ` | DTS | Digital Theater Systems |
| `vorb` | Vorbis | Ogg Vorbis |
| `opus` | Opus | Modern low-latency codec |
| `flac` | FLAC | Free Lossless Audio Codec |
| `alac` | ALAC | Apple Lossless |
| `pcm ` | PCM | Uncompressed audio |
| `wma2` | WMA | Windows Media Audio |

**Note**: FourCC codes are case-sensitive and may include trailing spaces to pad to 4 characters.

---

## Troubleshooting

### Issue: Parsing Returns "Skipped" Status

**Problem**: `media.ParsedStatus` returns `MediaParsedStatus.Skipped`

**Solution**:
- For local files, use `MediaParseOptions.ParseLocal`
- For network streams, use `MediaParseOptions.ParseNetwork`
- Ensure the file exists and is readable
- Check if the file format is supported

```csharp
// Try different parse options
await media.Parse(MediaParseOptions.ParseLocal | MediaParseOptions.FetchLocal);
```

### Issue: Track Information is Empty

**Problem**: `media.Tracks` returns empty or no video/audio tracks

**Solution**:
- Ensure you call `Parse()` before accessing tracks
- Wait for parsing to complete (check `IsParsed` or `ParsedStatus`)
- Verify the media file is not corrupted
- Some formats may require specific demuxer options

```csharp
// Add demuxer option for specific formats
media.AddOption("--demux=h264"); // For raw H.264 streams
```

### Issue: Statistics Show Zero Values

**Problem**: `media.Statistics` returns all zeros

**Solution**:
- Statistics are populated during playback
- Create a `MediaPlayer` and start playback
- Access statistics while media is playing

```csharp
using var mediaPlayer = new MediaPlayer(media);
mediaPlayer.Play();
await Task.Delay(2000); // Wait for playback to start

var stats = media.Statistics;
// Now stats should have values
```

### Issue: FourCC Conversion Shows Garbled Text

**Problem**: Converting FourCC to string produces unreadable characters

**Solution**:
- Some codecs use binary fourcc codes
- Filter non-printable characters
- Use a lookup table for known codecs

```csharp
static string SafeFourCCToString(uint fourcc)
{
    var chars = new[]
    {
        (char)(fourcc & 0xFF),
        (char)((fourcc >> 8) & 0xFF),
        (char)((fourcc >> 16) & 0xFF),
        (char)((fourcc >> 24) & 0xFF)
    };

    // Filter non-printable characters
    for (int i = 0; i < chars.Length; i++)
    {
        if (char.IsControl(chars[i]))
            chars[i] = '?';
    }

    return new string(chars);
}
```

### Issue: Bitrate Values Seem Incorrect

**Problem**: Track bitrate appears wrong or zero

**Solution**:
- Not all formats embed bitrate in metadata
- Calculate average bitrate: `(fileSize * 8) / duration`
- Use `media.Statistics.InputBitrate` during playback for accurate values
- Some variable bitrate (VBR) content may not report accurate values

```csharp
// Calculate average bitrate
var fileInfo = new FileInfo(filePath);
long fileSizeBytes = fileInfo.Length;
long durationMs = media.Duration;

float avgBitrate = (fileSizeBytes * 8.0f) / (durationMs / 1000.0f);
Console.WriteLine($"Average Bitrate: {avgBitrate / 1000000.0:F2} Mbps");
```

---

## Integration with Current Project

To integrate metadata extraction into the existing VideoPlayer application:

### 1. Add to MainWindow.axaml.cs

```csharp
private async void ShowMediaInfo()
{
    if (_currentMedia == null) return;

    await _currentMedia.Parse(MediaParseOptions.ParseLocal);

    var info = new StringBuilder();
    info.AppendLine("Media Information:");
    info.AppendLine($"Duration: {TimeSpan.FromMilliseconds(_currentMedia.Duration)}");
    info.AppendLine();

    foreach (var track in _currentMedia.Tracks)
    {
        if (track.TrackType == TrackType.Video)
        {
            var v = track.Data.Video;
            info.AppendLine($"Video: {v.Width}x{v.Height} @ {v.FrameRateNum / (float)v.FrameRateDen:F2}fps");
            info.AppendLine($"Codec: {FourCCToString(track.Codec)}");
        }
        else if (track.TrackType == TrackType.Audio)
        {
            var a = track.Data.Audio;
            info.AppendLine($"Audio: {a.Channels}ch {a.Rate}Hz");
            info.AppendLine($"Codec: {FourCCToString(track.Codec)}");
        }
    }

    // Display in console or UI
    Console.WriteLine(info.ToString());
}

private string FourCCToString(uint fourcc)
{
    return new string(new[]
    {
        (char)(fourcc & 0xFF),
        (char)((fourcc >> 8) & 0xFF),
        (char)((fourcc >> 16) & 0xFF),
        (char)((fourcc >> 24) & 0xFF)
    });
}
```

### 2. Store Current Media Reference

Modify `LoadVideo` method to keep media reference:

```csharp
private Media _currentMedia;

private async void LoadVideo(string filePath)
{
    try
    {
        if (_mediaPlayer == null || _libVLC == null)
        {
            Console.WriteLine("MediaPlayer not initialized");
            return;
        }

        _currentMedia = new Media(_libVLC, filePath, FromType.FromPath);
        _mediaPlayer.Media = _currentMedia;

        // Show overlay message while parsing
        NoVideoOverlay.IsVisible = false;

        // Parse metadata asynchronously
        await _currentMedia.Parse(MediaParseOptions.ParseLocal);

        // Optionally show media info
        ShowMediaInfo();

        _mediaPlayer.Play();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading video: {ex.Message}");
    }
}
```

---

## References

- **LibVLCSharp Documentation**: https://code.videolan.org/videolan/LibVLCSharp
- **LibVLC C API Reference**: https://videolan.org/developers/vlc/doc/doxygen/html/group__libvlc.html
- **FourCC Database**: https://fourcc.org/
- **VideoLAN FourCC Wiki**: https://wiki.videolan.org/FourCC/
- **VLC Source (fourcc_list.h)**: https://github.com/videolan/vlc/blob/master/src/misc/fourcc_list.h

---

## Summary

This guide covers the complete workflow for extracting video metadata using LibVLCSharp:

1. **Initialize** LibVLC and create Media object
2. **Parse** media to load metadata
3. **Access** track information via `Media.Tracks`
4. **Extract** codec, resolution, bitrate, and other details
5. **Use** MediaStats for runtime statistics

The provided example classes can be integrated directly into the VideoPlayer project to display comprehensive media information to users.

---

**Document Version**: 1.0
**Last Updated**: 2025-10-15
**LibVLCSharp Version**: 3.8.5
**Target Framework**: .NET 8.0
