using System.ComponentModel;

using TumblThree.Domain.Attributes;
using TumblThree.Domain.Converter;
using TumblThree.Domain.Properties;

namespace TumblThree.Domain.Models
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum MetadataType
    {
        [LocalizedDescription("text", typeof(Resources))]
        Text,
        [LocalizedDescription("json", typeof(Resources))]
        Json
    }
}
