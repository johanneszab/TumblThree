using System.Globalization;
using System.Windows.Controls;

using TumblThree.Presentation.Properties;

namespace TumblThree.Presentation.ValidationRules
{
    public class IntRangeRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                var temp = 0;
                if (int.TryParse((string)value, out temp))
                {
                    return new ValidationResult(true, null);
                }
            }
            catch
            {
                return new ValidationResult(false, string.Format(CultureInfo.CurrentCulture, Resources.IntRangeError));
            }
            return new ValidationResult(false, string.Format(CultureInfo.CurrentCulture, Resources.IntTypeError));
        }
    }
}
