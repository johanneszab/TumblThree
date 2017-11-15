using System.ComponentModel;

using TumblThree.Domain.Attributes;
using TumblThree.Domain.Converter;
using TumblThree.Domain.Properties;

namespace TumblThree.Domain.Models
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum GfycatTypes
    {
        [LocalizedDescription("mp4", typeof(Resources))]
        Mp4,
        [LocalizedDescription("webm", typeof(Resources))]
        Webm,
        [LocalizedDescription("webp", typeof(Resources))]
        Webp,
        [LocalizedDescription("poster", typeof(Resources))]
        Poster,
        [LocalizedDescription("max5mbgif", typeof(Resources))]
        Max5mbGif,
        [LocalizedDescription("max2mbgif", typeof(Resources))]
        Max2mbGif,
        [LocalizedDescription("mjpg", typeof(Resources))]
        Mjpg,
        [LocalizedDescription("gif", typeof(Resources))]
        Gif
    }
}
