using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using TumblThree.Applications.Properties;

namespace TumblThree.Applications.Parser
{
    public class TumblrXmlToTextParser : ITumblrXmlToTextParser
    {
        public string ParsePhotoMeta(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + 
                string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.PhotoUrl, post.Element("photo-url").Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.PhotoCaption, post.Element("photo-caption")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) + Environment.NewLine;
        }

        public string ParseVideoMeta(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + 
                string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.VideoPlayer, post.Element("video-player")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) + Environment.NewLine;
        }

        public string ParseAudioMeta(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + 
                string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.AudioCaption, post.Element("audio-caption")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Id3Artist, post.Element("id3-artist")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Id3Title, post.Element("id3-title")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.Element("id3-track")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Id3Album, post.Element("id3-album")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Id3Year, post.Element("id3-year")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) + Environment.NewLine;
        }

        public string ParseConversation(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + 
                string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Conversation, post.Element("conversation-text")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) + Environment.NewLine;
        }

        public string ParseLink(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + 
                string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Link, post.Element("link-text")?.Value) + Environment.NewLine + post.Element("link-url")?.Value + Environment.NewLine + post.Element("link-description")?.Value + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) + Environment.NewLine;
        }

        public string ParseQuote(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + 
                string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.Element("quote-text")?.Value) + Environment.NewLine + post.Element("quote-source")?.Value + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) + Environment.NewLine;
        }

        public string ParseText(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + 
                string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Title, post.Element("regular-title")?.Value) + Environment.NewLine + post.Element("regular-body")?.Value + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) + Environment.NewLine;
        }

        public string ParseAnswer(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + 
                string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) + Environment.NewLine + post.Element("question")?.Value + Environment.NewLine + post.Element("answer")?.Value + Environment.NewLine + 
                string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) + Environment.NewLine;
        }
    }
}
