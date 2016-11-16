using System;
using System.Globalization;
using System.Windows.Controls;

namespace TumblThree.Presentation.ValidationRules
{
    public class UIntRangeRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            UInt32 temp = 0;
            try
            {
                if (UInt32.TryParse((string)value, out temp))
                    return new ValidationResult(true, null);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, "Illegal characters or " + e.Message);
            }
            return new ValidationResult(false, "Please enter a legal UInt32.");
        }
    }
}
