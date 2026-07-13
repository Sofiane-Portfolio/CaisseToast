using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CaisseToast.Wpf.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

public sealed class PinDotFillConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int filled || parameter is not string s || !int.TryParse(s, out var index))
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E5EC")!);
        return filled > index
            ? new SolidColorBrush((Color)Application.Current.FindResource("Color.Navy"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E5EC")!);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrWhiteSpace(s) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
