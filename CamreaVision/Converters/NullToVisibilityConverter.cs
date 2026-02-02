using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CamreaVision.Converters;

/// <summary>
/// 将 null 值转换为 Visibility 的转换器
/// null 时显示（Visible），非 null 时隐藏（Collapsed）
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
