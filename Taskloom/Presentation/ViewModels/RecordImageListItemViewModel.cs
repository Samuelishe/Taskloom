namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Модель миниатюры изображения для отображения в карточке записи.
/// </summary>
public sealed class RecordImageListItemViewModel
{
    public RecordImageListItemViewModel(string path, string fileName)
    {
        Path = path;
        FileName = fileName;
    }

    public string Path { get; }

    public string FileName { get; }
}
