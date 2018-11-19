using System;
using System.Windows.Markup;

namespace TumblThree.Presentation.Extensions
{
    public class EnumBindingSourceExtension : MarkupExtension
    {
        private Type enumType;
        public Type EnumType
        {
            get => this.enumType;
            set
            {
                if (value == this.enumType)
                    return;

                if (null != value)
                {
                    Type enumType = Nullable.GetUnderlyingType(value) ?? value;

                    if (!enumType.IsEnum)
                        throw new ArgumentException("Type must be for an Enum.");
                }

                this.enumType = value;
            }
        }

        public EnumBindingSourceExtension() { }

        public EnumBindingSourceExtension(Type enumType) => this.EnumType = enumType;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (null == this.enumType)
                throw new InvalidOperationException("The EnumType must be specified.");

            Type actualEnumType = Nullable.GetUnderlyingType(this.enumType) ?? this.enumType;
            Array enumValues = Enum.GetValues(actualEnumType);

            if (actualEnumType == this.enumType)
                return enumValues;

            Array tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
            enumValues.CopyTo(tempArray, 1);
            return tempArray;
        }
    }
}
