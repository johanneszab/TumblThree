using System;
using System.Globalization;
using System.Linq;

using TumblThree.Applications.DataModels.TumblrSvcJson;
using TumblThree.Applications.Properties;

namespace TumblThree.Applications.Parser
{
    public class TumblrSvcJsonToTextParser<T> : ITumblrToTextParser<T> where T : Post
    {
        public string ParseText(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.PostUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.Summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Title, post.Title) +
                   Environment.NewLine + post.Body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseQuote(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.PostUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.Summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.Text) +
                   Environment.NewLine + post.Body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseLink(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.PostUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.Summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Link, post.Caption) +
                   Environment.NewLine + post.Body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseConversation(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.PostUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.Summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Quote,
                       post.dialogue.Select(dialogue => new { dialogue.Name, dialogue.Phrase })) +
                   Environment.NewLine + post.Body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseAnswer(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.PostUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.Summary) +
                   Environment.NewLine +
                   post.Question +
                   Environment.NewLine +
                   post.Answer +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParsePhotoMeta(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.PostUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.Summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoUrl,
                       post.Photos.Select(photo => photo.OriginalSize.Url).FirstOrDefault()) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoCaption,
                       post.Trail.Select(trail => trail.ContentRaw).FirstOrDefault()) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseVideoMeta(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.PostUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.Summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseAudioMeta(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.PostUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.Summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.AudioCaption,
                       post.Trail.Select(trail => trail.ContentRaw).FirstOrDefault()) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Artist, post.Artist) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Title, post.Title) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.Track) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Album, post.Album) +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.Year) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine;
        }
    }
}
