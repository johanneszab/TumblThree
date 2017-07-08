using System.Collections.Generic;

namespace TumblThree.Applications.DataModels
{
    public class TumblrJson
    {
        public Meta meta { get; set; }
        public Response response { get; set; }
    }

    public class Response
    {
        public int seconds_since_last_activity { get; set; }
        public List<Post> posts { get; set; }
        public TrackingHtml tracking_html { get; set; }
    }

    public class Meta
    {
        public int status { get; set; }
        public string msg { get; set; }
    }

    public class Theme
    {
        public string avatar_shape { get; set; }
        public string background_color { get; set; }
        public string body_font { get; set; }
        public string header_bounds { get; set; }
        public string header_image { get; set; }
        public string header_image_focused { get; set; }
        public string header_image_scaled { get; set; }
        public bool header_stretch { get; set; }
        public string link_color { get; set; }
        public bool show_avatar { get; set; }
        public bool show_description { get; set; }
        public bool show_header_image { get; set; }
        public bool show_title { get; set; }
        public string title_color { get; set; }
        public string title_font { get; set; }
        public string title_font_weight { get; set; }
    }

    public class Blog
    {
        public string name { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public int updated { get; set; }
        public string uuid { get; set; }
        public string key { get; set; }
        public Theme theme { get; set; }
        public bool can_message { get; set; }
        public bool share_likes { get; set; }
        public bool share_following { get; set; }
        public bool is_nsfw { get; set; }
        public bool is_adult { get; set; }
        public bool can_be_followed { get; set; }
        public string placement_id { get; set; }
    }

    public class NsfwSurvey
    {
        public string type { get; set; }
        public string href { get; set; }
    }

    public class Links
    {
        public NsfwSurvey nsfw_survey { get; set; }
    }

    public class Reblog
    {
        public string comment { get; set; }
    }

    public class Theme2
    {
        public string avatar_shape { get; set; }
        public string background_color { get; set; }
        public string body_font { get; set; }
        public string header_bounds { get; set; }
        public string header_image { get; set; }
        public string header_image_focused { get; set; }
        public string header_image_scaled { get; set; }
        public bool header_stretch { get; set; }
        public string link_color { get; set; }
        public bool show_avatar { get; set; }
        public bool show_description { get; set; }
        public bool show_header_image { get; set; }
        public bool show_title { get; set; }
        public string title_color { get; set; }
        public string title_font { get; set; }
        public string title_font_weight { get; set; }
        public int? header_full_width { get; set; }
        public int? header_full_height { get; set; }
        public int? header_focus_width { get; set; }
        public int? header_focus_height { get; set; }
    }

    public class Blog2
    {
        public string name { get; set; }
        public bool active { get; set; }
        public List<Theme2> theme { get; set; }
        public bool share_likes { get; set; }
        public bool share_following { get; set; }
        public bool can_be_followed { get; set; }
        public string uuid { get; set; }
        public bool is_adult { get; set; }
    }

    public class Post2
    {
        public string id { get; set; }
    }

    public class Trail
    {
        public Blog2 blog { get; set; }
        public Post2 post { get; set; }
        public string content_raw { get; set; }
        public string content { get; set; }
        public bool is_current_item { get; set; }
        public bool? is_root_item { get; set; }
    }

    public class OriginalSize
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class AltSize
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Exif
    {
        public string Camera { get; set; }
        public string ISO { get; set; }
        public string Aperture { get; set; }
        public string Exposure { get; set; }
        public string FocalLength { get; set; }
    }

    public class Photo
    {
        public string caption { get; set; }
        public OriginalSize original_size { get; set; }
        public List<AltSize> alt_sizes { get; set; }
        public Exif exif { get; set; }
    }

    public class SharePopoverData
    {
        public string tumblelog_name { get; set; }
        public string embed_key { get; set; }
        public string embed_did { get; set; }
        public string post_id { get; set; }
        public string root_id { get; set; }
        public string post_url { get; set; }
        public string post_tiny_url { get; set; }
        public int is_private { get; set; }
        public bool has_user { get; set; }
        public bool has_facebook { get; set; }
        public string twitter_username { get; set; }
        public string permalink_label { get; set; }
        public bool show_reporting_links { get; set; }
        public string abuse_url { get; set; }
        public bool show_pinterest { get; set; }
        public object pinterest_share_window { get; set; }
        public bool show_reddit { get; set; }
        public bool show_flagging { get; set; }
    }

