using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LaunchPlugin
{
    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = (value ?? string.Empty).ToString().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return Brushes.Transparent;
            }

            try
            {
                var brush = (Brush)new BrushConverter().ConvertFromString(text);
                return brush ?? Brushes.Transparent;
            }
            catch
            {
                return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
