using System.Xml.Linq;

namespace TumblThree.Applications.Parser
{
    public interface ITumblrXmlToTextParser
    {
        string ParseAnswer(XElement post);
        string ParseAudioMeta(XElement post);
        string ParseConversation(XElement post);
        string ParseLink(XElement post);
        string ParsePhotoMeta(XElement post);
        string ParseQuote(XElement post);
        string ParseText(XElement post);
        string ParseVideoMeta(XElement post);
    }
}