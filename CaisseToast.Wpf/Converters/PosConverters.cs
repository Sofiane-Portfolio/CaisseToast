using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CaisseToast.Wpf.Models;

namespace CaisseToast.Wpf.Converters;

public sealed class TableStatusToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TableStatus status)
            return new SolidColorBrush(Colors.White);

        var color = status switch
        {
            TableStatus.Occupied => "#084F8C",
            TableStatus.Bill => "#FB923C",
            TableStatus.Paying => "#F59E0B",
            TableStatus.Reserved => "#F8FBFF",
            TableStatus.Dirty => "#E5E7EB",
            TableStatus.Unavailable => "#1F2937",
            _ => "#FFFFFF"
        };
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class TableStatusToForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TableStatus status)
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B128C")!);

        var color = status switch
        {
            TableStatus.Occupied or TableStatus.Bill or TableStatus.Paying or TableStatus.Unavailable => "#FFFFFF",
            TableStatus.Dirty => "#6B7280",
            _ => "#0B128C"
        };
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class StringEqualsToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
