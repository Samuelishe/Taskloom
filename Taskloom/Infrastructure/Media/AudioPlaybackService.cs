using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using Taskloom.Services.Media;

namespace Taskloom.Infrastructure.Media;

/// <summary>
/// WPF-реализация единого аудиоплеера приложения.
/// </summary>
public sealed class AudioPlaybackService : IAudioPlaybackService
{
    private readonly MediaPlayer _player;
    private readonly DispatcherTimer _timer;
    private string? _currentSourcePath;
    private bool _isPlaying;
    private TimeSpan _duration;

    public AudioPlaybackService()
    {
        _player = new MediaPlayer();
        _player.MediaOpened += OnMediaOpened;
        _player.MediaEnded += OnMediaEnded;
        _player.MediaFailed += OnMediaFailed;

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _timer.Tick += OnTimerTick;
    }

    public event EventHandler<AudioPlaybackStateChangedEventArgs>? PlaybackStateChanged;

    public void TogglePlayback(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        if (!File.Exists(sourcePath))
        {
            Stop();
            return;
        }

        if (string.Equals(_currentSourcePath, sourcePath, StringComparison.OrdinalIgnoreCase))
        {
            if (_isPlaying)
            {
                _player.Pause();
                _isPlaying = false;
                _timer.Stop();
            }
            else
            {
                _player.Play();
                _isPlaying = true;
                _timer.Start();
            }

            RaisePlaybackStateChanged();
            return;
        }

        _player.Open(new Uri(sourcePath, UriKind.Absolute));
        _player.Play();

        _currentSourcePath = sourcePath;
        _duration = TimeSpan.Zero;
        _isPlaying = true;
        _timer.Start();
        RaisePlaybackStateChanged();
    }

    public void Seek(string sourcePath, TimeSpan position)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        if (!string.Equals(_currentSourcePath, sourcePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var clampedPosition = position < TimeSpan.Zero
            ? TimeSpan.Zero
            : (_duration > TimeSpan.Zero && position > _duration ? _duration : position);

        _player.Position = clampedPosition;
        RaisePlaybackStateChanged();
    }

    public void Stop()
    {
        _player.Stop();
        _timer.Stop();
        _currentSourcePath = null;
        _isPlaying = false;
        _duration = TimeSpan.Zero;
        RaisePlaybackStateChanged();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _player.MediaOpened -= OnMediaOpened;
        _player.MediaEnded -= OnMediaEnded;
        _player.MediaFailed -= OnMediaFailed;
        _player.Close();
    }

    private void OnMediaOpened(object? sender, EventArgs e)
    {
        _duration = _player.NaturalDuration.HasTimeSpan
            ? _player.NaturalDuration.TimeSpan
            : TimeSpan.Zero;
        RaisePlaybackStateChanged();
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        _player.Stop();
        _timer.Stop();
        _isPlaying = false;
        RaisePlaybackStateChanged();
    }

    private void OnMediaFailed(object? sender, ExceptionEventArgs e)
    {
        Stop();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        RaisePlaybackStateChanged();
    }

    private void RaisePlaybackStateChanged()
    {
        PlaybackStateChanged?.Invoke(
            this,
            new AudioPlaybackStateChangedEventArgs(
                _currentSourcePath,
                _isPlaying,
                _currentSourcePath is null ? TimeSpan.Zero : _player.Position,
                _duration));
    }
}
