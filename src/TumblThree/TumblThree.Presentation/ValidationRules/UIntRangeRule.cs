using System.Globalization;
using System.Windows.Controls;

using TumblThree.Presentation.Properties;

namespace TumblThree.Presentation.ValidationRules
{
    public class UIntRangeRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                uint temp = 0;
                if (uint.TryParse((string)value, out temp))
                {
                    return new ValidationResult(true, null);
                }
            }
            catch
            {
                return new ValidationResult(false, string.Format(CultureInfo.CurrentCulture, Resources.UIntRangeError));
            }
            return new ValidationResult(false, string.Format(CultureInfo.CurrentCulture, Resources.UIntTypeError));
        }
    }
}
