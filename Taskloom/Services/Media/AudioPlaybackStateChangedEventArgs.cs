namespace Taskloom.Services.Media;

/// <summary>
/// Снимок текущего состояния воспроизведения аудио.
/// </summary>
public sealed class AudioPlaybackStateChangedEventArgs : EventArgs
{
    public AudioPlaybackStateChangedEventArgs(
        string? currentSourcePath,
        bool isPlaying,
        TimeSpan position,
        TimeSpan duration)
    {
        CurrentSourcePath = currentSourcePath;
        IsPlaying = isPlaying;
        Position = position;
        Duration = duration;
    }

    public string? CurrentSourcePath { get; }

    public bool IsPlaying { get; }

    public TimeSpan Position { get; }

    public TimeSpan Duration { get; }
}
