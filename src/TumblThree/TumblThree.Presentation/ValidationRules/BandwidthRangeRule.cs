using System.Globalization;
using System.Windows.Controls;

using TumblThree.Presentation.Properties;

namespace TumblThree.Presentation.ValidationRules
{
    public class BandwidthRangeRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                int temp = int.Parse((string)value);

                if (temp > 0 && temp <= 2000000)
                {
                    return new ValidationResult(true, null);
                }
            }
            catch
            {
                return new ValidationResult(false, string.Format(CultureInfo.CurrentCulture, Resources.IntegerTypeError));
            }
            return new ValidationResult(false, string.Format(CultureInfo.CurrentCulture, Resources.IntegerRangeError));
        }
    }
}
