using System;
using System.ComponentModel;

using TumblThree.Domain.Attributes;
using TumblThree.Domain.Converter;
using TumblThree.Domain.Properties;

namespace TumblThree.Domain.Models
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum TumblrBlogCrawlerTypes
    {
        [LocalizedDescription("TumblrAPI", typeof(Resources))]
        TumblrAPI,

        [LocalizedDescription("TumblrSVC", typeof(Resources))]
        TumblrSVC
    }

    public static class TumblrBlogCrawlerTypesMethods
    {

        public static BlogTypes MapToBlogType(this TumblrBlogCrawlerTypes tumblrBlogType)
        {
            if (tumblrBlogType == TumblrBlogCrawlerTypes.TumblrAPI)
                return BlogTypes.tumblr;
            else if (tumblrBlogType == TumblrBlogCrawlerTypes.TumblrSVC)
                return BlogTypes.tmblrpriv;
            else
                throw new ArgumentException("Could not map Tumblr Blog to Tumblr Blog Crawler Implementation.", nameof(tumblrBlogType));
        }
    }
}
