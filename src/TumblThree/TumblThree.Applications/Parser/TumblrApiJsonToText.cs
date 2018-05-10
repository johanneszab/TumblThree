using System;
using System.Globalization;
using System.Linq;
using TumblThree.Applications.DataModels.TumblrApiJson;
using TumblThree.Applications.Properties;

namespace TumblThree.Applications.Parser
{
    public class TumblrApiJsonToTextParser<T> : ITumblrToTextParser<T> where T : Post
    {
        public string ParseText(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.url_with_slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Title, post.regular_title) +
                   Environment.NewLine + post.regular_body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseQuote(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.url_with_slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.quote_text) +
                   Environment.NewLine + post.quote_source +
                   Environment.NewLine + post.regular_body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseLink(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.url_with_slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Link, post.link_description) +
                   Environment.NewLine + post.link_text +
                   Environment.NewLine + post.link_url +
                   Environment.NewLine + post.regular_body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseConversation(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.url_with_slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.conversation.Select(dialogue => new { dialogue.name, dialogue.phrase })) +
                   Environment.NewLine + post.regular_body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseAnswer(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.url_with_slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   post.question +
                   Environment.NewLine +
                   post.answer +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParsePhotoMeta(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.url_with_slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoUrl, post.photo_url_1280) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoSetUrl, String.Join(" ", post.photos.Select(photo => photo.photo_url_1280))) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoCaption, post.photo_caption) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseVideoMeta(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.url_with_slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.VideoCaption, post.video_caption) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.VideoSource, post.video_source) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.VideoPlayer, post.video_player) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseAudioMeta(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.url_with_slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.AudioCaption, post.audio_caption) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Artist, post.id3_artist) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Title, post.id3_title) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.id3_track) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Album, post.id3_album) +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.id3_year) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }
    }
}
