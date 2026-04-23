using CommunityToolkit.Mvvm.ComponentModel;
using Taskloom.Domain;
using Taskloom.Services.Localization;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Вариант статуса события для редактора записи.
/// </summary>
public sealed class EventStatusOptionViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly string _titleKey;

    public EventStatusOptionViewModel(
        ILocalizationService localizationService,
        string titleKey,
        EventStatus status)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        ArgumentException.ThrowIfNullOrWhiteSpace(titleKey);

        _titleKey = titleKey;
        Status = status;

        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    public EventStatus Status { get; }

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
