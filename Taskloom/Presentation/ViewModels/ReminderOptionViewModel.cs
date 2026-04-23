using CommunityToolkit.Mvvm.ComponentModel;
using Taskloom.Services.Localization;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Вариант времени напоминания о событии.
/// </summary>
public sealed class ReminderOptionViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly string _titleKey;

    public ReminderOptionViewModel(
        ILocalizationService localizationService,
        string titleKey,
        int minutesBefore)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        ArgumentException.ThrowIfNullOrWhiteSpace(titleKey);

        _titleKey = titleKey;
        MinutesBefore = minutesBefore;

        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    public int MinutesBefore { get; }

    public string Title => _localizationService.GetString(_titleKey);

    public override string ToString()
    {
        return Title;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Title));
    }
}
