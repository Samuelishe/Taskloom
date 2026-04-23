using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Taskloom.Common.Converters;

/// <summary>
/// Преобразует путь к обложке аудио или массив байтов в BitmapImage.
/// </summary>
public sealed class AudioCoverSourceToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            if (value is byte[] bytes && bytes.Length > 0)
            {
                return CreateFromBytes(bytes);
            }

            if (value is string path && !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                return CreateFromPath(path);
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static BitmapImage CreateFromBytes(byte[] bytes)
    {
        using var memoryStream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = memoryStream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static BitmapImage CreateFromPath(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }
}
