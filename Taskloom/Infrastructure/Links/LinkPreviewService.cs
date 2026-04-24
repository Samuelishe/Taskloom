using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Security.Cryptography;
using Taskloom.Common.Text;
using Taskloom.Infrastructure.Storage;
using Taskloom.Services.Links;

namespace Taskloom.Infrastructure.Links;

/// <summary>
/// Общий движок обогащения ссылок preview-данными с локальным кэшем внутри каталога записи.
/// </summary>
public sealed class LinkPreviewService : ILinkPreviewService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
    private static readonly TimeSpan FailureRetryDelay = TimeSpan.FromHours(6);

    private readonly Dictionary<DetectedLinkKind, ILinkPreviewProvider> _providers;
    private readonly SemaphoreSlim _metadataGate = new(1, 1);
    private readonly SemaphoreSlim _networkGate = new(3, 3);
    private readonly HttpClient _httpClient;

    public LinkPreviewService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(4)
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Taskloom/1.0");

        _providers = new Dictionary<DetectedLinkKind, ILinkPreviewProvider>
        {
            [DetectedLinkKind.YouTubeVideo] = new YouTubeLinkPreviewProvider(_httpClient)
        };
    }

    public IReadOnlyList<DetectedLink> DetectLinks(string? text)
    {
        return LinkClassifier.DetectLinks(text);
    }

    public bool CanPreview(DetectedLink link)
    {
        ArgumentNullException.ThrowIfNull(link);
        return _providers.ContainsKey(link.Kind);
    }

    public async Task<LinkPreviewData> GetOrRefreshPreviewAsync(Guid recordId, DetectedLink link, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(link);

        var metadata = await LoadMetadataAsync(recordId, cancellationToken);
        var existingEntry = metadata.FirstOrDefault(item =>
            string.Equals(item.CanonicalUrl, link.CanonicalUrl, StringComparison.OrdinalIgnoreCase));

        if (TryCreateCachedPreview(recordId, link, existingEntry, out var cachedPreview))
        {
            return cachedPreview;
        }

        if (ShouldUseRecentFailurePlaceholder(existingEntry))
        {
            return CreatePlaceholder(link, existingEntry?.Title, existingEntry?.Description, existingEntry?.DurationText);
        }

        if (!_providers.TryGetValue(link.Kind, out var provider))
        {
            await SaveMetadataEntryAsync(recordId, link, existingEntry?.Title, existingEntry?.Description, existingEntry?.DurationText, null, false, cancellationToken);
            return CreatePlaceholder(link, existingEntry?.Title, existingEntry?.Description, existingEntry?.DurationText);
        }

        await _networkGate.WaitAsync(cancellationToken);

        try
        {
            var fetchResult = await provider.FetchAsync(link, cancellationToken);

            if (fetchResult is null)
            {
                await SaveMetadataEntryAsync(recordId, link, existingEntry?.Title, existingEntry?.Description, existingEntry?.DurationText, null, false, cancellationToken);
                return CreatePlaceholder(link, existingEntry?.Title, existingEntry?.Description, existingEntry?.DurationText);
            }

            var thumbnailAbsolutePath = await DownloadThumbnailAsync(recordId, link, fetchResult, cancellationToken);

            if (string.IsNullOrWhiteSpace(thumbnailAbsolutePath))
            {
                await SaveMetadataEntryAsync(recordId, link, fetchResult.Title, fetchResult.Description, fetchResult.DurationText, null, false, cancellationToken);
                return CreatePlaceholder(link, fetchResult.Title, fetchResult.Description, fetchResult.DurationText);
            }

            var thumbnailRelativePath = Path.GetRelativePath(
                TaskloomPaths.GetRecordDirectoryPath(recordId),
                thumbnailAbsolutePath);

            await SaveMetadataEntryAsync(recordId, link, fetchResult.Title, fetchResult.Description, fetchResult.DurationText, thumbnailRelativePath, true, cancellationToken);

            return new LinkPreviewData(
                link.OriginalUrl,
                link.CanonicalUrl,
                fetchResult.Title,
                link.SourceDisplayName,
                link.Kind,
                fetchResult.Description,
                fetchResult.DurationText,
                thumbnailAbsolutePath,
                true);
        }
        catch
        {
            await SaveMetadataEntryAsync(recordId, link, existingEntry?.Title, existingEntry?.Description, existingEntry?.DurationText, null, false, cancellationToken);
            return CreatePlaceholder(link, existingEntry?.Title, existingEntry?.Description, existingEntry?.DurationText);
        }
        finally
        {
            _networkGate.Release();
        }
    }

    private static LinkPreviewData CreatePlaceholder(
        DetectedLink link,
        string? title = null,
        string? description = null,
        string? durationText = null)
    {
        return new LinkPreviewData(
            link.OriginalUrl,
            link.CanonicalUrl,
            CreateFallbackTitle(link, title),
            link.SourceDisplayName,
            link.Kind,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            string.IsNullOrWhiteSpace(durationText) ? null : durationText.Trim(),
            null,
            false);
    }

    private static bool ShouldUseRecentFailurePlaceholder(LinkPreviewMetadataEntry? existingEntry)
    {
        return existingEntry is not null &&
               !existingEntry.IsSuccess &&
               existingEntry.LastAttemptUtc > DateTime.UtcNow - FailureRetryDelay;
    }

    private static bool TryCreateCachedPreview(
        Guid recordId,
        DetectedLink link,
        LinkPreviewMetadataEntry? existingEntry,
        out LinkPreviewData preview)
    {
        preview = null!;

        if (existingEntry is null ||
            !existingEntry.IsSuccess ||
            string.IsNullOrWhiteSpace(existingEntry.ThumbnailRelativePath))
        {
            return false;
        }

        var absoluteThumbnailPath = Path.Combine(
            TaskloomPaths.GetRecordDirectoryPath(recordId),
            existingEntry.ThumbnailRelativePath);

        if (!File.Exists(absoluteThumbnailPath))
        {
            return false;
        }

        preview = new LinkPreviewData(
            link.OriginalUrl,
            link.CanonicalUrl,
            CreateFallbackTitle(link, existingEntry.Title),
            link.SourceDisplayName,
            link.Kind,
            existingEntry.Description,
            existingEntry.DurationText,
            absoluteThumbnailPath,
            true);

        return true;
    }

    private async Task<string?> DownloadThumbnailAsync(
        Guid recordId,
        DetectedLink link,
        LinkPreviewFetchResult fetchResult,
        CancellationToken cancellationToken)
    {
        var previewsDirectoryPath = TaskloomPaths.GetRecordLinkPreviewsDirectoryPath(recordId);
        Directory.CreateDirectory(previewsDirectoryPath);

        var fileName = $"{ComputeHash(link.CanonicalUrl)}{fetchResult.ThumbnailFileExtension}";
        var absolutePath = Path.Combine(previewsDirectoryPath, fileName);

        using var response = await _httpClient.GetAsync(fetchResult.ThumbnailUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(absolutePath);
        await input.CopyToAsync(output, cancellationToken);

        return absolutePath;
    }

    private async Task<IReadOnlyList<LinkPreviewMetadataEntry>> LoadMetadataAsync(Guid recordId, CancellationToken cancellationToken)
    {
        var metadataPath = TaskloomPaths.GetRecordLinkPreviewsMetadataPath(recordId);

        if (!File.Exists(metadataPath))
        {
            return [];
        }

        await _metadataGate.WaitAsync(cancellationToken);

        try
        {
            await using var stream = File.OpenRead(metadataPath);
            var entries = await JsonSerializer.DeserializeAsync<List<LinkPreviewMetadataEntry>>(stream, cancellationToken: cancellationToken);
            return entries ?? [];
        }
        catch
        {
            return [];
        }
        finally
        {
            _metadataGate.Release();
        }
    }

    private async Task SaveMetadataEntryAsync(
        Guid recordId,
        DetectedLink link,
        string? title,
        string? description,
        string? durationText,
        string? thumbnailRelativePath,
        bool isSuccess,
        CancellationToken cancellationToken)
    {
        var recordDirectoryPath = TaskloomPaths.GetRecordDirectoryPath(recordId);
        Directory.CreateDirectory(recordDirectoryPath);

        var metadataPath = TaskloomPaths.GetRecordLinkPreviewsMetadataPath(recordId);
        await _metadataGate.WaitAsync(cancellationToken);

        try
        {
            List<LinkPreviewMetadataEntry> entries;

            if (File.Exists(metadataPath))
            {
                await using var input = File.OpenRead(metadataPath);
                entries = await JsonSerializer.DeserializeAsync<List<LinkPreviewMetadataEntry>>(input, cancellationToken: cancellationToken) ?? [];
            }
            else
            {
                entries = [];
            }

            entries.RemoveAll(item => string.Equals(item.CanonicalUrl, link.CanonicalUrl, StringComparison.OrdinalIgnoreCase));
            entries.Add(new LinkPreviewMetadataEntry(
                link.CanonicalUrl,
                link.Kind.ToString(),
                string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
                string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                string.IsNullOrWhiteSpace(durationText) ? null : durationText.Trim(),
                thumbnailRelativePath,
                isSuccess,
                DateTime.UtcNow));

            await using var output = File.Create(metadataPath);
            await JsonSerializer.SerializeAsync(output, entries, SerializerOptions, cancellationToken);
        }
        finally
        {
            _metadataGate.Release();
        }
    }

    private static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string CreateFallbackTitle(DetectedLink link, string? title)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title.Trim();
        }

        return link.Kind switch
        {
            DetectedLinkKind.YouTubeVideo => "YouTube video",
            DetectedLinkKind.TwitchClip => "Twitch clip",
            DetectedLinkKind.TwitchVideo => "Twitch video",
            DetectedLinkKind.VkVideo => "VK video",
            DetectedLinkKind.RuTubeVideo => "RuTube video",
            _ => link.SourceDisplayName
        };
    }

    private sealed record LinkPreviewMetadataEntry(
        string CanonicalUrl,
        string Kind,
        string? Title,
        string? Description,
        string? DurationText,
        string? ThumbnailRelativePath,
        bool IsSuccess,
        DateTime LastAttemptUtc);
}
