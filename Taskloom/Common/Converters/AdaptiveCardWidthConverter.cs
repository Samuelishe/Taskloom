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
    private const double MinimumCardWidth = 260d;
    private const double MaximumCardWidth = 440d;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return 360d;
        }

        if (values[0] is not double availableWidth || double.IsNaN(availableWidth) || availableWidth <= 0d)
        {
            return 360d;
        }

        var itemCount = values[1] is int count ? count : 0;
        var usableWidth = Math.Max(availableWidth - 32d, MinimumCardWidth);

        var columns = GetColumnCount(usableWidth, itemCount);
        var totalSpacing = CardSpacing * (columns - 1);
        var calculatedWidth = (usableWidth - totalSpacing) / columns;

        return Math.Clamp(calculatedWidth, MinimumCardWidth, MaximumCardWidth);
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
