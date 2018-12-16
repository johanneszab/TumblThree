using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

using TumblThree.Applications.DataModels.TumblrApiJson;
using TumblThree.Domain;

namespace TumblThree.Applications.Parser
{
    public class TumblrApiJsonToJsonParser<T> : ITumblrToTextParser<T> where T : Post
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
            postCopy.Tumblelog = new TumbleLog2();
            postCopy.RebloggedFromAvatarUrl128 = null;
            postCopy.RebloggedFromAvatarUrl16 = null;
            postCopy.RebloggedFromAvatarUrl24 = null;
            postCopy.RebloggedFromAvatarUrl30 = null;
            postCopy.RebloggedFromAvatarUrl40 = null;
            postCopy.RebloggedFromAvatarUrl48 = null;
            postCopy.RebloggedFromAvatarUrl512 = null;
            postCopy.RebloggedFromAvatarUrl64 = null;
            postCopy.RebloggedFromAvatarUrl96 = null;

            postCopy.RebloggedRootAvatarUrl128 = null;
            postCopy.RebloggedRootAvatarUrl16 = null;
            postCopy.RebloggedRootAvatarUrl24 = null;
            postCopy.RebloggedRootAvatarUrl30 = null;
            postCopy.RebloggedRootAvatarUrl40 = null;
            postCopy.RebloggedRootAvatarUrl48 = null;
            postCopy.RebloggedRootAvatarUrl512 = null;
            postCopy.RebloggedRootAvatarUrl64 = null;
            postCopy.RebloggedRootAvatarUrl96 = null;

            var serializer = new DataContractJsonSerializer(postCopy.GetType());

            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, postCopy);
                return JsonFormatter.FormatOutput(Encoding.UTF8.GetString(ms.ToArray()));
            }
        }
    }
}
