using CommunityToolkit.Mvvm.ComponentModel;
using Taskloom.Domain;
using Taskloom.Services.Localization;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Вариант фильтрации списка записей по типу.
/// </summary>
public sealed class RecordTypeFilterOptionViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly string _titleKey;

    public RecordTypeFilterOptionViewModel(
        ILocalizationService localizationService,
        string titleKey,
        RecordType? recordType)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        ArgumentException.ThrowIfNullOrWhiteSpace(titleKey);

        _titleKey = titleKey;
        RecordType = recordType;

        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    public string Title => _localizationService.GetString(_titleKey);

    public RecordType? RecordType { get; }

    public override string ToString()
    {
        return Title;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Title));
    }
}
