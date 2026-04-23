using System.Globalization;
using System.Windows.Data;

namespace Taskloom.Common.Converters;

/// <summary>
/// Вычисляет ширину карточки записи по доступной ширине списка и количеству элементов.
/// </summary>
public sealed class AdaptiveCardWidthConverter : IMultiValueConverter
{
    private const double CardSpacing = 12d;
    private const double WideThreshold = 1280d;
    private const double MediumThreshold = 860d;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return 320d;
        }

        if (values[0] is not double availableWidth || double.IsNaN(availableWidth) || availableWidth <= 0d)
        {
            return 320d;
        }

        var itemCount = values[1] is int count ? count : 0;
        var usableWidth = Math.Max(availableWidth - 32d, 220d);

        var columns = GetColumnCount(usableWidth, itemCount);
        var totalSpacing = CardSpacing * (columns - 1);

        return Math.Max((usableWidth - totalSpacing) / columns, 220d);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static int GetColumnCount(double usableWidth, int itemCount)
    {
        if (itemCount <= 1)
        {
            return 1;
        }

        if (usableWidth >= WideThreshold && itemCount >= 3)
        {
            return 3;
        }

        if (usableWidth >= MediumThreshold && itemCount >= 2)
        {
            return 2;
        }

        return 1;
    }
}
