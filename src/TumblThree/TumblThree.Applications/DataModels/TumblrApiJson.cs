using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Applications.DataModels.TumblrApiJson
{
    [DataContract]
    public class TumblrApiJson
    {
        [DataMember]
        public Tumblelog tumblelog { get; set; }

        [DataMember(Name = "posts-start")]
        public int posts_start { get; set; }

        [DataMember(Name = "posts-total")]
        public int posts_total { get; set; }

        [DataMember(Name = "posts-type")]
        public bool posts_type { get; set; }

        [DataMember]
        public List<Post> posts { get; set; }
    }

    [DataContract]
    public class Tumblelog
    {
        [DataMember]
        public string title { get; set; }

        [DataMember]
        public string description { get; set; }

        [DataMember]
        public string name { get; set; }

        [DataMember]
        public string timezone { get; set; }

        [DataMember]
        public bool cname { get; set; }

        [DataMember]
        public List<object> feeds { get; set; }
    }

    [DataContract]
    public class Tumblelog2
    {
        [DataMember]
        public string title { get; set; }

        [DataMember]
        public string name { get; set; }

        [DataMember]
        public bool cname { get; set; }

        [DataMember]
        public string url { get; set; }

        [DataMember]
        public string timezone { get; set; }

        [DataMember]
        public string avatar_url_16 { get; set; }

        [DataMember]
        public string avatar_url_24 { get; set; }

        [DataMember]
        public string avatar_url_30 { get; set; }

        [DataMember]
        public string avatar_url_40 { get; set; }

        [DataMember]
        public string avatar_url_48 { get; set; }

        [DataMember]
        public string avatar_url_64 { get; set; }

        [DataMember]
        public string avatar_url_96 { get; set; }

        [DataMember]
        public string avatar_url_128 { get; set; }

        [DataMember]
        public string avatar_url_512 { get; set; }
    }

    [DataContract]
    public class Conversation
    {
        [DataMember]
        public string name { get; set; }

        [DataMember]
        public string label { get; set; }

        [DataMember]
        public string phrase { get; set; }
    }

    [DataContract]
    public class Photo
    {
        [DataMember]
        public string offset { get; set; }
        [DataMember]
        public string caption { get; set; }
        [DataMember]
        public int width { get; set; }
        [DataMember]
        public int height { get; set; }

        [DataMember(Name = "photo-url-1280")]
        public string photo_url_1280 { get; set; }

        [DataMember(Name = "photo-url-500")]
        public string photo_url_500 { get; set; }

        [DataMember(Name = "photo-url-400")]
        public string photo_url_400 { get; set; }

        [DataMember(Name = "photo-url-250")]
        public string photo_url_250 { get; set; }

        [DataMember(Name = "photo-url-100")]
        public string photo_url_100 { get; set; }

        [DataMember(Name = "photo-url-75")]
        public string photo_url_75 { get; set; }
    }

    [DataContract]
    public class Post : ICloneable
    {
        [DataMember]
        public string id { get; set; }

        [DataMember]
        public string url { get; set; }

        [DataMember(Name = "url-with-slug")]
        public string url_with_slug { get; set; }

        [DataMember]
        public string type { get; set; }

        [DataMember(Name = "date-gmt")]
        public string date_gmt { get; set; }

        [DataMember]
        public string date { get; set; }

        [DataMember]
        public int bookmarklet { get; set; }

        [DataMember]
        public int mobile { get; set; }

        [DataMember(Name = "feed-item")]
        public string feed_item { get; set; }

        [DataMember(Name = "from-feed-id")]
        public int from_feed_id { get; set; }

        [DataMember(Name = "unix-timestamp")]
        public int unix_timestamp { get; set; }

        [DataMember]
        public string format { get; set; }

        [DataMember(Name = "reblog-key")]
        public string reblog_key { get; set; }
        [DataMember]
        public string slug { get; set; }

        [DataMember(Name = "is-submission")]
        public bool is_submission { get; set; }

        [DataMember(Name = "like-button")]
        public string like_button { get; set; }

        [DataMember(Name = "reblog-button")]
        public string reblog_button { get; set; }

        [DataMember(Name = "note-count")]
        public string note_count { get; set; }

        [DataMember(Name = "reblogged-from-url")]
        public string reblogged_from_url { get; set; }

        [DataMember(Name = "reblogged-from-name")]
        public string reblogged_from_name { get; set; }

        [DataMember(Name = "reblogged-from-title")]
        public string reblogged_from_title { get; set; }

        [DataMember]
        public string reblogged_from_avatar_url_16 { get; set; }

        [DataMember]
        public string reblogged_from_avatar_url_24 { get; set; }

        [DataMember]
        public string reblogged_from_avatar_url_30 { get; set; }

        [DataMember]
        public string reblogged_from_avatar_url_40 { get; set; }

        [DataMember]
        public string reblogged_from_avatar_url_48 { get; set; }

        [DataMember]
        public string reblogged_from_avatar_url_64 { get; set; }

        [DataMember]
        public string reblogged_from_avatar_url_96 { get; set; }

        [DataMember]
        public string reblogged_from_avatar_url_128 { get; set; }

        [DataMember]
        public string reblogged_from_avatar_url_512 { get; set; }

        [DataMember(Name = "reblogged-root-url")]
        public string reblogged_root_url { get; set; }

        [DataMember(Name = "reblogged-root-name")]
        public string reblogged_root_name { get; set; }

        [DataMember(Name = "reblogged-root-title")]
        public string reblogged_root_title { get; set; }

        [DataMember]
        public string reblogged_root_avatar_url_16 { get; set; }

        [DataMember]
        public string reblogged_root_avatar_url_24 { get; set; }

        [DataMember]
        public string reblogged_root_avatar_url_30 { get; set; }

        [DataMember]
        public string reblogged_root_avatar_url_40 { get; set; }

        [DataMember]
        public string reblogged_root_avatar_url_48 { get; set; }

        [DataMember]
        public string reblogged_root_avatar_url_64 { get; set; }

        [DataMember]
        public string reblogged_root_avatar_url_96 { get; set; }

        [DataMember]
        public string reblogged_root_avatar_url_128 { get; set; }

        [DataMember]
        public string reblogged_root_avatar_url_512 { get; set; }

        [DataMember]
        public Tumblelog2 tumblelog { get; set; }

        [DataMember(Name = "quote-text")]
        public string quote_text { get; set; }

        [DataMember(Name = "quote-source")]
        public string quote_source { get; set; }

        [DataMember]
        public List<string> tags { get; set; }

        [DataMember(Name = "photo-caption")]
        public string photo_caption { get; set; }

        [DataMember]
        public int? width { get; set; }

        [DataMember]
        public int? height { get; set; }

        [DataMember(Name = "photo-url-1280")]
        public string photo_url_1280 { get; set; }

        [DataMember(Name = "photo-url-500")]
        public string photo_url_500 { get; set; }

        [DataMember(Name = "photo-url-400")]
        public string photo_url_400 { get; set; }

        [DataMember(Name = "photo-url-250")]
        public string photo_url_250 { get; set; }

        [DataMember(Name = "photo-url-100")]
        public string photo_url_100 { get; set; }

        [DataMember(Name = "photo-url-75")]
        public string photo_url_75 { get; set; }

        [DataMember]
        public List<Photo> photos { get; set; }

        [DataMember(Name = "photo-link-url")]
        public string photo_link_url { get; set; }

        [DataMember(Name = "id3-artist")]
        public string id3_artist { get; set; }

        [DataMember(Name = "id3-album")]
        public string id3_album { get; set; }

        [DataMember(Name = "id3-year")]
        public string id3_year { get; set; }

        [DataMember(Name = "id3-track")]
        public string id3_track { get; set; }

        [DataMember(Name = "id3-title")]
        public string id3_title { get; set; }

        [DataMember(Name = "audio-caption")]
        public string audio_caption { get; set; }

        [DataMember(Name = "audio-player")]
        public string audio_player { get; set; }

        [DataMember(Name = "audio-embed")]
        public string audio_embed { get; set; }

        [DataMember(Name = "audio-plays")]
        public int? audio_plays { get; set; }

        [DataMember(Name = "regular-title")]        
        public string regular_title { get; set; }

        [DataMember(Name = "regular-body")]
        public string regular_body { get; set; }

        [DataMember(Name = "link-text")]
        public string link_text { get; set; }

        [DataMember(Name = "link-url")]
        public string link_url { get; set; }

        [DataMember(Name = "link-description")]
        public string link_description { get; set; }

        [DataMember(Name = "conversation-title")]
        public string conversation_title { get; set; }

        [DataMember(Name = "conversation-text")]
        public string conversation_text { get; set; }

        [DataMember(Name = "video-caption")]
        public string video_caption { get; set; }

        [DataMember(Name = "video-source")]
        public string video_source { get; set; }

        [DataMember(Name = "video-player")]
        public string video_player { get; set; }

        [DataMember(Name = "video-player-500")]
        public string video_player_500 { get; set; }

        [DataMember(Name = "video-player-250")]
        public string video_player_250 { get; set; }

        [DataMember]
        public List<Conversation> conversation { get; set; }

        [DataMember]
        public string submitter { get; set; }
        [DataMember]
        public string question { get; set; }
        [DataMember]
        public string answer { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        [OnDeserializing()]
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
