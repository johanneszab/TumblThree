using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using TumblThree.Applications.DataModels.TumblrSvcJson;
using TumblThree.Domain;

namespace TumblThree.Applications.Parser
{
    public class TumblrSvcJsonToJsonParser<T> : ITumblrToTextParser<T> where T : Post
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
            postCopy.blog = null;
            postCopy.trail = null;
            postCopy.share_popover_data = null;

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(postCopy.GetType());

            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, postCopy);
                return JsonFormatter.FormatOutput(Encoding.UTF8.GetString(ms.ToArray()));
            }
        }
    }
}
