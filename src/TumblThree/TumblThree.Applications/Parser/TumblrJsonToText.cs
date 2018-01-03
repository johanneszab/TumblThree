using System;
using System.Globalization;
using System.Linq;
using TumblThree.Applications.DataModels.TumblrSvcJson;
using TumblThree.Applications.Properties;

namespace TumblThree.Applications.Parser
{
    public class TumblrJsonToTextParser : ITumblrJsonToTextParser
    {
        public string ParseText(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Title, post.title) +
                   Environment.NewLine + post.body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseQuote(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.text) +
                   Environment.NewLine + post.body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseLink(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Link, post.caption) +
                   Environment.NewLine + post.body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseConversation(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.dialogue.Select(dialogue => new { dialogue.name, dialogue.phrase })) +
                   Environment.NewLine + post.body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseAnswer(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   post.question +
                   Environment.NewLine +
                   post.answer +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParsePhotoMeta(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoUrl, post.photos.Select(photo => photo.original_size.url).FirstOrDefault()) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoCaption, post.trail.Select(trail => trail.content_raw).FirstOrDefault()) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseVideoMeta(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseAudioMeta(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.AudioCaption, post.trail.Select(trail => trail.content_raw).FirstOrDefault()) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Artist, post.artist) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Title, post.title) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.track) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Album, post.album) +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.year) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }
    }
}
