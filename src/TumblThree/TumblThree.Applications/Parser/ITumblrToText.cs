namespace TumblThree.Applications.Parser
{
    public interface ITumblrToTextParser<T>
    {
        string ParseAnswer(T post);
        string ParseAudioMeta(T post);
        string ParseConversation(T post);
        string ParseLink(T post);
        string ParsePhotoMeta(T post);
        string ParseQuote(T post);
        string ParseText(T post);
        string ParseVideoMeta(T post);
    }
}