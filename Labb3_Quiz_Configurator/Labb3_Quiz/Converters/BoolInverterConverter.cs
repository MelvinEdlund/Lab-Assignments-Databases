using System;
using System.Globalization;
using System.Windows.Data;

namespace Labb3_Quiz_MongoDB.Converters;

// Inverterar bool-värden (true → false, false → true)
public class BoolInverterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }
}

