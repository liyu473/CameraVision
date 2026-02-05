using MvCameraControl;
using System.Globalization;
using System.Windows.Data;

namespace CameraVision.Converters;

public class DeviceInfoToDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IDeviceInfo device)
            return string.Empty;

        var prefix = $"{device.TLayerType}: ";

        if (!string.IsNullOrWhiteSpace(device.UserDefinedName))
        {
            return $"{prefix}{device.UserDefinedName} ({device.SerialNumber})";
        }

        return $"{prefix}{device.ManufacturerName} {device.ModelName} ({device.SerialNumber})";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;
}