    public class Notes
    {
        public int count { get; set; }
        public string less { get; set; }
        public string more { get; set; }
        public string current { get; set; }
    }

    public class Dialogue
    {
        public string name { get; set; }
        public string label { get; set; }
        public string phrase { get; set; }
    }

    public class PhotosetPhoto
    {
        public int width { get; set; }
        public int height { get; set; }
        public string low_res { get; set; }
        public string high_res { get; set; }
    }

    public class Post
    {
        public string type { get; set; }
        public bool is_nsfw_based_on_score { get; set; }
        public Blog blog { get; set; }
        public bool is_nsfw { get; set; }
        public double nsfw_score { get; set; }
        public Links _links { get; set; }
        public string id { get; set; }
        public string post_url { get; set; }
        public string slug { get; set; }
        public string date { get; set; }
        public int timestamp { get; set; }
        public string state { get; set; }
        public string format { get; set; }
        public string reblog_key { get; set; }
        public List<string> tags { get; set; }
        public string short_url { get; set; }
        public string summary { get; set; }
        public object recommended_source { get; set; }
        public object recommended_color { get; set; }
        public bool followed { get; set; }
        public bool liked { get; set; }
        public string source_url { get; set; }
        public string source_title { get; set; }
        public string caption { get; set; }
        public Reblog reblog { get; set; }
        public List<Trail> trail { get; set; }
        public string video_url { get; set; }
        public bool html5_capable { get; set; }
        public string thumbnail_url { get; set; }
        public int thumbnail_width { get; set; }
        public int thumbnail_height { get; set; }
        public float duration { get; set; }
        public object player { get; set; } // should be either List<Player> or Player instead of object
        public string audio_url { get; set; }
        public string audio_source_url { get; set; }
        public string audio_type { get; set; }
        public string video_type { get; set; }
        public string link_url { get; set; }
        public string image_permalink { get; set; }
        public List<Photo> photos { get; set; }
        public string reblogged_from_id { get; set; }
        public string reblogged_from_url { get; set; }
        public string reblogged_from_name { get; set; }
        public string reblogged_from_title { get; set; }
        public string reblogged_from_uuid { get; set; }
        public bool reblogged_from_can_message { get; set; }
        public bool reblogged_from_following { get; set; }
        public string reblogged_root_id { get; set; }
        public string reblogged_root_url { get; set; }
        public string reblogged_root_name { get; set; }
        public string reblogged_root_title { get; set; }
        public string reblogged_root_uuid { get; set; }
        public bool reblogged_root_can_message { get; set; }
        public bool reblogged_root_following { get; set; }
        public bool can_like { get; set; }
        public bool can_reblog { get; set; }
        public bool can_send_in_message { get; set; }
        public bool can_reply { get; set; }
        public bool display_avatar { get; set; }
        public string tumblelog_key { get; set; }
        public string tumblelog_uuid { get; set; }
        public object root_id { get; set; }
        public SharePopoverData share_popover_data { get; set; }
        public string posted_on_tooltip { get; set; }
        public string tag_layout_class { get; set; }
        public bool has_custom_source_url { get; set; }
        public string tumblelog { get; set; }
        public string reblogged_from_tumblr_url { get; set; }
        public Notes notes { get; set; }
        public bool reblogged_from_followed { get; set; }
        public string post_html { get; set; }
        public string asking_name { get; set; }
        public object asking_url { get; set; }
        public string question { get; set; }
        public string answer { get; set; }
        public string photoset_layout { get; set; }
        public List<PhotosetPhoto> photoset_photos { get; set; }
        public int? year { get; set; }
        public string track { get; set; }
        public bool? is_external { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public string text { get; set; }
        public string source { get; set; }
        public string artist { get; set; }
        public string track_name { get; set; }
        public string album_art { get; set; }
        public string embed { get; set; }
        public int plays { get; set; }
        public string album { get; set; }
        public List<Dialogue> dialogue { get; set; }
        public bool? is_anonymous { get; set; }
        public bool? is_submission { get; set; }
    }

    public class PixelbugUrl
    {
        public string type { get; set; }
        public string script { get; set; }
        public string img { get; set; }
    }

    public class PixelbugPost
    {
        public string type { get; set; }
        public string script { get; set; }
        public string img { get; set; }
    }

    public class Player
    {
        public int width { get; set; }
        public string embed_code { get; set; }
    }

    public class TrackingHtml
    {
        public PixelbugUrl pixelbug_url { get; set; }
        public PixelbugPost pixelbug_post { get; set; }
    }
}
