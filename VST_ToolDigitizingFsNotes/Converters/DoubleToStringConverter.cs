using System.Globalization;
using System.Windows.Data;

namespace VST_ToolDigitizingFsNotes.AppMain.Converters
{
    public class DoubleToStringConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            double number = (double)value;

            return number.ToString("#,0.###", CultureInfo.InvariantCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
