using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NopyCopyV2.Converters
{
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Boolean)
            {
                var booleanVal = (bool)value;

                if (booleanVal)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
            else
            {
                if (value == null)
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Boolean)
            {
                return (bool)value;
            }
            else if (value is string)
            {
                var stringVal = value as string;
                switch (stringVal)
                {
                    case nameof(Visibility.Visible):
                        return true;
                    case nameof(Visibility.Hidden):
                    case nameof(Visibility.Collapsed):
                    default:
                        return false;
                }
            }
            else
            {
                return value;
            }
        }
    }
}
