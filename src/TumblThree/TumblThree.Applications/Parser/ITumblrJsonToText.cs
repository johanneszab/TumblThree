using TumblThree.Applications.DataModels.TumblrSvcJson;

namespace TumblThree.Applications.Parser
{
    public interface ITumblrJsonToTextParser
    {
        string ParseAnswer(Post post);
        string ParseAudioMeta(Post post);
        string ParseConversation(Post post);
        string ParseLink(Post post);
        string ParsePhotoMeta(Post post);
        string ParseQuote(Post post);
        string ParseText(Post post);
        string ParseVideoMeta(Post post);
    }
}