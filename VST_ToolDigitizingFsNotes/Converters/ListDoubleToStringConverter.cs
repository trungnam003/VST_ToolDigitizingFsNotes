using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace VST_ToolDigitizingFsNotes.AppMain.Converters
{
    public class ListDoubleToStringConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            // try parse list of double
            if (value is not List<double> numbers)
                return null;

            if(numbers.Count < 2)
                return null;

            StringBuilder sb = new StringBuilder();
            foreach (var number in numbers)
            {
                sb.Append(number.ToString("#,0.###", CultureInfo.InvariantCulture));
                sb.Append(" + ");
            }
            return sb.ToString().TrimEnd(' ', '+');
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
