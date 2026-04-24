using System.IO;
using System.Text.Json;
using Taskloom.Services.Settings;

namespace Taskloom.Infrastructure.Settings;

/// <summary>
/// Хранит настройки приложения в отдельном JSON-файле.
/// </summary>
public sealed class AppSettingsService : IAppSettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;
    private readonly SemaphoreSlim _settingsLock = new(1, 1);

    public AppSettingsService(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        _settingsPath = settingsPath;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _settingsLock.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            await using var stream = File.OpenRead(_settingsPath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions, cancellationToken);
            return settings ?? new AppSettings();
        }
        finally
        {
            _settingsLock.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _settingsLock.WaitAsync(cancellationToken);

        try
        {
            var directoryPath = Path.GetDirectoryName(_settingsPath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            await using var stream = File.Create(_settingsPath);
            await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken);
        }
        finally
        {
            _settingsLock.Release();
        }
    }
}
