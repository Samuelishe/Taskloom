using CommunityToolkit.Mvvm.ComponentModel;
using Taskloom.Services.Media;
using System.IO;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Presentation-модель аудиофайла записи с состоянием проигрывания.
/// </summary>
public sealed partial class RecordAudioListItemViewModel : ObservableObject, IDisposable
{
    private readonly IAudioPlaybackService _audioPlaybackService;
    private readonly TimeSpan _metadataDuration;
    private bool _isSeeking;

    public RecordAudioListItemViewModel(
        string path,
        string fileName,
        string displayTitle,
        string? coverPath,
        double? durationSeconds,
        string? albumTitle,
        string? genre,
        IAudioPlaybackService audioPlaybackService)
    {
        Path = path;
        FileName = fileName;
        DisplayTitle = displayTitle;
        CoverPath = coverPath;
        AlbumTitle = albumTitle;
        Genre = genre;
        _metadataDuration = durationSeconds is > 0 ? TimeSpan.FromSeconds(durationSeconds.Value) : TimeSpan.Zero;
        _audioPlaybackService = audioPlaybackService ?? throw new ArgumentNullException(nameof(audioPlaybackService));
        _audioPlaybackService.PlaybackStateChanged += OnPlaybackStateChanged;
        ProgressMaximum = _metadataDuration.TotalSeconds > 0 ? _metadataDuration.TotalSeconds : 1;
        PlaybackDurationText = FormatTime(_metadataDuration);
    }

    [ObservableProperty]
    private bool isCurrent;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private double progressValue;

    [ObservableProperty]
    private double progressMaximum;

    [ObservableProperty]
    private string playbackPositionText = "00:00";

    [ObservableProperty]
    private string playbackDurationText = "00:00";

    public string Path { get; }

    public string FileName { get; }

    public string DisplayTitle { get; }

    public string? CoverPath { get; }

    public string? AlbumTitle { get; }

    public string? Genre { get; }

    public bool HasCover => !string.IsNullOrWhiteSpace(CoverPath);

    public bool HasDistinctFileName =>
        !string.Equals(DisplayTitle, FileName, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(DisplayTitle, System.IO.Path.GetFileNameWithoutExtension(FileName), StringComparison.OrdinalIgnoreCase);

    public bool HasMetadataLine =>
        !string.IsNullOrWhiteSpace(AlbumTitle) ||
        !string.IsNullOrWhiteSpace(Genre);

    public string MetadataLine
    {
        get
        {
            if (string.IsNullOrWhiteSpace(AlbumTitle))
            {
                return Genre ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(Genre))
            {
                return AlbumTitle;
            }

            return $"{AlbumTitle} • {Genre}";
        }
    }

    public string PlaybackGlyph => IsPlaying ? "■" : "▶";

    partial void OnIsPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(PlaybackGlyph));
    }

    public void BeginSeek()
    {
        _isSeeking = true;
    }

    public void UpdateSeekPreview(double seconds)
    {
        if (!_isSeeking)
        {
            return;
        }

        ProgressValue = Math.Min(ProgressMaximum, Math.Max(0, seconds));
        PlaybackPositionText = FormatTime(TimeSpan.FromSeconds(ProgressValue));
    }

    public void EndSeek()
    {
        _isSeeking = false;
    }

    public void Dispose()
    {
        _audioPlaybackService.PlaybackStateChanged -= OnPlaybackStateChanged;
    }

    private void OnPlaybackStateChanged(object? sender, AudioPlaybackStateChangedEventArgs e)
    {
        var isCurrentTrack = string.Equals(e.CurrentSourcePath, Path, StringComparison.OrdinalIgnoreCase);
        IsCurrent = isCurrentTrack;
        IsPlaying = isCurrentTrack && e.IsPlaying;

        var effectiveDuration = isCurrentTrack && e.Duration > TimeSpan.Zero
            ? e.Duration
            : _metadataDuration;

        ProgressMaximum = effectiveDuration.TotalSeconds > 0 ? effectiveDuration.TotalSeconds : 1;
        PlaybackDurationText = FormatTime(effectiveDuration);

        if (!isCurrentTrack)
        {
            ProgressValue = 0;
            PlaybackPositionText = "00:00";
            return;
        }

        if (_isSeeking)
        {
            return;
        }

        ProgressValue = Math.Min(ProgressMaximum, Math.Max(0, e.Position.TotalSeconds));
        PlaybackPositionText = FormatTime(e.Position);
    }

    private static string FormatTime(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            return "00:00";
        }

        return value.TotalHours >= 1
            ? value.ToString(@"hh\:mm\:ss")
            : value.ToString(@"mm\:ss");
    }
}
