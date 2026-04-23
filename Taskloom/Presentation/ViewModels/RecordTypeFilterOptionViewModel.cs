using Taskloom.Domain;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Вариант фильтрации списка записей по типу.
/// </summary>
public sealed class RecordTypeFilterOptionViewModel
{
    public RecordTypeFilterOptionViewModel(string title, RecordType? recordType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title;
        RecordType = recordType;
    }

    public string Title { get; }

    public RecordType? RecordType { get; }
}
