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
        private bool _isUpdatingSliderProgrammatically = false;
        private bool _wasPlayingBeforeDrag = false;

        // Marker positions in milliseconds
        private long _startMarkerMs = 0;
        private long _endMarkerMs = 0;
        private bool _isLooping = false;

        // Frame rate (approximate, will be calculated from video)
        private double _frameDurationMs = 33.33; // Default ~30fps

        public MainWindow()
        {
            InitializeComponent();
            InitializeVLC();
            InitializeTimeline();
        }

        private void InitializeTimeline()
        {
            // Create timer to update timeline
            _timelineUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS update rate
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

                // Ensure VideoView is properly initialized and attached
                this.Opened += (sender, args) =>
                {
                    if (VideoView != null && _mediaPlayer != null)
                    {
                        VideoView.MediaPlayer = _mediaPlayer;
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing VLC: {ex.Message}");
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
                var media = new Media(_libVLC, filePath, FromType.FromPath);
                _mediaPlayer.Play(media);

                // Hide the overlay when video starts
                NoVideoOverlay.IsVisible = false;

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
                                Console.WriteLine("Warning: Media is not seekable");
                                TimelineSlider.IsEnabled = false;
                            }
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading video: {ex.Message}");
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
            }
            else
            {
                _mediaPlayer.Play();
                PlayPauseButton.Content = "⏸ Pause";
            }
        }

        private void LoopButton_Click(object? sender, RoutedEventArgs e)
        {
            _isLooping = !_isLooping;
            LoopButton.Content = _isLooping ? "🔁 Loop: ON" : "🔁 Loop: OFF";
            LoopButton.Background = _isLooping ? new SolidColorBrush(Color.Parse("#007ACC")) : new SolidColorBrush(Color.Parse("#2D2D30"));
        }

        private void TimelineUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (_mediaPlayer == null || _isUserDraggingSlider)
                return;

            try
            {
                long currentTime = _mediaPlayer.Time;
                long duration = _mediaPlayer.Length;

                if (duration > 0)
                {
                    // Update timeline slider (prevent triggering ValueChanged event)
                    _isUpdatingSliderProgrammatically = true;
                    TimelineSlider.Maximum = duration;
                    TimelineSlider.Value = currentTime;
                    _isUpdatingSliderProgrammatically = false;

                    // Update time displays
                    CurrentTimeText.Text = FormatTime(currentTime);
                    TotalTimeText.Text = FormatTime(duration);

                    // Update marker positions on timeline
                    UpdateMarkerPositions();

                    // Check for loop
                    if (_isLooping && currentTime >= _endMarkerMs)
                    {
                        _mediaPlayer.Time = _startMarkerMs;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating timeline: {ex.Message}");
            }
        }

        private void TimelineSlider_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                Console.WriteLine($"[TIMELINE] Slider pressed - Current video position: {FormatTime(_mediaPlayer.Time)} ({_mediaPlayer.Time}ms)");
            }
            else
            {
                Console.WriteLine("[TIMELINE] Slider pressed - No media player available");
            }

            _isUserDraggingSlider = true;

            // Remember playback state but don't pause yet
            if (_mediaPlayer != null)
            {
                _wasPlayingBeforeDrag = _mediaPlayer.IsPlaying;
                Console.WriteLine($"[TIMELINE] Was playing before drag: {_wasPlayingBeforeDrag}");
            }
        }

        private void TimelineSlider_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            // Update preview time while dragging
            if (_isUserDraggingSlider && _mediaPlayer != null)
            {
                long targetTime = (long)TimelineSlider.Value;
                CurrentTimeText.Text = FormatTime(targetTime);
                Console.WriteLine($"[TIMELINE] Dragging to: {FormatTime(targetTime)} ({targetTime}ms)");
            }
        }

        private void TimelineSlider_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            Console.WriteLine("[TIMELINE] ========================================");
            Console.WriteLine("[TIMELINE] Slider released - Initiating seek operation");
            _isUserDraggingSlider = false;

            if (_mediaPlayer != null && _mediaPlayer.IsSeekable)
            {
                try
                {
                    long targetTime = (long)TimelineSlider.Value;
                    long duration = _mediaPlayer.Length;
                    long positionBeforeSeek = _mediaPlayer.Time;

                    Console.WriteLine($"[TIMELINE] Video duration: {FormatTime(duration)} ({duration}ms)");
                    Console.WriteLine($"[TIMELINE] Target seek time: {FormatTime(targetTime)} ({targetTime}ms)");
                    Console.WriteLine($"[TIMELINE] Position before seek: {FormatTime(positionBeforeSeek)} ({positionBeforeSeek}ms)");
                    Console.WriteLine($"[TIMELINE] Is seekable: {_mediaPlayer.IsSeekable}");

                    if (duration > 0 && targetTime >= 0 && targetTime <= duration)
                    {
                        Console.WriteLine($"[SEEK] Performing seek to {targetTime}ms...");

                        // Directly set the Time property - this is the most straightforward approach
                        _mediaPlayer.Time = targetTime;

                        // Wait a moment for seek to complete, then verify position
                        Task.Run(async () =>
                        {
                            await Task.Delay(100); // Give VLC time to seek
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (_mediaPlayer != null)
                                {
                                    long actualPosition = _mediaPlayer.Time;
                                    long difference = Math.Abs(actualPosition - targetTime);

                                    Console.WriteLine($"[SEEK] Seek completed!");
                                    Console.WriteLine($"[SEEK] Target position: {FormatTime(targetTime)} ({targetTime}ms)");
                                    Console.WriteLine($"[SEEK] Actual position: {FormatTime(actualPosition)} ({actualPosition}ms)");
                                    Console.WriteLine($"[SEEK] Difference: {difference}ms");

                                    if (difference > 500)
                                    {
                                        Console.WriteLine($"[SEEK] ⚠ Warning: Large seek difference detected!");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[SEEK] ✓ Seek successful (within tolerance)");
                                    }
                                }
                            });
                        });
                    }
                    else
                    {
                        Console.WriteLine($"[SEEK] ✗ Invalid seek range: {targetTime} not in [0, {duration}]");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SEEK] ✗ Error during seek: {ex.Message}");
                    Console.WriteLine($"[SEEK] Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine($"[SEEK] ✗ Cannot seek - MediaPlayer null: {_mediaPlayer == null}, Seekable: {_mediaPlayer?.IsSeekable}");
            }

            Console.WriteLine("[TIMELINE] ========================================");
        }

        private void TimelineSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            // Only update time display when user is dragging, not during programmatic updates
            if (_isUserDraggingSlider && !_isUpdatingSliderProgrammatically && _mediaPlayer != null)
            {
                CurrentTimeText.Text = FormatTime((long)e.NewValue);
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
            }
        }

        private void ResetMarkersButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _startMarkerMs = 0;
                _endMarkerMs = _mediaPlayer.Length;
                UpdateMarkerDisplay();
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

            // Calculate marker positions - center them by accounting for their width
            double sliderWidth = TimelineSlider.Bounds.Width;
            double markerWidth = StartMarkerVisual.Bounds.Width; // Both markers have same width (3px)

            // Calculate the position ratio and subtract half the marker width to center it
            double startPositionRatio = (double)_startMarkerMs / duration;
            double endPositionRatio = (double)_endMarkerMs / duration;

            double startPosition = (sliderWidth * startPositionRatio) - (markerWidth / 2);
            double endPosition = (sliderWidth * endPositionRatio) - (markerWidth / 2);

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
                }
                else
                {
                    // Invalid time - show error in textbox
                    SeekTimeTextBox.Text = "Invalid range!";
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
            return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        }

        private void UpdateMetadata()
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null)
                return;

            try
            {
                var media = _mediaPlayer.Media;

                // Parse media to get metadata (with timeout)
                _ = media.Parse(MediaParseOptions.ParseLocal, 5000);

                // File information
                MetadataFilename.Text = System.IO.Path.GetFileName(_currentFilePath) ?? "-";
                MetadataDuration.Text = FormatTime(_mediaPlayer.Length);

                // Get video size if available
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

                // Get frame rate if available (VLC provides fps property)
                float fps = _mediaPlayer.Fps;
                if (fps > 0)
                {
                    MetadataFPS.Text = $"{fps:F2}";
                    _frameDurationMs = 1000.0 / fps;
                }
                else
                {
                    MetadataFPS.Text = "-";
                }

                // Get tracks
                var tracks = media.Tracks;
                int videoTrackCount = 0;
                int audioTrackCount = 0;

                foreach (var track in tracks)
                {
                    if (track.TrackType == TrackType.Video)
                    {
                        videoTrackCount++;
                    }
                    else if (track.TrackType == TrackType.Audio)
                    {
                        audioTrackCount++;
                    }
                }

                // Basic codec information
                MetadataVideoCodec.Text = videoTrackCount > 0 ? "Available" : "-";
                MetadataVideoBitrate.Text = "-";

                MetadataAudioCodec.Text = audioTrackCount > 0 ? "Available" : "-";
                MetadataAudioSampleRate.Text = "-";
                MetadataAudioChannels.Text = audioTrackCount > 0 ? audioTrackCount.ToString() + " track(s)" : "-";
                MetadataAudioBitrate.Text = "-";

                // Format information
                string extension = System.IO.Path.GetExtension(_currentFilePath)?.ToUpper().TrimStart('.') ?? "-";
                MetadataFormat.Text = extension;
                MetadataStreamCount.Text = tracks.Length.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving metadata: {ex.Message}");
                // Set defaults on error
                MetadataFilename.Text = System.IO.Path.GetFileName(_currentFilePath) ?? "-";
                MetadataDuration.Text = "-";
                MetadataResolution.Text = "-";
                MetadataFPS.Text = "-";
                MetadataVideoCodec.Text = "-";
                MetadataVideoBitrate.Text = "-";
                MetadataAudioCodec.Text = "-";
                MetadataAudioSampleRate.Text = "-";
                MetadataAudioChannels.Text = "-";
                MetadataAudioBitrate.Text = "-";
                MetadataFormat.Text = System.IO.Path.GetExtension(_currentFilePath)?.ToUpper().TrimStart('.') ?? "-";
                MetadataStreamCount.Text = "-";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _timelineUpdateTimer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            base.OnClosed(e);
        }
    }
}
