using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using TumblThree.Applications.DataModels.TumblrApiJson;
using TumblThree.Domain;

namespace TumblThree.Applications.Parser
{
    public class TumblrApiJsonToJsonParser<T> : ITumblrToTextParser<T> where T : Post
    {
        public string ParseText(T post)
        {
            return GetPostAsString(post);
        }

        public string ParseQuote(T post)
        {
            return GetPostAsString(post);
        }

        public string ParseLink(T post)
        {
            return GetPostAsString(post);
        }

        public string ParseConversation(T post)
        {
            return GetPostAsString(post);
        }

        public string ParseAnswer(T post)
        {
            return GetPostAsString(post);
        }

        public string ParsePhotoMeta(T post)
        {
            return GetPostAsString(post);
        }

        public string ParseVideoMeta(T post)
        {
            return GetPostAsString(post);
        }

        public string ParseAudioMeta(T post)
        {
            return GetPostAsString(post);
        }

        private string GetPostAsString(T post)
        {
            var postCopy = (Post)post.Clone();
            postCopy.tumblelog = new Tumblelog2();
            postCopy.reblogged_from_avatar_url_128 = null;
            postCopy.reblogged_from_avatar_url_16 = null;
            postCopy.reblogged_from_avatar_url_24 = null;
            postCopy.reblogged_from_avatar_url_30 = null;
            postCopy.reblogged_from_avatar_url_40 = null;
            postCopy.reblogged_from_avatar_url_48 = null;
            postCopy.reblogged_from_avatar_url_512 = null;
            postCopy.reblogged_from_avatar_url_64 = null;
            postCopy.reblogged_from_avatar_url_96 = null;

            postCopy.reblogged_root_avatar_url_128 = null;
            postCopy.reblogged_root_avatar_url_16 = null;
            postCopy.reblogged_root_avatar_url_24 = null;
            postCopy.reblogged_root_avatar_url_30 = null;
            postCopy.reblogged_root_avatar_url_40 = null;
            postCopy.reblogged_root_avatar_url_48 = null;
            postCopy.reblogged_root_avatar_url_512 = null;
            postCopy.reblogged_root_avatar_url_64 = null;
            postCopy.reblogged_root_avatar_url_96 = null;

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(postCopy.GetType());

            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, postCopy);
                return JsonFormatter.FormatOutput(Encoding.UTF8.GetString(ms.ToArray()));
            }
        }
    }
}
