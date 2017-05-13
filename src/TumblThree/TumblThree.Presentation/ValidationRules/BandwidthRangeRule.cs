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
                long temp = long.Parse((string)value);

                if (temp >= 0 && temp <= (long.MaxValue / 1024))
                {
                    return new ValidationResult(true, null);
                }
            }
            catch
            {
                return new ValidationResult(false, string.Format(CultureInfo.CurrentCulture, Resources.IntTypeError));
            }
            return new ValidationResult(false, string.Format(CultureInfo.CurrentCulture, Resources.BandwidthRangeError));
        }
    }
}
