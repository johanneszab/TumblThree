using System.Globalization;
using System.Windows.Data;

namespace System.Waf.Presentation.Converters
{
    /// <summary>
    /// Value converter that converts an object into a formatted string. The format specification is passed via the 
    /// ConverterParameter property.
    /// </summary>
    [ValueConversion(typeof(object), typeof(string))]
    public sealed class StringFormatConverter : IValueConverter
    {
        private static readonly StringFormatConverter defaultInstance = new StringFormatConverter();

        /// <summary>
        /// Gets the default instance of this converter.
        /// </summary>
        public static StringFormatConverter Default { get { return defaultInstance; } }


        /// <summary>
        /// Converts an object into a formatted string.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <param name="targetType">The type of the binding target property. This parameter will be ignored.</param>
        /// <param name="parameter">The format specification used to format the object.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The formatted string.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string format = parameter as string ?? "{0}";

            return string.Format(culture, format, value);
        }

        /// <summary>
        /// This method is not supported and throws an exception when it is called.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>Nothing because this method throws an exception.</returns>
        /// <exception cref="NotSupportedException">Throws this exception when the method is called.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
