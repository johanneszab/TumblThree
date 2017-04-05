using System.Collections.Generic;
using System.Xml.Serialization;

namespace TumblThree.Applications.DataModels.Xml
{
    [XmlRoot(ElementName = "a")]
    public class A
    {
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "audio-caption")]
    public class Audiocaption
    {
        [XmlElement(ElementName = "p")]
        public P P { get; set; }
    }

    [XmlRoot(ElementName = "audio-embed")]
    public class Audioembed
    {
        [XmlElement(ElementName = "iframe")]
        public Iframe Iframe { get; set; }
    }

    [XmlRoot(ElementName = "audio-player")]
    public class Audioplayer
    {
        [XmlElement(ElementName = "embed")]
        public Embed Embed { get; set; }
    }

    [XmlRoot(ElementName = "conversation")]
    public class Conversation
    {
        [XmlElement(ElementName = "line")]
        public List<Line> Line { get; set; }
    }

    [XmlRoot(ElementName = "video-source")]
    public class Videosource
    {
        [XmlElement(ElementName = "content-type")]
        public string Contenttype { get; set; }

        [XmlElement(ElementName = "extension")]
        public string Extension { get; set; }

        [XmlElement(ElementName = "width")]
        public string Width { get; set; }

        [XmlElement(ElementName = "height")]
        public string Height { get; set; }

        [XmlElement(ElementName = "duration")]
        public string Duration { get; set; }

        [XmlElement(ElementName = "revision")]
        public string Revision { get; set; }
    }

    [XmlRoot(ElementName = "video")]
    public class Video
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "class")]
        public string Class { get; set; }

        [XmlAttribute(AttributeName = "width")]
        public string Width { get; set; }

        [XmlAttribute(AttributeName = "height")]
        public string Height { get; set; }

        [XmlAttribute(AttributeName = "poster")]
        public string Poster { get; set; }

        [XmlAttribute(AttributeName = "preload")]
        public string Preload { get; set; }

        [XmlAttribute(AttributeName = "data-crt-options")]
        public string Datacrtoptions { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "video-player")]
    public class Videoplayer
    {
        [XmlElement(ElementName = "video")]
        public Video Video { get; set; }

        [XmlAttribute(AttributeName = "max-width")]
        public string Maxwidth { get; set; }
    }

    [XmlRoot(ElementName = "embed")]
    public class Embed
    {
        [XmlAttribute(AttributeName = "height")]
        public string Height { get; set; }

        [XmlAttribute(AttributeName = "quality")]
        public string Quality { get; set; }

        [XmlAttribute(AttributeName = "src")]
        public string Src { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "width")]
        public string Width { get; set; }

        [XmlAttribute(AttributeName = "wmode")]
        public string Wmode { get; set; }
    }

    [XmlRoot(ElementName = "iframe")]
    public class Iframe
    {
        [XmlAttribute(AttributeName = "allowtransparency")]
        public string Allowtransparency { get; set; }

        [XmlAttribute(AttributeName = "class")]
        public string Class { get; set; }

        [XmlAttribute(AttributeName = "frameborder")]
        public string Frameborder { get; set; }

        [XmlAttribute(AttributeName = "height")]
        public string Height { get; set; }

        [XmlAttribute(AttributeName = "scrolling")]
        public string Scrolling { get; set; }

        [XmlAttribute(AttributeName = "src")]
        public string Src { get; set; }

        [XmlAttribute(AttributeName = "width")]
        public string Width { get; set; }
    }

    [XmlRoot(ElementName = "line")]
    public class Line
    {
        [XmlAttribute(AttributeName = "label")]
        public string Label { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "link-description")]
    public class Linkdescription
    {
        [XmlElement(ElementName = "p")]
        public string P { get; set; }
    }

    [XmlRoot(ElementName = "p")]
    public class P
    {
        [XmlElement(ElementName = "a")]
        public A A { get; set; }

        [XmlElement(ElementName = "strong")]
        public Strong Strong { get; set; }
    }

    [XmlRoot(ElementName = "photo-caption")]
    public class Photocaption
    {
        [XmlElement(ElementName = "p")]
        public P P { get; set; }
    }

    [XmlRoot(ElementName = "photo-url")]
    public class Photourl
    {
        [XmlAttribute(AttributeName = "max-width")]
        public string Maxwidth { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "post")]
    public class Post
    {
        [XmlElement(ElementName = "audio-caption")]
        public Audiocaption Audiocaption { get; set; }

        [XmlElement(ElementName = "audio-embed")]
        public Audioembed Audioembed { get; set; }

        [XmlElement(ElementName = "audio-player")]
        public Audioplayer Audioplayer { get; set; }

        [XmlAttribute(AttributeName = "audio-plays")]
        public string Audioplays { get; set; }

        [XmlElement(ElementName = "conversation")]
        public Conversation Conversation { get; set; }

        [XmlElement(ElementName = "conversation-text")]
        public string Conversationtext { get; set; }

        [XmlAttribute(AttributeName = "date")]
        public string Date { get; set; }

        [XmlAttribute(AttributeName = "date-gmt")]
        public string Dategmt { get; set; }

        [XmlAttribute(AttributeName = "format")]
        public string Format { get; set; }

        [XmlAttribute(AttributeName = "height")]
        public string Height { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlElement(ElementName = "id3-album")]
        public string Id3album { get; set; }

        [XmlElement(ElementName = "id3-artist")]
        public string Id3artist { get; set; }

        [XmlElement(ElementName = "id3-title")]
        public string Id3title { get; set; }

        [XmlElement(ElementName = "id3-track")]
        public string Id3track { get; set; }

        [XmlElement(ElementName = "id3-year")]
        public string Id3year { get; set; }

        [XmlElement(ElementName = "link-description")]
        public Linkdescription Linkdescription { get; set; }

        [XmlElement(ElementName = "link-text")]
        public string Linktext { get; set; }

        [XmlElement(ElementName = "link-url")]
        public string Linkurl { get; set; }

        [XmlElement(ElementName = "photo-caption")]
        public Photocaption Photocaption { get; set; }

        [XmlElement(ElementName = "photo-url")]
        public List<Photourl> Photourl { get; set; }

        [XmlElement(ElementName = "quote-source")]
        public Quotesource Quotesource { get; set; }

        [XmlElement(ElementName = "quote-text")]
        public string Quotetext { get; set; }

        [XmlAttribute(AttributeName = "reblog-key")]
        public string Reblogkey { get; set; }

        [XmlElement(ElementName = "regular-body")]
        public Regularbody Regularbody { get; set; }

        [XmlElement(ElementName = "regular-title")]
        public string Regulartitle { get; set; }

        [XmlAttribute(AttributeName = "slug")]
        public string Slug { get; set; }

        [XmlElement(ElementName = "tag")]
        public List<string> Tag { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "unix-timestamp")]
        public string Unixtimestamp { get; set; }

        [XmlAttribute(AttributeName = "url")]
        public string Url { get; set; }

        [XmlAttribute(AttributeName = "url-with-slug")]
        public string Urlwithslug { get; set; }

        [XmlAttribute(AttributeName = "width")]
        public string Width { get; set; }

        [XmlAttribute(AttributeName = "direct-video")]
        public string Directvideo { get; set; }

        [XmlElement(ElementName = "video-source")]
        public Videosource Videosource { get; set; }

        [XmlElement(ElementName = "video-player")]
        public List<Videoplayer> Videoplayer { get; set; }
    }

    [XmlRoot(ElementName = "posts")]
    public class Posts
    {
        [XmlElement(ElementName = "post")]
        public List<Post> Post { get; set; }

        [XmlAttribute(AttributeName = "start")]
        public string Start { get; set; }

        [XmlAttribute(AttributeName = "total")]
        public string Total { get; set; }
    }

    [XmlRoot(ElementName = "quote-source")]
    public class Quotesource
    {
        [XmlElement(ElementName = "a")]
        public A A { get; set; }
    }

    [XmlRoot(ElementName = "regular-body")]
    public class Regularbody
    {
        [XmlElement(ElementName = "blockquote")]
        public string Blockquote { get; set; }

        [XmlElement(ElementName = "p")]
        public List<P> P { get; set; }

        [XmlElement(ElementName = "ul")]
        public Ul Ul { get; set; }
    }

    [XmlRoot(ElementName = "strong")]
    public class Strong
    {
        [XmlElement(ElementName = "a")]
        public A A { get; set; }
    }

    [XmlRoot(ElementName = "tumblelog")]
    public class Tumblelog
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlText]
        public string Text { get; set; }

        [XmlAttribute(AttributeName = "timezone")]
        public string Timezone { get; set; }

        [XmlAttribute(AttributeName = "title")]
        public string Title { get; set; }
    }

    [XmlRoot(ElementName = "tumblr")]
    public class Tumblr
    {
        [XmlElement(ElementName = "posts")]
        public Posts Posts { get; set; }

        [XmlElement(ElementName = "tumblelog")]
        public Tumblelog Tumblelog { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }
    }

    [XmlRoot(ElementName = "ul")]
    public class Ul
    {
        [XmlElement(ElementName = "li")]
        public List<string> Li { get; set; }
    }
}
