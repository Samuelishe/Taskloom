namespace Taskloom.Infrastructure.Localization;

/// <summary>
/// Хранит текущий источник локализации для XAML.
/// </summary>
public static class LocalizationManager
{
    private static LocalizationSource? _source;

    public static LocalizationSource Source =>
        _source ?? throw new InvalidOperationException("Источник локализации ещё не инициализирован.");

    public static void Initialize(LocalizationSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }
}
