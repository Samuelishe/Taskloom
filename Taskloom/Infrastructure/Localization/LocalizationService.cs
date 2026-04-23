using System.Globalization;
using System.IO;
using System.Text.Json;
using Taskloom.Services.Localization;

namespace Taskloom.Infrastructure.Localization;

/// <summary>
/// Загружает локализованные строки из JSON-файлов.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private readonly string _localizationDirectoryPath;
    private readonly string _defaultCultureName;
    private readonly LocalizationSource _source;
    private IReadOnlyDictionary<string, string> _strings = new Dictionary<string, string>();

    public LocalizationService(string localizationDirectoryPath, string defaultCultureName = "ru-RU")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localizationDirectoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCultureName);

        _localizationDirectoryPath = localizationDirectoryPath;
        _defaultCultureName = defaultCultureName;
        _source = new LocalizationSource();
    }

    public string CurrentCultureName { get; private set; } = string.Empty;

    public LocalizationSource Source => _source;

    public event EventHandler? LanguageChanged;

    public string GetString(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _strings.TryGetValue(key, out var value)
            ? value
            : $"[{key}]";
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(CultureInfo.CurrentUICulture, GetString(key), args);
    }

    public async Task SetCultureAsync(string cultureName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureName);

        var strings = await LoadStringsAsync(cultureName, cancellationToken);
        _strings = strings;
        CurrentCultureName = cultureName;
        _source.UpdateStrings(strings);

        var cultureInfo = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task<IReadOnlyDictionary<string, string>> LoadStringsAsync(
        string cultureName,
        CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_localizationDirectoryPath, $"{cultureName}.json");

        if (!File.Exists(filePath))
        {
            if (!string.Equals(cultureName, _defaultCultureName, StringComparison.OrdinalIgnoreCase))
            {
                return await LoadStringsAsync(_defaultCultureName, cancellationToken);
            }

            throw new FileNotFoundException("Не найден файл локализации.", filePath);
        }

        await using var stream = File.OpenRead(filePath);
        var strings = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, cancellationToken: cancellationToken);

        if (strings is null)
        {
            throw new InvalidOperationException($"Не удалось прочитать файл локализации {filePath}.");
        }

        return strings;
    }
}
