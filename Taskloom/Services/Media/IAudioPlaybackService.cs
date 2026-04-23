namespace Taskloom.Services.Media;

/// <summary>
/// Управляет единым аудиоплеером приложения.
/// </summary>
public interface IAudioPlaybackService : IDisposable
{
    event EventHandler<AudioPlaybackStateChangedEventArgs>? PlaybackStateChanged;

    void TogglePlayback(string sourcePath);

    void Seek(string sourcePath, TimeSpan position);

    void Stop();
}
