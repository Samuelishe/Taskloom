using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Taskloom.Common.Converters;

/// <summary>
/// Преобразует абсолютный путь к изображению в BitmapImage с ограничением размера декодирования.
/// </summary>
public sealed class ImagePathToBitmapConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);

        if (parameter is not null &&
            int.TryParse(parameter.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var decodePixelWidth) &&
            decodePixelWidth > 0)
        {
            bitmap.DecodePixelWidth = decodePixelWidth;
        }

        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
