using System;

namespace TumblThree.Presentation.Converters
{
    internal static class ConverterHelper
    {
        public static bool IsParameterSet(string expectedParameter, object actualParameter)
        {
            var parameter = actualParameter as string;
            return !string.IsNullOrEmpty(parameter) &&
                   string.Equals(parameter, expectedParameter, StringComparison.OrdinalIgnoreCase);
        }
    }
}
