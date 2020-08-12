using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

using TumblThree.Applications.DataModels.TumblrSvcJson;
using TumblThree.Domain;

namespace TumblThree.Applications.Parser
{
    public class TumblrSvcJsonToJsonParser<T> : ITumblrToTextParser<T> where T : Post
    {
        public string ParseText(T post) => GetPostAsString(post);

        public string ParseQuote(T post) => GetPostAsString(post);

        public string ParseLink(T post) => GetPostAsString(post);

        public string ParseConversation(T post) => GetPostAsString(post);

        public string ParseAnswer(T post) => GetPostAsString(post);

        public string ParsePhotoMeta(T post) => GetPostAsString(post);

        public string ParseVideoMeta(T post) => GetPostAsString(post);

        public string ParseAudioMeta(T post) => GetPostAsString(post);

        private string GetPostAsString(T post)
        {
            var postCopy = (Post)post.Clone();
            postCopy.Blog = null;
            postCopy.Trail = null;
            postCopy.SharePopoverData = null;

            var serializer = new DataContractJsonSerializer(postCopy.GetType());

            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, postCopy);
                return JsonFormatter.FormatOutput(Encoding.UTF8.GetString(ms.ToArray()));
            }
        }
    }
}
