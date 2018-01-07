using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Applications.DataModels.TumblrApiJson
{
    [DataContract]
    public class TumblrApiJson
    {
        [DataMember(EmitDefaultValue = false)]
        public Tumblelog tumblelog { get; set; }

        [DataMember(Name = "posts-start", EmitDefaultValue = false)]
        public int posts_start { get; set; }

        [DataMember(Name = "posts-total", EmitDefaultValue = false)]
        public int posts_total { get; set; }

        [DataMember(Name = "posts-type", EmitDefaultValue = false)]
        public bool posts_type { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<Post> posts { get; set; }
    }

    [DataContract]
    public class Tumblelog
    {
        [DataMember(EmitDefaultValue = false)]
        public string title { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string description { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string timezone { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public object cname { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<object> feeds { get; set; }
    }

    [DataContract]
    public class Tumblelog2
    {
        [DataMember(EmitDefaultValue = false)]
        public string title { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public object cname { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string url { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string timezone { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string avatar_url_16 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string avatar_url_24 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string avatar_url_30 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string avatar_url_40 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string avatar_url_48 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string avatar_url_64 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string avatar_url_96 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string avatar_url_128 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string avatar_url_512 { get; set; }
    }

    [DataContract]
    public class Conversation
    {
        [DataMember(EmitDefaultValue = false)]
        public string name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string label { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string phrase { get; set; }
    }

    [DataContract]
    public class Photo
    {
        [DataMember(EmitDefaultValue = false)]
        public string offset { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string caption { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int width { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int height { get; set; }

        [DataMember(Name = "photo-url-1280", EmitDefaultValue = false)]
        public string photo_url_1280 { get; set; }

        [DataMember(Name = "photo-url-500", EmitDefaultValue = false)]
        public string photo_url_500 { get; set; }

        [DataMember(Name = "photo-url-400", EmitDefaultValue = false)]
        public string photo_url_400 { get; set; }

        [DataMember(Name = "photo-url-250", EmitDefaultValue = false)]
        public string photo_url_250 { get; set; }

        [DataMember(Name = "photo-url-100", EmitDefaultValue = false)]
        public string photo_url_100 { get; set; }

        [DataMember(Name = "photo-url-75", EmitDefaultValue = false)]
        public string photo_url_75 { get; set; }
    }

    [DataContract]
    public class Post : ICloneable
    {
        [DataMember(EmitDefaultValue = false)]
        public string id { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string url { get; set; }

        [DataMember(Name = "url-with-slug", EmitDefaultValue = false)]
        public string url_with_slug { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string type { get; set; }

        [DataMember(Name = "date-gmt", EmitDefaultValue = false)]
        public string date_gmt { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string date { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? bookmarklet { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? mobile { get; set; }

        [DataMember(Name = "feed-item", EmitDefaultValue = false)]
        public string feed_item { get; set; }

        [DataMember(Name = "from-feed-id", EmitDefaultValue = false)]
        public int from_feed_id { get; set; }

        [DataMember(Name = "unix-timestamp", EmitDefaultValue = false)]
        public int unix_timestamp { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string format { get; set; }

        [DataMember(Name = "reblog-key", EmitDefaultValue = false)]
        public string reblog_key { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string slug { get; set; }

        [DataMember(Name = "is-submission", EmitDefaultValue = false)]
        public bool is_submission { get; set; }

        [DataMember(Name = "like-button", EmitDefaultValue = false)]
        public string like_button { get; set; }

        [DataMember(Name = "reblog-button", EmitDefaultValue = false)]
        public string reblog_button { get; set; }

        [DataMember(Name = "note-count", EmitDefaultValue = false)]
        public string note_count { get; set; }

        [DataMember(Name = "reblogged-from-url", EmitDefaultValue = false)]
        public string reblogged_from_url { get; set; }

        [DataMember(Name = "reblogged-from-name", EmitDefaultValue = false)]
        public string reblogged_from_name { get; set; }

        [DataMember(Name = "reblogged-from-title", EmitDefaultValue = false)]
        public string reblogged_from_title { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_avatar_url_16 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_avatar_url_24 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_avatar_url_30 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_avatar_url_40 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_avatar_url_48 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_avatar_url_64 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_avatar_url_96 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_avatar_url_128 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_avatar_url_512 { get; set; }

        [DataMember(Name = "reblogged-root-url", EmitDefaultValue = false)]
        public string reblogged_root_url { get; set; }

        [DataMember(Name = "reblogged-root-name", EmitDefaultValue = false)]
        public string reblogged_root_name { get; set; }

        [DataMember(Name = "reblogged-root-title", EmitDefaultValue = false)]
        public string reblogged_root_title { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_avatar_url_16 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_avatar_url_24 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_avatar_url_30 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_avatar_url_40 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_avatar_url_48 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_avatar_url_64 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_avatar_url_96 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_avatar_url_128 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_avatar_url_512 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Tumblelog2 tumblelog { get; set; }

        [DataMember(Name = "quote-text", EmitDefaultValue = false)]
        public string quote_text { get; set; }

        [DataMember(Name = "quote-source", EmitDefaultValue = false)]
        public string quote_source { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<string> tags { get; set; }

        [DataMember(Name = "photo-caption", EmitDefaultValue = false)]
        public string photo_caption { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? width { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? height { get; set; }

        [DataMember(Name = "photo-url-1280", EmitDefaultValue = false)]
        public string photo_url_1280 { get; set; }

        [DataMember(Name = "photo-url-500", EmitDefaultValue = false)]
        public string photo_url_500 { get; set; }

        [DataMember(Name = "photo-url-400", EmitDefaultValue = false)]
        public string photo_url_400 { get; set; }

        [DataMember(Name = "photo-url-250", EmitDefaultValue = false)]
        public string photo_url_250 { get; set; }

        [DataMember(Name = "photo-url-100", EmitDefaultValue = false)]
        public string photo_url_100 { get; set; }

        [DataMember(Name = "photo-url-75", EmitDefaultValue = false)]
        public string photo_url_75 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<Photo> photos { get; set; }

        [DataMember(Name = "photo-link-url", EmitDefaultValue = false)]
        public string photo_link_url { get; set; }

        [DataMember(Name = "id3-artist", EmitDefaultValue = false)]
        public string id3_artist { get; set; }

        [DataMember(Name = "id3-album", EmitDefaultValue = false)]
        public string id3_album { get; set; }

        [DataMember(Name = "id3-year", EmitDefaultValue = false)]
        public string id3_year { get; set; }

        [DataMember(Name = "id3-track", EmitDefaultValue = false)]
        public string id3_track { get; set; }

        [DataMember(Name = "id3-title", EmitDefaultValue = false)]
        public string id3_title { get; set; }

        [DataMember(Name = "audio-caption", EmitDefaultValue = false)]
        public string audio_caption { get; set; }

        [DataMember(Name = "audio-player", EmitDefaultValue = false)]
        public string audio_player { get; set; }

        [DataMember(Name = "audio-embed", EmitDefaultValue = false)]
        public string audio_embed { get; set; }

        [DataMember(Name = "audio-plays", EmitDefaultValue = false)]
        public int? audio_plays { get; set; }

        [DataMember(Name = "regular-title", EmitDefaultValue = false)]
        public string regular_title { get; set; }

        [DataMember(Name = "regular-body", EmitDefaultValue = false)]
        public string regular_body { get; set; }

        [DataMember(Name = "link-text", EmitDefaultValue = false)]
        public string link_text { get; set; }

        [DataMember(Name = "link-url", EmitDefaultValue = false)]
        public string link_url { get; set; }

        [DataMember(Name = "link-description", EmitDefaultValue = false)]
        public string link_description { get; set; }

        [DataMember(Name = "conversation-title", EmitDefaultValue = false)]
        public string conversation_title { get; set; }

        [DataMember(Name = "conversation-text", EmitDefaultValue = false)]
        public string conversation_text { get; set; }

        [DataMember(Name = "video-caption", EmitDefaultValue = false)]
        public string video_caption { get; set; }

        [DataMember(Name = "video-source", EmitDefaultValue = false)]
        public string video_source { get; set; }

        [DataMember(Name = "video-player", EmitDefaultValue = false)]
        public string video_player { get; set; }

        [DataMember(Name = "video-player-500", EmitDefaultValue = false)]
        public string video_player_500 { get; set; }

        [DataMember(Name = "video-player-250", EmitDefaultValue = false)]
        public string video_player_250 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<Conversation> conversation { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string submitter { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string question { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string answer { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        [OnDeserializing]
        internal void OnDeserializingMethod(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            id = string.Empty;
            url = string.Empty;
            url_with_slug = string.Empty;
            type = string.Empty;
            date_gmt = string.Empty;
            date = string.Empty;
            bookmarklet = 0;
            mobile = 0;
            feed_item = string.Empty;
            from_feed_id = 0;
            unix_timestamp = 0;
            format = string.Empty;
            reblog_key = string.Empty;
            slug = string.Empty;
            is_submission = false;
            like_button = string.Empty;
            reblog_button = string.Empty;
            note_count = string.Empty;
            reblogged_from_url = string.Empty;
            reblogged_from_name = string.Empty;
            reblogged_from_title = string.Empty;
            reblogged_from_avatar_url_16 = string.Empty;
            reblogged_from_avatar_url_24 = string.Empty;
            reblogged_from_avatar_url_30 = string.Empty;
            reblogged_from_avatar_url_40 = string.Empty;
            reblogged_from_avatar_url_48 = string.Empty;
            reblogged_from_avatar_url_64 = string.Empty;
            reblogged_from_avatar_url_96 = string.Empty;
            reblogged_from_avatar_url_128 = string.Empty;
            reblogged_from_avatar_url_512 = string.Empty;
            reblogged_root_url = string.Empty;
            reblogged_root_name = string.Empty;
            reblogged_root_title = string.Empty;
            reblogged_root_avatar_url_16 = string.Empty;
            reblogged_root_avatar_url_24 = string.Empty;
            reblogged_root_avatar_url_30 = string.Empty;
            reblogged_root_avatar_url_40 = string.Empty;
            reblogged_root_avatar_url_48 = string.Empty;
            reblogged_root_avatar_url_64 = string.Empty;
            reblogged_root_avatar_url_96 = string.Empty;
            reblogged_root_avatar_url_128 = string.Empty;
            reblogged_root_avatar_url_512 = string.Empty;
            tumblelog = new Tumblelog2();
            quote_text = string.Empty;
            quote_source = string.Empty;
            tags = new List<string>();
            photo_caption = string.Empty;
            width = 0;
            height = 0;
            photo_url_1280 = string.Empty;
            photo_url_500 = string.Empty;
            photo_url_400 = string.Empty;
            photo_url_250 = string.Empty;
            photo_url_100 = string.Empty;
            photo_url_75 = string.Empty;
            photos = new List<Photo>();
            photo_link_url = string.Empty;
            id3_artist = string.Empty;
            id3_album = string.Empty;
            id3_year = string.Empty;
            id3_track = string.Empty;
            id3_title = string.Empty;
            audio_caption = string.Empty;
            audio_player = string.Empty;
            audio_embed = string.Empty;
            audio_plays = 0;
            regular_title = string.Empty;
            regular_body = string.Empty;
            link_text = string.Empty;
            link_url = string.Empty;
            link_description = string.Empty;
            conversation_title = string.Empty;
            conversation_text = string.Empty;
            video_caption = string.Empty;
            video_source = string.Empty;
            video_player = string.Empty;
            video_player_500 = string.Empty;
            video_player_250 = string.Empty;
            conversation = new List<Conversation>();
            submitter = string.Empty;
            question = string.Empty;
            answer = string.Empty;
        }
    }
}
