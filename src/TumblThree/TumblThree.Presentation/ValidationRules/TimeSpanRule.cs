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
            if (((string)value).Length > 7)
            {
                TimeSpan temp;
                if (TimeSpan.TryParse((string)value, out temp))
                {
                    return new ValidationResult(true, null);
                }
            }
            return new ValidationResult(false, string.Format(CultureInfo.CurrentCulture, Resources.TimeSpanTypeError));
        }
    }
}
