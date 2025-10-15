using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VideoPlayer
{
    /// <summary>
    /// Helper class for extracting comprehensive metadata from video files using LibVLCSharp
    /// </summary>
    public class VideoMetadataExtractor
    {
        /// <summary>
        /// Extracts comprehensive metadata from a media object
        /// </summary>
        public static async Task<MediaInfo> ExtractMetadataAsync(Media media, string filePath, int timeoutMs = 5000)
        {
            if (media == null)
                throw new ArgumentNullException(nameof(media));

            try
            {
                // Parse media to load metadata
                var parseResult = await media.Parse(MediaParseOptions.ParseLocal, timeoutMs);

                if (parseResult != MediaParsedStatus.Done && parseResult != MediaParsedStatus.Skipped)
                {
                    Console.WriteLine($"Warning: Media parse status: {parseResult}");
                }

                var info = new MediaInfo
                {
                    FilePath = filePath,
                    FileName = System.IO.Path.GetFileName(filePath),
                    Duration = media.Duration,
                    FileExtension = System.IO.Path.GetExtension(filePath)?.ToUpper().TrimStart('.') ?? "Unknown"
                };

                // Extract track information
                var tracks = media.Tracks;
                if (tracks != null && tracks.Length > 0)
                {
                    foreach (var track in tracks)
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
                                    Language = track.Language ?? "Unknown",
                                    Description = track.Description ?? "Subtitle Track"
                                });
                                break;
                        }
                    }
                }

                // Extract statistics if available
                try
                {
                    var stats = media.Statistics;
                    info.InputBitrate = stats.InputBitrate;
                    info.DemuxBitrate = stats.DemuxBitrate;
                }
                catch
                {
                    // Statistics may not be available yet
                }

                return info;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting metadata: {ex.Message}");
                // Return minimal info on error
                return new MediaInfo
                {
                    FilePath = filePath,
                    FileName = System.IO.Path.GetFileName(filePath),
                    FileExtension = System.IO.Path.GetExtension(filePath)?.ToUpper().TrimStart('.') ?? "Unknown"
                };
            }
        }

        private static VideoTrackInfo ExtractVideoTrack(MediaTrack track)
        {
            var video = track.Data.Video;

            return new VideoTrackInfo
            {
                Id = track.Id,
                Codec = GetCodecName(track.Codec),
                CodecFourCC = FourCCToString(track.Codec),
                Width = (int)video.Width,
                Height = (int)video.Height,
                FrameRateNum = (int)video.FrameRateNum,
                FrameRateDen = (int)video.FrameRateDen,
                FrameRate = video.FrameRateDen > 0 ? (float)video.FrameRateNum / video.FrameRateDen : 0,
                Bitrate = (int)track.Bitrate,
                Profile = track.Profile,
                Level = track.Level,
                Orientation = video.Orientation.ToString(),
                Projection = video.Projection.ToString()
            };
        }

        private static AudioTrackInfo ExtractAudioTrack(MediaTrack track)
        {
            var audio = track.Data.Audio;

            return new AudioTrackInfo
            {
                Id = track.Id,
                Codec = GetCodecName(track.Codec),
                CodecFourCC = FourCCToString(track.Codec),
                Channels = (int)audio.Channels,
                SampleRate = (int)audio.Rate,
                Bitrate = (int)track.Bitrate,
                Language = track.Language ?? "Unknown",
                Description = track.Description ?? "Audio Track"
            };
        }

        /// <summary>
        /// Converts a FourCC code to a readable string
        /// </summary>
        public static string FourCCToString(uint fourcc)
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
                if (char.IsControl(chars[i]) || chars[i] < 32 || chars[i] > 126)
                    chars[i] = '?';
            }

            return new string(chars).Trim();
        }

        /// <summary>
        /// Gets a human-readable codec name from a FourCC code
        /// </summary>
        public static string GetCodecName(uint fourcc)
        {
            string code = FourCCToString(fourcc);

            return code switch
            {
                // H.264/AVC variants
                "h264" or "H264" or "avc1" or "AVC1" or "x264" or "X264" => "H.264/AVC",

                // H.265/HEVC variants
                "h265" or "H265" or "hevc" or "HEVC" or "hev1" or "hvc1" or "x265" or "X265" => "H.265/HEVC",

                // VP codecs
                "vp09" or "vp90" or "VP90" or "VP9" => "VP9",
                "vp08" or "vp80" or "VP80" or "VP8" => "VP8",

                // AV1
                "av01" or "AV01" => "AV1",

                // MPEG-4
                "mp4v" or "MP4V" or "MPEG" or "mpg4" => "MPEG-4 Video",

                // Xvid/DivX
                "xvid" or "XVID" => "Xvid",
                "divx" or "DIVX" or "DX50" => "DivX",

                // Windows Media
                "WMV3" or "wmv3" => "WMV9",
                "WMV2" or "wmv2" => "WMV8",
                "WMV1" or "wmv1" => "WMV7",
                "VC-1" or "vc-1" => "VC-1",

                // Other video codecs
                "theo" or "THEO" => "Theora",
                "mjpg" or "MJPG" or "mjpa" or "jpeg" => "Motion JPEG",
                "FLV1" or "flv1" => "Flash Video",

                // AAC variants
                "mp4a" or "MP4A" => "AAC",
                "aac " or "AAC " => "AAC",

                // MPEG Audio
                "mpga" or "MPGA" or ".mp3" or "mp3 " => "MP3",
                "mp2 " or "MP2 " => "MPEG Audio Layer 2",

                // Dolby
                "ac-3" or "AC-3" or "ac3 " => "Dolby Digital (AC-3)",
                "eac3" or "EAC3" => "Dolby Digital Plus (E-AC-3)",

                // DTS
                "dts " or "DTS " => "DTS",
                "dtsh" or "DTSH" => "DTS-HD",

                // Vorbis/Opus
                "vorb" or "VORB" => "Vorbis",
                "opus" or "OPUS" => "Opus",

                // Lossless audio
                "flac" or "FLAC" => "FLAC",
                "alac" or "ALAC" => "Apple Lossless",

                // PCM
                "pcm " or "PCM " or "sowt" => "PCM",

                // Windows Media Audio
                "wma2" or "WMA2" => "Windows Media Audio 9",
                "wma1" or "WMA1" => "Windows Media Audio",

                _ => string.IsNullOrWhiteSpace(code) ? "Unknown" : code
            };
        }
    }

    /// <summary>
    /// Complete media information
    /// </summary>
    public class MediaInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long Duration { get; set; }
        public List<VideoTrackInfo> VideoTracks { get; set; } = new();
        public List<AudioTrackInfo> AudioTracks { get; set; } = new();
        public List<SubtitleTrackInfo> SubtitleTracks { get; set; } = new();
        public float InputBitrate { get; set; }
        public float DemuxBitrate { get; set; }

        public int TotalTrackCount => VideoTracks.Count + AudioTracks.Count + SubtitleTracks.Count;
    }

    /// <summary>
    /// Video track information
    /// </summary>
    public class VideoTrackInfo
    {
        public int Id { get; set; }
        public string Codec { get; set; } = string.Empty;
        public string CodecFourCC { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public int FrameRateNum { get; set; }
        public int FrameRateDen { get; set; }
        public float FrameRate { get; set; }
        public int Bitrate { get; set; }
        public int Profile { get; set; }
        public int Level { get; set; }
        public string Orientation { get; set; } = string.Empty;
        public string Projection { get; set; } = string.Empty;

        public string ResolutionString => $"{Width}x{Height}";
        public string FrameRateString => FrameRate > 0 ? $"{FrameRate:F2} fps" : "Unknown";
        public string BitrateString => Bitrate > 0 ? $"{Bitrate / 1000000.0:F2} Mbps" : "Unknown";

        public override string ToString()
        {
            return $"{Width}x{Height} @ {FrameRateString}, {Codec}, {BitrateString}";
        }
    }

    /// <summary>
    /// Audio track information
    /// </summary>
    public class AudioTrackInfo
    {
        public int Id { get; set; }
        public string Codec { get; set; } = string.Empty;
        public string CodecFourCC { get; set; } = string.Empty;
        public int Channels { get; set; }
        public int SampleRate { get; set; }
        public int Bitrate { get; set; }
        public string Language { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string ChannelString => Channels switch
        {
            1 => "Mono",
            2 => "Stereo",
            6 => "5.1 Surround",
            8 => "7.1 Surround",
            _ => $"{Channels} channels"
        };

        public string SampleRateString => SampleRate > 0 ? $"{SampleRate / 1000.0:F1} kHz" : "Unknown";
        public string BitrateString => Bitrate > 0 ? $"{Bitrate / 1000.0:F0} kbps" : "Unknown";

        public override string ToString()
        {
            return $"{ChannelString}, {SampleRateString}, {Codec}, {BitrateString}";
        }
    }

    /// <summary>
    /// Subtitle track information
    /// </summary>
    public class SubtitleTrackInfo
    {
        public int Id { get; set; }
        public string Language { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{Language} - {Description}";
        }
    }
}
