using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace VideoPlayer
{
    public partial class MainWindow : Window
    {
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        private string? _currentFilePath;
        private DispatcherTimer? _timelineUpdateTimer;
        private bool _isUserDraggingSlider = false;
        private bool _wasPlayingBeforeDrag = false;

        // Marker positions in milliseconds
        private long _startMarkerMs = 0;
        private long _endMarkerMs = 0;
        private bool _isLooping = false;

        // Frame rate (approximate, will be calculated from video)
        private double _frameDurationMs = 1000 / 25.0; // 1000 / fps

        // Volume control
        private bool _isMuted = false;
        private int _volumeBeforeMute = 100;

        private bool _isUserSelecting = false;

        // Playback rate
        private float _currentPlaybackRate = 1.0f;

        // Logger window
        private LoggerWindow? _loggerWindow;

        public MainWindow()
        {
            InitializeComponent();
            InitializeVLC();
            InitializeTimeline();
            AttachTimelineSliderEvents();
            AttachKeyboardShortcuts();
        }

        private void AttachKeyboardShortcuts()
        {
            // Attach keyboard event handler for shortcuts
            this.KeyDown += MainWindow_KeyDown;
            Logger.Debug("Keyboard shortcuts attached");
        }

        private void MainWindow_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            // F12 - Open Logger Window
            if (e.Key == Avalonia.Input.Key.F12)
            {
                Logger.Info("F12 pressed - Opening logger window");
                ShowLoggerButton_Click(null, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void AttachTimelineSliderEvents()
        {
            // Attach pointer events with handledEventsToo = true to receive events even if handled by child controls
            TimelineSlider.AddHandler(PointerPressedEvent, TimelineSlider_PointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
            TimelineSlider.AddHandler(PointerReleasedEvent, TimelineSlider_PointerReleased, Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
            TimelineSlider.AddHandler(PointerMovedEvent, TimelineSlider_PointerMoved, Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);

            Logger.Debug("Timeline slider pointer events attached with handledEventsToo=true");
        }

        private void InitializeTimeline()
        {
            // Create timer to update timeline
            _timelineUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000 / 30) // ~30 FPS update rate
            };
            _timelineUpdateTimer.Tick += TimelineUpdateTimer_Tick;
        }

        private void InitializeVLC()
        {
            try
            {
                Core.Initialize();
                _libVLC = new LibVLC("--no-xlib");
                _mediaPlayer = new MediaPlayer(_libVLC);

                // Attach VLC log event handler
                _libVLC.Log += LibVLC_Log;

                Logger.Info("VLC initialized successfully");

                // Ensure VideoView is properly initialized and attached
                this.Opened += (sender, args) =>
                {
                    if (VideoView != null && _mediaPlayer != null)
                    {
                        VideoView.MediaPlayer = _mediaPlayer;
                        Logger.Debug("VideoView attached to MediaPlayer");
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Error initializing VLC: {ex.Message}");
            }
        }

        private void LibVLC_Log(object? sender, LogEventArgs e)
        {
            // Route VLC logs to our Logger window based on VLC log level
            string message = $"[VLC/{e.Module}] {e.Message}";

            switch (e.Level)
            {
                case LibVLCSharp.Shared.LogLevel.Debug:
                    Logger.Debug(message);
                    break;
                case LibVLCSharp.Shared.LogLevel.Notice:
                case LibVLCSharp.Shared.LogLevel.Warning:
                    Logger.Warning(message);
                    break;
                case LibVLCSharp.Shared.LogLevel.Error:
                    Logger.Error(message);
                    break;
                default:
                    Logger.Info(message);
                    break;
            }
        }

        // Menu handlers
        private async void MenuOpen_Click(object? sender, RoutedEventArgs e)
        {
            await OpenVideoFile();
        }

        private void MenuExit_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        // Open file dialog (used by menu and potentially other controls)
        private async Task OpenVideoFile()
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Video File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Video Files")
                    {
                        Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv", "*.webm", "*.ts" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                _currentFilePath = files[0].Path.LocalPath;
                LoadAndPlayVideo(_currentFilePath);
            }
        }

        private void LoadAndPlayVideo(string filePath)
        {
            if (_mediaPlayer == null || _libVLC == null)
                return;

            try
            {
                Logger.Info($"Loading video: {System.IO.Path.GetFileName(filePath)}");
                var media = new Media(_libVLC, filePath, FromType.FromPath);
                _mediaPlayer.Play(media);

                // Hide the overlay when video starts
                NoVideoOverlay.IsVisible = false;
                Logger.Debug("Video overlay hidden, playback started");

                // Update Play/Pause button since video autostarts
                PlayPauseButton.Content = "⏸ Pause";
                Logger.Debug("Play button updated to Pause (video autostarted)");

                // Enable control buttons
                PlayPauseButton.IsEnabled = true;
                LoopButton.IsEnabled = true;
                PreviousFrameButton.IsEnabled = true;
                NextFrameButton.IsEnabled = true;
                TimelineSlider.IsEnabled = true;
                SetStartMarkerButton.IsEnabled = true;
                SetEndMarkerButton.IsEnabled = true;
                ResetMarkersButton.IsEnabled = true;
                SeekTimeTextBox.IsEnabled = true;
                SeekToTimeButton.IsEnabled = true;
                VolumeSlider.IsEnabled = true;
                MuteButton.IsEnabled = true;
                PlaybackRate1xButton.IsEnabled = true;
                PlaybackRate2xButton.IsEnabled = true;
                PlaybackRate4xButton.IsEnabled = true;

                // Set initial volume
                _mediaPlayer.Volume = (int)VolumeSlider.Value;

                // Set initial playback rate
                _currentPlaybackRate = 1.0f;
                _mediaPlayer.SetRate(_currentPlaybackRate);
                UpdatePlaybackRateButtons();

                // Start timeline update timer
                _timelineUpdateTimer?.Start();

                // Wait for media to be parsed to get duration and metadata
                Task.Run(async () =>
                {
                    await Task.Delay(500); // Wait for media to load
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_mediaPlayer != null && _mediaPlayer.Length > 0)
                        {
                            _startMarkerMs = 0;
                            _endMarkerMs = _mediaPlayer.Length;
                            UpdateMarkerDisplay();
                            UpdateMetadata();

                            // Verify media is seekable
                            if (!_mediaPlayer.IsSeekable)
                            {
                                Logger.Warning("Media is not seekable");
                                TimelineSlider.IsEnabled = false;
                            }
                            else
                            {
                                Logger.Debug("Media is seekable and ready");
                            }
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading video: {ex.Message}");
            }
        }

        private void PlayPauseButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null)
                return;

            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                PlayPauseButton.Content = "▶ Play";
                Logger.Debug("Playback paused");
            }
            else
            {
                _mediaPlayer.Play();
                PlayPauseButton.Content = "⏸ Pause";
                Logger.Debug("Playback resumed");
            }
        }

        private void LoopButton_Click(object? sender, RoutedEventArgs e)
        {
            _isLooping = !_isLooping;
            LoopButton.Content = _isLooping ? "🔁 Loop: ON" : "🔁 Loop: OFF";
            LoopButton.Background = _isLooping ? new SolidColorBrush(Color.Parse("#007ACC")) : new SolidColorBrush(Color.Parse("#2D2D30"));
            Logger.Info($"Loop mode {(_isLooping ? "enabled" : "disabled")}");
        }

        private void TimelineUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (_mediaPlayer == null)
            {
                Logger.Debug("[TIMER] Tick skipped - No media player");
                return;
            }

            if (_isUserDraggingSlider)
            {
                Logger.Debug("[TIMER] Tick BLOCKED - User is dragging slider");
                return;
            }

            try
            {
                long currentTime = _mediaPlayer.Time;
                long duration = _mediaPlayer.Length;

                if (duration > 0)
                {
                    // Update timeline slider
                    TimelineSlider.Maximum = duration;
                    TimelineSlider.Value = currentTime;

                    // Update time displays
                    CurrentTimeText.Text = FormatTime(currentTime);
                    TotalTimeText.Text = FormatTime(duration);

                    // Update marker positions on timeline
                    UpdateMarkerPositions();

                    // Check for loop
                    if (_isLooping && currentTime >= _endMarkerMs)
                    {
                        _mediaPlayer.Time = _startMarkerMs;
                        Logger.Info($"[LOOP] Looping back from {FormatTime(currentTime)} to {FormatTime(_startMarkerMs)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[TIMER] Error updating timeline: {ex.Message}");
            }

        }

        private void TimelineSlider_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            Logger.Info("[TIMELINE] ========================================");
            Logger.Info("[TIMELINE] Slider pressed - User interaction started");

            // CRITICAL: Set this flag IMMEDIATELY to stop the timer from updating
            _isUserDraggingSlider = true;
            _isUserSelecting = true;

            Logger.Info("[TIMELINE] User dragging flag set to TRUE - Timer updates will be BLOCKED");

            if (_mediaPlayer != null)
            {
                Logger.Info($"[TIMELINE] Video position at press: {FormatTime(_mediaPlayer.Time)} ({_mediaPlayer.Time}ms)");
                Logger.Info($"[TIMELINE] Slider value at press: {FormatTime((long)TimelineSlider.Value)} ({TimelineSlider.Value}ms)");

                // Remember playback state
                _wasPlayingBeforeDrag = _mediaPlayer.IsPlaying;
                Logger.Info($"[TIMELINE] Was playing before interaction: {_wasPlayingBeforeDrag}");
            }
            else
            {
                Logger.Warning("[TIMELINE] Slider pressed - No media player available");
            }
        }

        private void TimelineSlider_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            // Just log the drag - ValueChanged will update the time display
            if (_isUserDraggingSlider && _mediaPlayer != null)
            {
                long targetTime = (long)TimelineSlider.Value;
                Logger.Debug($"[TIMELINE] Dragging to: {FormatTime(targetTime)} ({targetTime}ms)");
            }
        }

        private void TimelineSlider_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            Logger.Info("[TIMELINE] ========================================");
            Logger.Info("[TIMELINE] Slider released");

            // Seeking is handled by ValueChanged during dragging
            // Just release the dragging flag to allow timer updates
            _isUserDraggingSlider = false;
            _isUserSelecting = true;
            Logger.Info("[TIMELINE] User dragging flag set to FALSE - Timer updates will RESUME");
            Logger.Info("[TIMELINE] ========================================");
        }

        private void TimelineSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            // Update time display when slider value changes
            if (_mediaPlayer != null)
            {
                CurrentTimeText.Text = FormatTime((long)e.NewValue);

                // Only set media player time when user is dragging (not during timer updates)
                if (_isUserDraggingSlider || _isUserSelecting)
                {
                    _mediaPlayer.Time = (long)e.NewValue;
                    Logger.Debug($"[SLIDER] ValueChanged (user drag): Seeking to {FormatTime((long)e.NewValue)}");

                    _isUserSelecting = false;
                }
                else
                {
                    // Logger.Debug($"[SLIDER] ValueChanged (timer update): {FormatTime((long)e.NewValue)}");
                }
            }
        }

        private void SetStartMarkerButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _startMarkerMs = _mediaPlayer.Time;
                if (_startMarkerMs >= _endMarkerMs)
                {
                    _startMarkerMs = Math.Max(0, _endMarkerMs - 1000);
                }
                UpdateMarkerDisplay();
                Logger.Info($"Start marker set to {FormatTime(_startMarkerMs)}");
            }
        }

        private void SetEndMarkerButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _endMarkerMs = _mediaPlayer.Time;
                if (_endMarkerMs <= _startMarkerMs)
                {
                    _endMarkerMs = Math.Min(_mediaPlayer.Length, _startMarkerMs + 1000);
                }
                UpdateMarkerDisplay();
                Logger.Info($"End marker set to {FormatTime(_endMarkerMs)}");
            }
        }

        private void ResetMarkersButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _startMarkerMs = 0;
                _endMarkerMs = _mediaPlayer.Length;
                UpdateMarkerDisplay();
                Logger.Info("Markers reset to video start and end");
            }
        }

        private void UpdateMarkerDisplay()
        {
            StartMarkerText.Text = $"Start: {FormatTime(_startMarkerMs)}";
            EndMarkerText.Text = $"End: {FormatTime(_endMarkerMs)}";
        }

        private void UpdateMarkerPositions()
        {
            if (_mediaPlayer == null || TimelineSlider.Bounds.Width == 0)
                return;

            long duration = _mediaPlayer.Length;
            if (duration <= 0)
                return;

            // Calculate marker positions - align with slider thumb center
            double sliderWidth = TimelineSlider.Bounds.Width;
            double markerWidth = StartMarkerVisual.Bounds.Width; // Both markers have same width (3px)

            // Calculate the position ratio
            double startPositionRatio = (double)_startMarkerMs / duration;
            double endPositionRatio = (double)_endMarkerMs / duration;

            // Position aligned with slider thumb center
            double startPosition = (sliderWidth * startPositionRatio) - (markerWidth / 2);
            double endPosition = (sliderWidth * endPositionRatio) - (markerWidth / 2) - 2;

            // Update marker visuals
            StartMarkerVisual.IsVisible = true;
            EndMarkerVisual.IsVisible = true;
            Canvas.SetLeft(StartMarkerVisual, startPosition);
            Canvas.SetLeft(EndMarkerVisual, endPosition);
        }

        private void PreviousFrameButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                long currentTime = _mediaPlayer.Time;
                long newTime = Math.Max(0, currentTime - (long)_frameDurationMs);
                _mediaPlayer.Time = newTime;
                Logger.Debug($"Previous frame: {FormatTime(currentTime)} -> {FormatTime(newTime)}");
            }
        }

        private void NextFrameButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                long currentTime = _mediaPlayer.Time;
                long duration = _mediaPlayer.Length;
                long newTime = Math.Min(duration, currentTime + (long)_frameDurationMs);
                _mediaPlayer.Time = newTime;
                Logger.Debug($"Next frame: {FormatTime(currentTime)} -> {FormatTime(newTime)}");
            }
        }

        private void SeekToTimeButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null || string.IsNullOrWhiteSpace(SeekTimeTextBox.Text))
                return;

            string timeText = SeekTimeTextBox.Text.Trim();

            if (TryParseTimeString(timeText, out long timeMs))
            {
                long duration = _mediaPlayer.Length;

                if (timeMs >= 0 && timeMs <= duration)
                {
                    _mediaPlayer.Time = timeMs;
                    SeekTimeTextBox.Text = string.Empty;
                    Logger.Info($"Seek to time: {FormatTime(timeMs)}");
                }
                else
                {
                    // Invalid time - show error in textbox
                    SeekTimeTextBox.Text = "Invalid range!";
                    Logger.Warning($"Invalid seek time range: {timeText} -> {timeMs}ms (duration: {duration}ms)");
                    Task.Run(async () =>
                    {
                        await Task.Delay(2000);
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (SeekTimeTextBox.Text == "Invalid range!")
                                SeekTimeTextBox.Text = string.Empty;
                        });
                    });
                }
            }
            else
            {
                // Invalid format - show error in textbox
                SeekTimeTextBox.Text = "Invalid format!";
                Logger.Warning($"Invalid seek time format: {timeText}");
                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (SeekTimeTextBox.Text == "Invalid format!")
                            SeekTimeTextBox.Text = string.Empty;
                    });
                });
            }
        }

        private bool TryParseTimeString(string timeString, out long milliseconds)
        {
            milliseconds = 0;

            // Support formats: HH:MM:SS, MM:SS, or SS
            string[] parts = timeString.Split(':');

            try
            {
                if (parts.Length == 1)
                {
                    // Just seconds
                    int seconds = int.Parse(parts[0]);
                    milliseconds = seconds * 1000L;
                    return true;
                }
                else if (parts.Length == 2)
                {
                    // MM:SS
                    int minutes = int.Parse(parts[0]);
                    int seconds = int.Parse(parts[1]);
                    milliseconds = (minutes * 60 + seconds) * 1000L;
                    return true;
                }
                else if (parts.Length == 3)
                {
                    // HH:MM:SS
                    int hours = int.Parse(parts[0]);
                    int minutes = int.Parse(parts[1]);
                    int seconds = int.Parse(parts[2]);
                    milliseconds = (hours * 3600 + minutes * 60 + seconds) * 1000L;
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private string FormatTime(long milliseconds)
        {
            if (milliseconds < 0)
                milliseconds = 0;

            TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);
            return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
        }

        private async void UpdateMetadata()
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null || string.IsNullOrEmpty(_currentFilePath))
            {
                ResetMetadataDisplay();
                return;
            }

            try
            {
                var media = _mediaPlayer.Media;

                // Extract comprehensive metadata using helper class
                var mediaInfo = await VideoMetadataExtractor.ExtractMetadataAsync(media, _currentFilePath);

                // File Information
                MetadataFilename.Text = mediaInfo.FileName;
                MetadataDuration.Text = FormatTime(mediaInfo.Duration);
                MetadataFormat.Text = mediaInfo.FileExtension;
                MetadataStreamCount.Text = mediaInfo.TotalTrackCount.ToString();

                // Video Track Information
                if (mediaInfo.VideoTracks.Count > 0)
                {
                    var videoTrack = mediaInfo.VideoTracks[0]; // Use first video track

                    // Resolution
                    MetadataResolution.Text = videoTrack.ResolutionString;

                    // Frame Rate
                    if (videoTrack.FrameRate > 0)
                    {
                        MetadataFPS.Text = $"{videoTrack.FrameRate:F2} fps";
                        _frameDurationMs = 1000.0 / videoTrack.FrameRate;
                    }
                    else
                    {
                        MetadataFPS.Text = "-";
                    }

                    // Codec
                    string codecInfo = videoTrack.Codec;
                    if (!string.IsNullOrEmpty(videoTrack.CodecFourCC) && videoTrack.CodecFourCC != "Unknown")
                    {
                        codecInfo += $" ({videoTrack.CodecFourCC})";
                    }
                    MetadataVideoCodec.Text = codecInfo;

                    // Bitrate
                    MetadataVideoBitrate.Text = videoTrack.BitrateString;
                }
                else
                {
                    // Try to get video info from MediaPlayer properties as fallback
                    uint width = 0, height = 0;
                    bool hasSize = _mediaPlayer.Size(0, ref width, ref height);
                    if (hasSize && width > 0 && height > 0)
                    {
                        MetadataResolution.Text = $"{width}x{height}";
                    }
                    else
                    {
                        MetadataResolution.Text = "-";
                    }

                    float fps = _mediaPlayer.Fps;
                    if (fps > 0)
                    {
                        MetadataFPS.Text = $"{fps:F2} fps";
                        _frameDurationMs = 1000.0 / fps;
                    }
                    else
                    {
                        MetadataFPS.Text = "-";
                    }

                    MetadataVideoCodec.Text = "-";
                    MetadataVideoBitrate.Text = "-";
                }

                // Audio Track Information
                if (mediaInfo.AudioTracks.Count > 0)
                {
                    var audioTrack = mediaInfo.AudioTracks[0]; // Use first audio track

                    // Codec
                    string codecInfo = audioTrack.Codec;
                    if (!string.IsNullOrEmpty(audioTrack.CodecFourCC) && audioTrack.CodecFourCC != "Unknown")
                    {
                        codecInfo += $" ({audioTrack.CodecFourCC})";
                    }
                    MetadataAudioCodec.Text = codecInfo;

                    // Sample Rate
                    MetadataAudioSampleRate.Text = audioTrack.SampleRateString;

                    // Channels
                    MetadataAudioChannels.Text = audioTrack.ChannelString;

                    // Bitrate
                    MetadataAudioBitrate.Text = audioTrack.BitrateString;
                }
                else
                {
                    MetadataAudioCodec.Text = "-";
                    MetadataAudioSampleRate.Text = "-";
                    MetadataAudioChannels.Text = "-";
                    MetadataAudioBitrate.Text = "-";
                }

                Logger.Info($"Metadata extracted successfully: {mediaInfo.VideoTracks.Count} video, {mediaInfo.AudioTracks.Count} audio, {mediaInfo.SubtitleTracks.Count} subtitle tracks");

                if (mediaInfo.VideoTracks.Count > 0)
                {
                    Logger.Debug($"  Video: {mediaInfo.VideoTracks[0]}");
                }

                if (mediaInfo.AudioTracks.Count > 0)
                {
                    Logger.Debug($"  Audio: {mediaInfo.AudioTracks[0]}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving metadata: {ex.Message}");
                Logger.Debug($"Stack trace: {ex.StackTrace}");

                // Set basic information on error
                MetadataFilename.Text = System.IO.Path.GetFileName(_currentFilePath) ?? "-";
                MetadataFormat.Text = System.IO.Path.GetExtension(_currentFilePath)?.ToUpper().TrimStart('.') ?? "-";

                // Try to at least get duration from player
                if (_mediaPlayer != null && _mediaPlayer.Length > 0)
                {
                    MetadataDuration.Text = FormatTime(_mediaPlayer.Length);
                }
                else
                {
                    MetadataDuration.Text = "-";
                }

                // Reset other fields
                MetadataResolution.Text = "-";
                MetadataFPS.Text = "-";
                MetadataVideoCodec.Text = "-";
                MetadataVideoBitrate.Text = "-";
                MetadataAudioCodec.Text = "-";
                MetadataAudioSampleRate.Text = "-";
                MetadataAudioChannels.Text = "-";
                MetadataAudioBitrate.Text = "-";
                MetadataStreamCount.Text = "-";
            }
        }

        private void ResetMetadataDisplay()
        {
            MetadataFilename.Text = "-";
            MetadataDuration.Text = "-";
            MetadataResolution.Text = "-";
            MetadataFPS.Text = "-";
            MetadataVideoCodec.Text = "-";
            MetadataVideoBitrate.Text = "-";
            MetadataAudioCodec.Text = "-";
            MetadataAudioSampleRate.Text = "-";
            MetadataAudioChannels.Text = "-";
            MetadataAudioBitrate.Text = "-";
            MetadataFormat.Text = "-";
            MetadataStreamCount.Text = "-";
        }

        // Volume control handlers
        private void VolumeSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_mediaPlayer == null)
                return;

            int volume = (int)e.NewValue;
            _mediaPlayer.Volume = volume;
            VolumeText.Text = $"{volume}%";

            Logger.Debug($"Volume changed to {volume}%");

            // Update mute button if volume changes
            if (volume == 0 && !_isMuted)
            {
                MuteButton.Content = "🔇";
            }
            else if (volume > 0 && !_isMuted)
            {
                MuteButton.Content = "🔊";
            }
        }

        private void MuteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null)
                return;

            _isMuted = !_isMuted;

            if (_isMuted)
            {
                Logger.Info("Audio muted");
                _volumeBeforeMute = (int)VolumeSlider.Value;
                VolumeSlider.Value = 0;
                _mediaPlayer.Volume = 0;
                MuteButton.Content = "🔇";
            }
            else
            {
                Logger.Info("Audio unmuted");
                VolumeSlider.Value = _volumeBeforeMute;
                _mediaPlayer.Volume = _volumeBeforeMute;
                MuteButton.Content = "🔊";
            }
        }

        // Logger window handler
        private void ShowLoggerButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_loggerWindow == null || !_loggerWindow.IsVisible)
            {
                _loggerWindow = new LoggerWindow();
                _loggerWindow.Show();
                Logger.Info("Logger window opened from main window");
            }
            else
            {
                _loggerWindow.Activate();
            }
        }

        // Playback rate handler
        private void PlaybackRateButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null || sender is not Button button || button.Tag is not string rateString)
                return;

            if (float.TryParse(rateString, out float rate))
            {
                _currentPlaybackRate = rate;

                // VLC supports negative playback rates for reverse playback
                int result = _mediaPlayer.SetRate(rate);

                if (result == 0)
                {
                    Logger.Info($"Playback rate changed to {rate}x");
                    UpdatePlaybackRateButtons();
                }
                else
                {
                    Logger.Warning($"Failed to set playback rate to {rate}x (error code: {result})");
                }
            }
        }

        private void UpdatePlaybackRateButtons()
        {
            // Reset all buttons to default style
            PlaybackRate1xButton.Background = new SolidColorBrush(Color.Parse("#4A4A4F"));
            PlaybackRate2xButton.Background = new SolidColorBrush(Color.Parse("#4A4A4F"));
            PlaybackRate4xButton.Background = new SolidColorBrush(Color.Parse("#4A4A4F"));

            // Highlight the active playback rate button
            if (Math.Abs(_currentPlaybackRate - 1.0f) < 0.01f)
            {
                PlaybackRate1xButton.Background = new SolidColorBrush(Color.Parse("#007ACC"));
            }
            else if (Math.Abs(_currentPlaybackRate - 2.0f) < 0.01f)
            {
                PlaybackRate2xButton.Background = new SolidColorBrush(Color.Parse("#007ACC"));
            }
            else if (Math.Abs(_currentPlaybackRate - 4.0f) < 0.01f)
            {
                PlaybackRate4xButton.Background = new SolidColorBrush(Color.Parse("#007ACC"));
            }

            Logger.Debug($"Playback rate buttons updated, current rate: {_currentPlaybackRate}x");
        }

        protected override void OnClosed(EventArgs e)
        {
            Logger.Info("Application closing - Cleaning up resources");

            // Stop the timeline update timer
            _timelineUpdateTimer?.Stop();

            // Close logger window if open
            if (_loggerWindow != null && _loggerWindow.IsVisible)
            {
                Logger.Info("Closing logger window");
                _loggerWindow.Close();
                _loggerWindow = null;
            }

            // Detach VLC log event handler
            if (_libVLC != null)
            {
                _libVLC.Log -= LibVLC_Log;
            }

            // Dispose media player and VLC
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();

            Logger.Info("Application cleanup complete");
            base.OnClosed(e);
        }
    }
}
