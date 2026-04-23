using System.ComponentModel;

namespace Taskloom.Infrastructure.Localization;

/// <summary>
/// Источник локализованных строк для WPF bindings.
/// </summary>
public sealed class LocalizationSource : INotifyPropertyChanged
{
    private IReadOnlyDictionary<string, string> _strings = new Dictionary<string, string>();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            return _strings.TryGetValue(key, out var value)
                ? value
                : $"[{key}]";
        }
    }

    /// <summary>
    /// Обновляет набор строк и уведомляет bindings.
    /// </summary>
    public void UpdateStrings(IReadOnlyDictionary<string, string> strings)
    {
        _strings = strings;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
