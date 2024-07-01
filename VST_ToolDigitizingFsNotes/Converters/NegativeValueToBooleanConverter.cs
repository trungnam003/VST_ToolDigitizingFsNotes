using System.Windows.Data;

namespace VST_ToolDigitizingFsNotes.AppMain.Converters
{
    public class NegativeValueToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // negative -1, zero 0, positive 1
            var intValue = (double)value;
            return intValue == 0 ? 0 : intValue < 0 ? -1 : 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
