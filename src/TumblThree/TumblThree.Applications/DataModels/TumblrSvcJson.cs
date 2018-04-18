using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Applications.DataModels.TumblrSvcJson
{
    [DataContract]
    public class TumblrJson
    {
        [DataMember(EmitDefaultValue = false)]
        public Meta meta { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public Response response { get; set; }
    }

    [DataContract]
    public class Response
    {
        [DataMember(EmitDefaultValue = false)]
        public int seconds_since_last_activity { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public List<Post> posts { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public TrackingHtml tracking_html { get; set; }
    }

    [DataContract]
    public class Meta
    {
        [DataMember(EmitDefaultValue = false)]
        public int status { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string msg { get; set; }
    }

    [DataContract]
    public class Theme
    {
        [DataMember(EmitDefaultValue = false)]
        public int header_full_width { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int header_full_height { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int header_focus_width { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int header_focus_height { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string avatar_shape { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string background_color { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string body_font { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string header_bounds { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string header_image { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string header_image_focused { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string header_image_scaled { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool header_stretch { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string link_color { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool show_avatar { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool show_description { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool show_header_image { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool show_title { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string title_color { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string title_font { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string title_font_weight { get; set; }
    }

    [DataContract]
    public class Blog
    {
        [DataMember(EmitDefaultValue = false)]
        public string name { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string title { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string description { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int updated { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string uuid { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string key { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public Theme theme { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool can_message { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool share_likes { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool share_following { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool is_nsfw { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool is_adult { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool can_be_followed { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string placement_id { get; set; }
    }

    [DataContract]
    public class NsfwSurvey
    {
        [DataMember(EmitDefaultValue = false)]
        public string type { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string href { get; set; }
    }

    [DataContract]
    public class Links
    {
        [DataMember(EmitDefaultValue = false)]
        public NsfwSurvey nsfw_survey { get; set; }
    }

    [DataContract]
    public class Reblog
    {
        [DataMember(EmitDefaultValue = false)]
        public string comment { get; set; }
    }

    [DataContract]
    public class Theme2
    {
        [DataMember(EmitDefaultValue = false)]
        public string avatar_shape { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string background_color { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string body_font { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string header_bounds { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string header_image { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string header_image_focused { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string header_image_scaled { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool header_stretch { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string link_color { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool show_avatar { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool show_description { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool show_header_image { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool show_title { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string title_color { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string title_font { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string title_font_weight { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int? header_full_width { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int? header_full_height { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int? header_focus_width { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int? header_focus_height { get; set; }
    }

    [DataContract]
    public class Blog2
    {
        [DataMember(EmitDefaultValue = false)]
        public string name { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool active { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public List<Theme2> theme { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool share_likes { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool share_following { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool can_be_followed { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string uuid { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool is_adult { get; set; }
    }

    [DataContract]
    public class Post2
    {
        [DataMember(EmitDefaultValue = false)]
        public string id { get; set; }
    }

    [DataContract]
    public class Trail
    {
        [DataMember(EmitDefaultValue = false)]
        public Blog2 blog { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public Post2 post { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string content_raw { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string content { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool is_current_item { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool? is_root_item { get; set; }
    }

    [DataContract]
    public class LinkImageDimensions
    {
        [DataMember(EmitDefaultValue = false)]
        public int width { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int height { get; set; }
    }

    [DataContract]
    public class OriginalSize
    {
        [DataMember(EmitDefaultValue = false)]
        public string url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int width { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int height { get; set; }
    }

    [DataContract]
    public class AltSize
    {
        [DataMember(EmitDefaultValue = false)]
        public string url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int width { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int height { get; set; }
    }

    [DataContract]
    public class Exif
    {
        [DataMember(EmitDefaultValue = false)]
        public string Camera { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string ISO { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string Aperture { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string Exposure { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string FocalLength { get; set; }
    }

    [DataContract]
    public class Photo
    {
        [DataMember(EmitDefaultValue = false)]
        public string caption { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public OriginalSize original_size { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public List<AltSize> alt_sizes { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public Exif exif { get; set; }
    }

    [DataContract]
    public class SharePopoverData
    {
        [DataMember(EmitDefaultValue = false)]
        public string tumblelog_name { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string embed_key { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string embed_did { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string post_id { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string root_id { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string post_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string post_tiny_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int is_private { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool has_user { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool has_facebook { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string twitter_username { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string permalink_label { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool show_reporting_links { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string abuse_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool show_pinterest { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public object pinterest_share_window { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool show_reddit { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool show_flagging { get; set; }
    }

    [DataContract]
    public class Notes
    {
        [DataMember(EmitDefaultValue = false)]
        public int count { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string less { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string more { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string current { get; set; }
    }

    [DataContract]
    public class Dialogue
    {
        [DataMember(EmitDefaultValue = false)]
        public string name { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string label { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string phrase { get; set; }
    }

    [DataContract]
    public class PinterestShareWindowClass
    {
        [DataMember(EmitDefaultValue = false)]
        public string url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string name { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string dimensions { get; set; }
    }

    [DataContract]
    public class PhotosetPhoto
    {
        [DataMember(EmitDefaultValue = false)]
        public int width { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int height { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string low_res { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string high_res { get; set; }
    }

    [DataContract]
    public class Post : ICloneable
    {
        [DataMember(EmitDefaultValue = false)]
        public string type { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool is_nsfw_based_on_score { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public List<string> supply_logging { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public Blog blog { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool is_nsfw { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public double nsfw_score { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public Links _links { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string id { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string post_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string slug { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string date { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int timestamp { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string state { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string format { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string reblog_key { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public List<string> tags { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string short_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string summary { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool is_blocks_post_format { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string recommended_source { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string recommended_color { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool followed { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool liked { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string source_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string source_title { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string caption { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public Reblog reblog { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public List<Trail> trail { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string video_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool html5_capable { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public Video video { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string thumbnail_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int thumbnail_width { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int thumbnail_height { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public float duration { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public object player { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string audio_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string audio_source_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string audio_type { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string video_type { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string link_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string image_permalink { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public List<Photo> photos { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_id { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_name { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_title { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_uuid { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool reblogged_from_can_message { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool reblogged_from_following { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_id { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_name { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_title { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string reblogged_root_uuid { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool reblogged_root_can_message { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool reblogged_root_following { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool can_like { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool can_reblog { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool can_send_in_message { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool can_reply { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool display_avatar { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string tumblelog_key { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string tumblelog_uuid { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string root_id { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public SharePopoverData share_popover_data { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string posted_on_tooltip { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string tag_layout_class { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool has_custom_source_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string tumblelog { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string reblogged_from_tumblr_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public Notes notes { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool reblogged_from_followed { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string post_html { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string asking_name { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string asking_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string question { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string answer { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string photoset_layout { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public List<PhotosetPhoto> photoset_photos { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int? year { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string track { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool? is_external { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string title { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string body { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string text { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string source { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string artist { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string track_name { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string album_art { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string embed { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int plays { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string album { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public List<Dialogue> dialogue { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool? is_anonymous { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool? is_submission { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool should_bypass_tagfiltering { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool should_bypass_safemode { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool can_modify_safe_mode { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public object survey { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string link_image { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public LinkImageDimensions link_image_dimensions { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public object link_author { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string excerpt { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string publisher { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string description { get; set; }


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
            type = string.Empty;
            is_nsfw_based_on_score = false;
            supply_logging = new List<string>();
            blog = new Blog();
            is_nsfw = false;
            nsfw_score = 0.0;
            _links = new Links();
            id = string.Empty;
            post_url = string.Empty;
            slug = string.Empty;
            date = string.Empty;
            timestamp = 0;
            state = string.Empty;
            format = string.Empty;
            reblog_key = string.Empty;
            tags = new List<string>();
            short_url = string.Empty;
            summary = string.Empty;
            recommended_source = string.Empty;
            recommended_color = string.Empty;
            followed = false;
            liked = false;
            source_url = string.Empty;
            source_title = string.Empty;
            caption = string.Empty;
            reblog = new Reblog();
            trail = new List<Trail>();
            video_url = string.Empty;
            html5_capable = false;
            thumbnail_url = string.Empty;
            thumbnail_width = 0;
            thumbnail_height = 0;
            duration = 0;
            player = new object();
            audio_url = string.Empty;
            audio_source_url = string.Empty;
            audio_type = string.Empty;
            video_type = string.Empty;
            link_url = string.Empty;
            image_permalink = string.Empty;
            photos = new List<Photo>();
            reblogged_from_id = string.Empty;
            reblogged_from_url = string.Empty;
            reblogged_from_name = string.Empty;
            reblogged_from_title = string.Empty;
            reblogged_from_uuid = string.Empty;
            reblogged_from_can_message = false;
            reblogged_from_following = false;
            reblogged_root_id = string.Empty;
            reblogged_root_url = string.Empty;
            reblogged_root_name = string.Empty;
            reblogged_root_title = string.Empty;
            reblogged_root_uuid = string.Empty;
            reblogged_root_can_message = false;
            reblogged_root_following = false;
            can_like = false;
            can_reblog = false;
            can_send_in_message = false;
            can_reply = false;
            display_avatar = false;
            tumblelog_key = string.Empty;
            tumblelog_uuid = string.Empty;
            root_id = string.Empty;
            share_popover_data = new SharePopoverData();
            posted_on_tooltip = string.Empty;
            tag_layout_class = string.Empty;
            has_custom_source_url = false;
            tumblelog = string.Empty;
            reblogged_from_tumblr_url = string.Empty;
            notes = new Notes();
            reblogged_from_followed = false;
            post_html = string.Empty;
            asking_name = string.Empty;
            asking_url = string.Empty;
            question = string.Empty;
            answer = string.Empty;
            photoset_layout = string.Empty;
            photoset_photos = new List<PhotosetPhoto>();
            year = 0;
            track = string.Empty;
            is_external = false;
            title = string.Empty;
            body = string.Empty;
            text = string.Empty;
            source = string.Empty;
            artist = string.Empty;
            track_name = string.Empty;
            album_art = string.Empty;
            embed = string.Empty;
            plays = 0;
            album = string.Empty;
            dialogue = new List<Dialogue>();
            is_anonymous = false;
            is_submission = false;
            should_bypass_safemode = false;
            can_modify_safe_mode = false;
            survey = new object();
            url = string.Empty;
            link_image = string.Empty;
            link_image_dimensions = new LinkImageDimensions();
            link_author = string.Empty;
            excerpt = string.Empty;
            publisher = string.Empty;
            description = string.Empty;
        }
    }

    [DataContract]
    public class PixelbugUrl
    {
        [DataMember(EmitDefaultValue = false)]
        public string type { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string script { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string img { get; set; }
    }

    [DataContract]
    public class PixelbugPost
    {
        [DataMember(EmitDefaultValue = false)]
        public string type { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string script { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string img { get; set; }
    }

    [DataContract]
    public class Player
    {
        [DataMember(EmitDefaultValue = false)]
        public int width { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string embed_code { get; set; }
    }

    [DataContract]
    public class Youtube
    {
        [DataMember(EmitDefaultValue = false)]
        public string video_id { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int width { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int height { get; set; }
    }

    [DataContract]
    public class Video
    {
        [DataMember(EmitDefaultValue = false)]
        public Youtube youtube { get; set; }
    }

    [DataContract]
    public class TrackingHtml
    {
        [DataMember(EmitDefaultValue = false)]
        public PixelbugUrl pixelbug_url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public PixelbugPost pixelbug_post { get; set; }
    }
}
