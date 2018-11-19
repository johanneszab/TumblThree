using System;
using System.Globalization;
using System.Windows.Controls;

using TumblThree.Presentation.Properties;

namespace TumblThree.Presentation.ValidationRules
{
    public class TimeSpanRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return (value as string).Length > 7 && TimeSpan.TryParse((string)value, out TimeSpan _)
                ? new ValidationResult(true, null)
                : new ValidationResult(false, string.Format(CultureInfo.CurrentCulture, Resources.TimeSpanTypeError));
        }
    }
}
