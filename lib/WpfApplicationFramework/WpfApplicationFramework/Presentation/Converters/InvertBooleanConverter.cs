using System.Globalization;
using System.Windows.Data;

namespace System.Waf.Presentation.Converters
{
    /// <summary>
    /// Value converter that inverts a boolean value.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InvertBooleanConverter : IValueConverter
    {
        private static readonly InvertBooleanConverter defaultInstance = new InvertBooleanConverter();

        /// <summary>
        /// Gets the default instance of this converter.
        /// </summary>
        public static InvertBooleanConverter Default { get { return defaultInstance; } }


        /// <summary>
        /// Converts a boolean value into the inverted value.
        /// </summary>
        /// <param name="value">The boolean value to invert.</param>
        /// <param name="targetType">The type of the binding target property. This parameter will be ignored.</param>
        /// <param name="parameter">The converter parameter to use. This parameter will be ignored.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The inverter boolean value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        /// <summary>
        /// Converts a boolean value into the inverted value.
        /// </summary>
        /// <param name="value">The boolean value to invert.</param>
        /// <param name="targetType">The type to convert to. This parameter will be ignored.</param>
        /// <param name="parameter">The converter parameter to use. This parameter will be ignored.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The inverter boolean value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }
}
