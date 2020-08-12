using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Applications.DataModels.TumblrSvcJson
{

    [DataContract]
    public class TumblrJson
    {
        [DataMember(Name = "meta", EmitDefaultValue = false)]
        public Meta Meta { get; set; }

        [DataMember(Name = "response", EmitDefaultValue = false)]
        public Response Response { get; set; }
    }

    [DataContract]
    public class Response
    {
        [DataMember(Name = "seconds_since_last_activity", EmitDefaultValue = false)]
        public int SecondsSinceLastActivity { get; set; }

        [DataMember(Name = "posts", EmitDefaultValue = false)]
        public List<Post> Posts { get; set; }

        [DataMember(Name = "tracking_html", EmitDefaultValue = false)]
        public TrackingHtml TrackingHtml { get; set; }
    }

    [DataContract]
    public class Meta
    {
        [DataMember(Name = "status", EmitDefaultValue = false)]
        public int Status { get; set; }

        [DataMember(Name = "msg", EmitDefaultValue = false)]
        public string Msg { get; set; }
    }

    [DataContract]
    public class Theme
    {
        [DataMember(Name = "header_full_width", EmitDefaultValue = false)]
        public int HeaderFullWidth { get; set; }

        [DataMember(Name = "header_full_height", EmitDefaultValue = false)]
        public int HeaderFullHeight { get; set; }

        [DataMember(Name = "header_focus_width", EmitDefaultValue = false)]
        public int HeaderFocusWidth { get; set; }

        [DataMember(Name = "header_focus_height", EmitDefaultValue = false)]
        public int HeaderFocusHeight { get; set; }

        [DataMember(Name = "avatar_shape", EmitDefaultValue = false)]
        public string AvatarShape { get; set; }

        [DataMember(Name = "background_color", EmitDefaultValue = false)]
        public string BackgroundColor { get; set; }

        [DataMember(Name = "body_font", EmitDefaultValue = false)]
        public string BodyFont { get; set; }

        [DataMember(Name = "header_bounds", EmitDefaultValue = false)]
        public string HeaderBounds { get; set; }

        [DataMember(Name = "header_image", EmitDefaultValue = false)]
        public string HeaderImage { get; set; }

        [DataMember(Name = "header_image_focused", EmitDefaultValue = false)]
        public string HeaderImageFocused { get; set; }

        [DataMember(Name = "header_image_scaled", EmitDefaultValue = false)]
        public string HeaderImageScaled { get; set; }

        [DataMember(Name = "header_stretch", EmitDefaultValue = false)]
        public bool HeaderStretch { get; set; }

        [DataMember(Name = "link_color", EmitDefaultValue = false)]
        public string LinkColor { get; set; }

        [DataMember(Name = "show_avatar", EmitDefaultValue = false)]
        public bool ShowAvatar { get; set; }

        [DataMember(Name = "show_description", EmitDefaultValue = false)]
        public bool ShowDescription { get; set; }

        [DataMember(Name = "show_header_image", EmitDefaultValue = false)]
        public bool ShowHeaderImage { get; set; }

        [DataMember(Name = "show_title", EmitDefaultValue = false)]
        public bool ShowTitle { get; set; }

        [DataMember(Name = "title_color", EmitDefaultValue = false)]
        public string TitleColor { get; set; }

        [DataMember(Name = "title_font", EmitDefaultValue = false)]
        public string TitleFont { get; set; }

        [DataMember(Name = "title_font_weight", EmitDefaultValue = false)]
        public string TitleFontWeight { get; set; }
    }

    [DataContract]
    public class Blog
    {
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Title { get; set; }

        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string Description { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "updated", EmitDefaultValue = false)]
        public int Updated { get; set; }

        [DataMember(Name = "uuid", EmitDefaultValue = false)]
        public string Uuid { get; set; }

        [DataMember(Name = "key", EmitDefaultValue = false)]
        public string Key { get; set; }

        [DataMember(Name = "theme", EmitDefaultValue = false)]
        public Theme Theme { get; set; }

        [DataMember(Name = "can_message", EmitDefaultValue = false)]
        public bool CanMessage { get; set; }

        [DataMember(Name = "share_likes", EmitDefaultValue = false)]
        public bool ShareLikes { get; set; }

        [DataMember(Name = "share_following", EmitDefaultValue = false)]
        public bool ShareFollowing { get; set; }

        [DataMember(Name = "is_nsfw", EmitDefaultValue = false)]
        public bool IsNsfw { get; set; }

        [DataMember(Name = "is_adult", EmitDefaultValue = false)]
        public bool IsAdult { get; set; }

        [DataMember(Name = "can_be_followed", EmitDefaultValue = false)]
        public bool CanBeFollowed { get; set; }

        [DataMember(Name = "placement_id", EmitDefaultValue = false)]
        public string PlacementId { get; set; }
    }

    [DataContract]
    public class NsfwSurvey
    {
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "href", EmitDefaultValue = false)]
        public string Href { get; set; }
    }

    [DataContract]
    public class Links
    {
        [DataMember(Name = "nsfw_survey", EmitDefaultValue = false)]
        public NsfwSurvey NsfwSurvey { get; set; }
    }

    [DataContract]
    public class Reblog
    {
        [DataMember(Name = "comment", EmitDefaultValue = false)]
        public string Comment { get; set; }
    }

    [DataContract]
    public class Theme2
    {
        [DataMember(Name = "avatar_shape", EmitDefaultValue = false)]
        public string AvatarShape { get; set; }

        [DataMember(Name = "background_color", EmitDefaultValue = false)]
        public string BackgroundColor { get; set; }

        [DataMember(Name = "body_font", EmitDefaultValue = false)]
        public string BodyFont { get; set; }

        [DataMember(Name = "header_bounds", EmitDefaultValue = false)]
        public string HeaderBounds { get; set; }

        [DataMember(Name = "header_image", EmitDefaultValue = false)]
        public string HeaderImage { get; set; }

        [DataMember(Name = "header_image_focused", EmitDefaultValue = false)]
        public string HeaderImageFocused { get; set; }

        [DataMember(Name = "header_image_scaled", EmitDefaultValue = false)]
        public string HeaderImageScaled { get; set; }

        [DataMember(Name = "header_stretch", EmitDefaultValue = false)]
        public bool HeaderStretch { get; set; }

        [DataMember(Name = "link_color", EmitDefaultValue = false)]
        public string LinkColor { get; set; }

        [DataMember(Name = "show_avatar", EmitDefaultValue = false)]
        public bool ShowAvatar { get; set; }

        [DataMember(Name = "show_description", EmitDefaultValue = false)]
        public bool ShowDescription { get; set; }

        [DataMember(Name = "show_header_image", EmitDefaultValue = false)]
        public bool ShowHeaderImage { get; set; }

        [DataMember(Name = "show_title", EmitDefaultValue = false)]
        public bool ShowTitle { get; set; }

        [DataMember(Name = "title_color", EmitDefaultValue = false)]
        public string TitleColor { get; set; }

        [DataMember(Name = "title_font", EmitDefaultValue = false)]
        public string TitleFont { get; set; }

        [DataMember(Name = "title_font_weight", EmitDefaultValue = false)]
        public string TitleFontWeight { get; set; }

        [DataMember(Name = "header_full_width", EmitDefaultValue = false)]
        public int? HeaderFullWidth { get; set; }

        [DataMember(Name = "header_full_height", EmitDefaultValue = false)]
        public int? HeaderFullHeight { get; set; }

        [DataMember(Name = "header_focus_width", EmitDefaultValue = false)]
        public int? HeaderFocusWidth { get; set; }

        [DataMember(Name = "header_focus_height", EmitDefaultValue = false)]
        public int? HeaderFocusHeight { get; set; }
    }

    [DataContract]
    public class Blog2
    {
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "routactiveeSet", EmitDefaultValue = false)]
        public bool Active { get; set; }

        [DataMember(Name = "theme", EmitDefaultValue = false)]
        public List<Theme2> Theme { get; set; }

        [DataMember(Name = "share_likes", EmitDefaultValue = false)]
        public bool ShareLikes { get; set; }

        [DataMember(Name = "share_following", EmitDefaultValue = false)]
        public bool ShareFollowing { get; set; }

        [DataMember(Name = "can_be_followed", EmitDefaultValue = false)]
        public bool CanBeFollowed { get; set; }

        [DataMember(Name = "uuid", EmitDefaultValue = false)]
        public string Uuid { get; set; }

        [DataMember(Name = "ris_adultouteSet", EmitDefaultValue = false)]
        public bool IsAdult { get; set; }
    }

    [DataContract]
    public class Post2
    {
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }
    }

    [DataContract]
    public class Trail
    {
        [DataMember(Name = "blog", EmitDefaultValue = false)]
        public Blog2 Blog { get; set; }

        [DataMember(Name = "post", EmitDefaultValue = false)]
        public Post2 Post { get; set; }

        [DataMember(Name = "content_raw", EmitDefaultValue = false)]
        public string ContentRaw { get; set; }

        [DataMember(Name = "content", EmitDefaultValue = false)]
        public string Content { get; set; }

        [DataMember(Name = "is_current_item", EmitDefaultValue = false)]
        public bool IsCurrentItem { get; set; }

        [DataMember(Name = "is_root_item", EmitDefaultValue = false)]
        public bool? IsRootItem { get; set; }
    }

    [DataContract]
    public class LinkImageDimensions
    {
        [DataMember(Name = "width", EmitDefaultValue = false)]
        public int Width { get; set; }

        [DataMember(Name = "height", EmitDefaultValue = false)]
        public int Height { get; set; }
    }

    [DataContract]
    public class OriginalSize
    {
        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "width", EmitDefaultValue = false)]
        public int Width { get; set; }

        [DataMember(Name = "height", EmitDefaultValue = false)]
        public int Height { get; set; }
    }

    [DataContract]
    public class AltSize
    {
        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "width", EmitDefaultValue = false)]
        public int Width { get; set; }

        [DataMember(Name = "height", EmitDefaultValue = false)]
        public int Height { get; set; }
    }

    [DataContract]
    public class Exif
    {
        [DataMember(Name = "Camera", EmitDefaultValue = false)]
        public string Camera { get; set; }

        [DataMember(Name = "ISO", EmitDefaultValue = false)]
        public string ISO { get; set; }

        [DataMember(Name = "Aperture", EmitDefaultValue = false)]
        public string Aperture { get; set; }

        [DataMember(Name = "Exposure", EmitDefaultValue = false)]
        public string Exposure { get; set; }

        [DataMember(Name = "FocalLength", EmitDefaultValue = false)]
        public string FocalLength { get; set; }
    }

    [DataContract]
    public class Photo
    {
        [DataMember(Name = "caption", EmitDefaultValue = false)]
        public string Caption { get; set; }

        [DataMember(Name = "original_size", EmitDefaultValue = false)]
        public OriginalSize OriginalSize { get; set; }

        [DataMember(Name = "alt_sizes", EmitDefaultValue = false)]
        public List<AltSize> AltSizes { get; set; }

        [DataMember(Name = "exif", EmitDefaultValue = false)]
        public Exif Exif { get; set; }
    }

    [DataContract]
    public class SharePopoverData
    {
        [DataMember(Name = "tumblelog_name", EmitDefaultValue = false)]
        public string TumblelogName { get; set; }

        [DataMember(Name = "embed_key", EmitDefaultValue = false)]
        public string EmbedKey { get; set; }

        [DataMember(Name = "embed_did", EmitDefaultValue = false)]
        public string EmbedDid { get; set; }

        [DataMember(Name = "post_id", EmitDefaultValue = false)]
        public string PostId { get; set; }

        [DataMember(Name = "root_id", EmitDefaultValue = false)]
        public string RootId { get; set; }

        [DataMember(Name = "post_url", EmitDefaultValue = false)]
        public string PostUrl { get; set; }

        [DataMember(Name = "post_tiny_url", EmitDefaultValue = false)]
        public string PostTinyUrl { get; set; }

        [DataMember(Name = "is_private", EmitDefaultValue = false)]
        public int IsPrivate { get; set; }

        [DataMember(Name = "has_user", EmitDefaultValue = false)]
        public bool HasUser { get; set; }

        [DataMember(Name = "has_facebook", EmitDefaultValue = false)]
        public bool HasFacebook { get; set; }

        [DataMember(Name = "twitter_username", EmitDefaultValue = false)]
        public string TwitterUsername { get; set; }

        [DataMember(Name = "permalink_label", EmitDefaultValue = false)]
        public string PermalinkLabel { get; set; }

        [DataMember(Name = "show_reporting_links", EmitDefaultValue = false)]
        public bool ShowReportingLinks { get; set; }

        [DataMember(Name = "abuse_url", EmitDefaultValue = false)]
        public string AbuseUrl { get; set; }

        [DataMember(Name = "show_pinterest", EmitDefaultValue = false)]
        public bool ShowPinterest { get; set; }

        [DataMember(Name = "pinterest_share_window", EmitDefaultValue = false)]
        public object PinterestShareWindow { get; set; }

        [DataMember(Name = "show_reddit", EmitDefaultValue = false)]
        public bool ShowReddit { get; set; }

        [DataMember(Name = "show_flagging", EmitDefaultValue = false)]
        public bool ShowFlagging { get; set; }
    }

    [DataContract]
    public class Notes
    {
        [DataMember(Name = "count", EmitDefaultValue = false)]
        public int Count { get; set; }

        [DataMember(Name = "less", EmitDefaultValue = false)]
        public string Less { get; set; }

        [DataMember(Name = "more", EmitDefaultValue = false)]
        public string More { get; set; }

        [DataMember(Name = "current", EmitDefaultValue = false)]
        public string Current { get; set; }
    }

    [DataContract]
    public class Dialogue
    {
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "label", EmitDefaultValue = false)]
        public string Label { get; set; }

        [DataMember(Name = "phrase", EmitDefaultValue = false)]
        public string Phrase { get; set; }
    }

    [DataContract]
    public class PinterestShareWindowClass
    {
        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "dimensions", EmitDefaultValue = false)]
        public string Dimensions { get; set; }
    }

    [DataContract]
    public class PhotosetPhoto
    {
        [DataMember(Name = "width", EmitDefaultValue = false)]
        public int Width { get; set; }

        [DataMember(Name = "height", EmitDefaultValue = false)]
        public int Height { get; set; }

        [DataMember(Name = "low_res", EmitDefaultValue = false)]
        public string LowRes { get; set; }

        [DataMember(Name = "high_res", EmitDefaultValue = false)]
        public string HighRes { get; set; }
    }

    [DataContract]
    public class Post : ICloneable
    {
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "is_nsfw_based_on_score", EmitDefaultValue = false)]
        public bool IsNsfwBasedOnScore { get; set; }

        [DataMember(Name = "supply_logging", EmitDefaultValue = false)]
        public List<string> SupplyLogging { get; set; }

        [DataMember(Name = "blog", EmitDefaultValue = false)]
        public Blog Blog { get; set; }

        [DataMember(Name = "is_nsfw", EmitDefaultValue = false)]
        public bool IsNsfw { get; set; }

        [DataMember(Name = "nsfw_score", EmitDefaultValue = false)]
        public double NsfwScore { get; set; }

        [DataMember(Name = "_links", EmitDefaultValue = false)]
        public Links Links { get; set; }

        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        [DataMember(Name = "post_url", EmitDefaultValue = false)]
        public string PostUrl { get; set; }

        [DataMember(Name = "slug", EmitDefaultValue = false)]
        public string Slug { get; set; }

        [DataMember(Name = "date", EmitDefaultValue = false)]
        public string Date { get; set; }

        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public int Timestamp { get; set; }

        [DataMember(Name = "state", EmitDefaultValue = false)]
        public string State { get; set; }

        [DataMember(Name = "format", EmitDefaultValue = false)]
        public string Format { get; set; }

        [DataMember(Name = "reblog_key", EmitDefaultValue = false)]
        public string ReblogKey { get; set; }

        [DataMember(Name = "tags", EmitDefaultValue = false)]
        public List<string> Tags { get; set; }

        [DataMember(Name = "short_url", EmitDefaultValue = false)]
        public string ShortUrl { get; set; }

        [DataMember(Name = "summary", EmitDefaultValue = false)]
        public string Summary { get; set; }

        [DataMember(Name = "is_blocks_post_format", EmitDefaultValue = false)]
        public bool IsBlocksPostFormat { get; set; }

        [DataMember(Name = "recommended_source", EmitDefaultValue = false)]
        public string RecommendedSource { get; set; }

        [DataMember(Name = "recommended_color", EmitDefaultValue = false)]
        public string RecommendedColor { get; set; }

        [DataMember(Name = "followed", EmitDefaultValue = false)]
        public bool Followed { get; set; }

        [DataMember(Name = "liked", EmitDefaultValue = false)]
        public bool Liked { get; set; }

        [DataMember(Name = "source_url", EmitDefaultValue = false)]
        public string SourceUrl { get; set; }

        [DataMember(Name = "source_title", EmitDefaultValue = false)]
        public string SourceTitle { get; set; }

        [DataMember(Name = "caption", EmitDefaultValue = false)]
        public string Caption { get; set; }

        [DataMember(Name = "reblog", EmitDefaultValue = false)]
        public Reblog Reblog { get; set; }

        [DataMember(Name = "trail", EmitDefaultValue = false)]
        public List<Trail> Trail { get; set; }

        [DataMember(Name = "video_url", EmitDefaultValue = false)]
        public string VideoUrl { get; set; }

        [DataMember(Name = "html5_capable", EmitDefaultValue = false)]
        public bool Html5Capable { get; set; }

        [DataMember(Name = "video", EmitDefaultValue = false)]
        public Video Video { get; set; }

        [DataMember(Name = "thumbnail_url", EmitDefaultValue = false)]
        public string ThumbnailUrl { get; set; }

        [DataMember(Name = "thumbnail_width", EmitDefaultValue = false)]
        public int ThumbnailWidth { get; set; }

        [DataMember(Name = "thumbnail_height", EmitDefaultValue = false)]
        public int ThumbnailHeight { get; set; }

        [DataMember(Name = "duration", EmitDefaultValue = false)]
        public float Duration { get; set; }

        [DataMember(Name = "player", EmitDefaultValue = false)]
        public object Player { get; set; }

        [DataMember(Name = "audio_url", EmitDefaultValue = false)]
        public string AudioUrl { get; set; }

        [DataMember(Name = "audio_source_url", EmitDefaultValue = false)]
        public string AudioSourceUrl { get; set; }

        [DataMember(Name = "audio_type", EmitDefaultValue = false)]
        public string AudioType { get; set; }

        [DataMember(Name = "video_type", EmitDefaultValue = false)]
        public string VideoType { get; set; }

        [DataMember(Name = "link_url", EmitDefaultValue = false)]
        public string LinkUrl { get; set; }

        [DataMember(Name = "image_permalink", EmitDefaultValue = false)]
        public string ImagePermalink { get; set; }

        [DataMember(Name = "photos", EmitDefaultValue = false)]
        public List<Photo> Photos { get; set; }

        [DataMember(Name = "reblogged_from_id", EmitDefaultValue = false)]
        public string RebloggedFromId { get; set; }

        [DataMember(Name = "reblogged_from_url", EmitDefaultValue = false)]
        public string RebloggedFromUrl { get; set; }

        [DataMember(Name = "reblogged_from_name", EmitDefaultValue = false)]
        public string RebloggedFromName { get; set; }

        [DataMember(Name = "reblogged_from_title", EmitDefaultValue = false)]
        public string RebloggedFromTitle { get; set; }

        [DataMember(Name = "reblogged_from_uuid", EmitDefaultValue = false)]
        public string RebloggedFromUuid { get; set; }

        [DataMember(Name = "reblogged_from_can_message", EmitDefaultValue = false)]
        public bool RebloggedFromCanMessage { get; set; }

        [DataMember(Name = "reblogged_from_following", EmitDefaultValue = false)]
        public bool RebloggedFromFollowing { get; set; }

        [DataMember(Name = "reblogged_root_id", EmitDefaultValue = false)]
        public string RebloggedRootId { get; set; }

        [DataMember(Name = "reblogged_root_url", EmitDefaultValue = false)]
        public string RebloggedRootUrl { get; set; }

        [DataMember(Name = "reblogged_root_name", EmitDefaultValue = false)]
        public string RebloggedRootName { get; set; }

        [DataMember(Name = "reblogged_root_title", EmitDefaultValue = false)]
        public string RebloggedRootTitle { get; set; }

        [DataMember(Name = "reblogged_root_uuid", EmitDefaultValue = false)]
        public string RebloggedRootUuid { get; set; }

        [DataMember(Name = "reblogged_root_can_message", EmitDefaultValue = false)]
        public bool RebloggedRootCanMessage { get; set; }

        [DataMember(Name = "reblogged_root_following", EmitDefaultValue = false)]
        public bool RebloggedRootFollowing { get; set; }

        [DataMember(Name = "can_like", EmitDefaultValue = false)]
        public bool CanLike { get; set; }

        [DataMember(Name = "can_reblog", EmitDefaultValue = false)]
        public bool CanReblog { get; set; }

        [DataMember(Name = "can_send_in_message", EmitDefaultValue = false)]
        public bool CanSendInMessage { get; set; }

        [DataMember(Name = "can_reply", EmitDefaultValue = false)]
        public bool CanReply { get; set; }

        [DataMember(Name = "display_avatar", EmitDefaultValue = false)]
        public bool DisplayAvatar { get; set; }

        [DataMember(Name = "tumblelog_key", EmitDefaultValue = false)]
        public string TumblelogKey { get; set; }

        [DataMember(Name = "tumblelog_uuid", EmitDefaultValue = false)]
        public string TumblelogUuid { get; set; }

        [DataMember(Name = "root_id", EmitDefaultValue = false)]
        public string RootId { get; set; }

        [DataMember(Name = "share_popover_data", EmitDefaultValue = false)]
        public SharePopoverData SharePopoverData { get; set; }

        [DataMember(Name = "posted_on_tooltip", EmitDefaultValue = false)]
        public string PostedOnTooltip { get; set; }

        [DataMember(Name = "tag_layout_class", EmitDefaultValue = false)]
        public string TagLayoutClass { get; set; }

        [DataMember(Name = "has_custom_source_url", EmitDefaultValue = false)]
        public bool HasCustomSourceUrl { get; set; }

        [DataMember(Name = "tumblelog", EmitDefaultValue = false)]
        public string Tumblelog { get; set; }

        [DataMember(Name = "reblogged_from_tumblr_url", EmitDefaultValue = false)]
        public string RebloggedFromTumblrUrl { get; set; }

        [DataMember(Name = "notes", EmitDefaultValue = false)]
        public Notes Notes { get; set; }

        [DataMember(Name = "reblogged_from_followed", EmitDefaultValue = false)]
        public bool RebloggedFromFollowed { get; set; }

        [DataMember(Name = "post_html", EmitDefaultValue = false)]
        public string PostHtml { get; set; }

        [DataMember(Name = "asking_name", EmitDefaultValue = false)]
        public string AskingName { get; set; }

        [DataMember(Name = "asking_url", EmitDefaultValue = false)]
        public string AskingUrl { get; set; }

        [DataMember(Name = "question", EmitDefaultValue = false)]
        public string Question { get; set; }

        [DataMember(Name = "answer", EmitDefaultValue = false)]
        public string Answer { get; set; }

        [DataMember(Name = "photoset_layout", EmitDefaultValue = false)]
        public string PhotosetLayout { get; set; }

        [DataMember(Name = "photoset_photos", EmitDefaultValue = false)]
        public List<PhotosetPhoto> PhotosetPhotos { get; set; }

        [DataMember(Name = "year", EmitDefaultValue = false)]
        public int? Year { get; set; }

        [DataMember(Name = "track", EmitDefaultValue = false)]
        public string Track { get; set; }

        [DataMember(Name = "is_external", EmitDefaultValue = false)]
        public bool? IsExternal { get; set; }

        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Title { get; set; }

        [DataMember(Name = "body", EmitDefaultValue = false)]
        public string Body { get; set; }

        [DataMember(Name = "text", EmitDefaultValue = false)]
        public string Text { get; set; }

        [DataMember(Name = "source", EmitDefaultValue = false)]
        public string Source { get; set; }

        [DataMember(Name = "artist", EmitDefaultValue = false)]
        public string Artist { get; set; }

        [DataMember(Name = "track_name", EmitDefaultValue = false)]
        public string TrackName { get; set; }

        [DataMember(Name = "album_art", EmitDefaultValue = false)]
        public string AlbumArt { get; set; }

        [DataMember(Name = "embed", EmitDefaultValue = false)]
        public string Embed { get; set; }

        [DataMember(Name = "plays", EmitDefaultValue = false)]
        public int Plays { get; set; }

        [DataMember(Name = "album", EmitDefaultValue = false)]
        public string Album { get; set; }

        [DataMember(Name = "dialogue", EmitDefaultValue = false)]
        public List<Dialogue> dialogue { get; set; }

        [DataMember(Name = "is_anonymous", EmitDefaultValue = false)]
        public bool? IsAnonymous { get; set; }

        [DataMember(Name = "is_submission", EmitDefaultValue = false)]
        public bool? IsSubmission { get; set; }

        [DataMember(Name = "should_bypass_tagfiltering", EmitDefaultValue = false)]
        public bool ShouldypassTagfiltering { get; set; }

        [DataMember(Name = "should_bypass_safemode", EmitDefaultValue = false)]
        public bool ShouldBypassSafemode { get; set; }

        [DataMember(Name = "can_modify_safe_mode", EmitDefaultValue = false)]
        public bool CanModifySafeMode { get; set; }

        [DataMember(Name = "survey", EmitDefaultValue = false)]
        public object Survey { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "link_image", EmitDefaultValue = false)]
        public string LinkImage { get; set; }

        [DataMember(Name = "link_image_dimensions", EmitDefaultValue = false)]
        public LinkImageDimensions LinkImageDimensions { get; set; }

        [DataMember(Name = "link_author", EmitDefaultValue = false)]
        public object LinkAuthor { get; set; }

        [DataMember(Name = "excerpt", EmitDefaultValue = false)]
        public string Excerpt { get; set; }

        [DataMember(Name = "publisher", EmitDefaultValue = false)]
        public string Publisher { get; set; }

        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string Description { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    [DataContract]
    public class PixelbugUrl
    {
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "script", EmitDefaultValue = false)]
        public string Script { get; set; }

        [DataMember(Name = "img", EmitDefaultValue = false)]
        public string Img { get; set; }
    }

    [DataContract]
    public class PixelbugPost
    {
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "script", EmitDefaultValue = false)]
        public string Script { get; set; }

        [DataMember(Name = "img", EmitDefaultValue = false)]
        public string Img { get; set; }
    }

    [DataContract]
    public class Player
    {
        [DataMember(Name = "width", EmitDefaultValue = false)]
        public int Width { get; set; }

        [DataMember(Name = "embed_code", EmitDefaultValue = false)]
        public string EmbedCode { get; set; }
    }

    [DataContract]
    public class Youtube
    {
        [DataMember(Name = "video_id", EmitDefaultValue = false)]
        public string VideoId { get; set; }

        [DataMember(Name = "width", EmitDefaultValue = false)]
        public int Width { get; set; }

        [DataMember(Name = "height", EmitDefaultValue = false)]
        public int Height { get; set; }
    }

    [DataContract]
    public class Video
    {
        [DataMember(Name = "youtube", EmitDefaultValue = false)]
        public Youtube Youtube { get; set; }
    }

    [DataContract]
    public class TrackingHtml
    {
        [DataMember(Name = "pixelbug_url", EmitDefaultValue = false)]
        public PixelbugUrl PixelbugUrl { get; set; }

        [DataMember(Name = "pixelbug_post", EmitDefaultValue = false)]
        public PixelbugPost PixelbugPost { get; set; }
    }

}
