using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Applications.DataModels.TumblrApiJson
{
    [DataContract]
    public class Post : ICloneable
    {
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "url-with-slug", EmitDefaultValue = false)]
        public string UrlWithSlug { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "date-gmt", EmitDefaultValue = false)]
        public string DateGmt { get; set; }

        [DataMember(Name = "date", EmitDefaultValue = false)]
        public string Date { get; set; }

        [DataMember(Name = "bookmarklet", EmitDefaultValue = false)]
        public int? Bookmarklet { get; set; }

        [DataMember(Name = "mobile", EmitDefaultValue = false)]
        public int? Mobile { get; set; }

        [DataMember(Name = "feed-item", EmitDefaultValue = false)]
        public string FeedItem { get; set; }

        [DataMember(Name = "from-feed-id", EmitDefaultValue = false)]
        public int FromFeedId { get; set; }

        [DataMember(Name = "unix-timestamp", EmitDefaultValue = false)]
        public int UnixTimestamp { get; set; }

        [DataMember(Name = "format", EmitDefaultValue = false)]
        public string Format { get; set; }

        [DataMember(Name = "reblog-key", EmitDefaultValue = false)]
        public string ReblogKey { get; set; }

        [DataMember(Name = "slug", EmitDefaultValue = false)]
        public string Slug { get; set; }

        [DataMember(Name = "is-submission", EmitDefaultValue = false)]
        public bool IsSubmission { get; set; }

        [DataMember(Name = "like-button", EmitDefaultValue = false)]
        public string LikeButton { get; set; }

        [DataMember(Name = "reblog-button", EmitDefaultValue = false)]
        public string ReblogButton { get; set; }

        [DataMember(Name = "note-count", EmitDefaultValue = false)]
        public string NoteCount { get; set; }

        [DataMember(Name = "reblogged-from-url", EmitDefaultValue = false)]
        public string RebloggedFromUrl { get; set; }

        [DataMember(Name = "reblogged-from-name", EmitDefaultValue = false)]
        public string RebloggedFromName { get; set; }

        [DataMember(Name = "reblogged-from-title", EmitDefaultValue = false)]
        public string RebloggedFromTitle { get; set; }

        [DataMember(Name = "reblogged-from-avatar-url-16", EmitDefaultValue = false)]
        public string RebloggedFromAvatarUrl16 { get; set; }

        [DataMember(Name = "reblogged-from-avatar-url-24", EmitDefaultValue = false)]
        public string RebloggedFromAvatarUrl24 { get; set; }

        [DataMember(Name = "reblogged-from-avatar-url-30", EmitDefaultValue = false)]
        public string RebloggedFromAvatarUrl30 { get; set; }

        [DataMember(Name = "reblogged-from-avatar-url-40", EmitDefaultValue = false)]
        public string RebloggedFromAvatarUrl40 { get; set; }

        [DataMember(Name = "reblogged-from-avatar-url-48", EmitDefaultValue = false)]
        public string RebloggedFromAvatarUrl48 { get; set; }

        [DataMember(Name = "reblogged-from-avatar-url-64", EmitDefaultValue = false)]
        public string RebloggedFromAvatarUrl64 { get; set; }

        [DataMember(Name = "reblogged-from-avatar-url-96", EmitDefaultValue = false)]
        public string RebloggedFromAvatarUrl96 { get; set; }

        [DataMember(Name = "reblogged-from-avatar-url-128", EmitDefaultValue = false)]
        public string RebloggedFromAvatarUrl128 { get; set; }

        [DataMember(Name = "reblogged-from-avatar-url-512", EmitDefaultValue = false)]
        public string RebloggedFromAvatarUrl512 { get; set; }

        [DataMember(Name = "reblogged-root-url", EmitDefaultValue = false)]
        public string RebloggedRootUrl { get; set; }

        [DataMember(Name = "reblogged-root-name", EmitDefaultValue = false)]
        public string RebloggedRootName { get; set; }

        [DataMember(Name = "reblogged-root-title", EmitDefaultValue = false)]
        public string RebloggedRootTitle { get; set; }

        [DataMember(Name = "reblogged-root-avatar-url-16", EmitDefaultValue = false)]
        public string RebloggedRootAvatarUrl16 { get; set; }

        [DataMember(Name = "reblogged-root-avatar-url-24", EmitDefaultValue = false)]
        public string RebloggedRootAvatarUrl24 { get; set; }

        [DataMember(Name = "reblogged-root-avatar-url-30", EmitDefaultValue = false)]
        public string RebloggedRootAvatarUrl30 { get; set; }

        [DataMember(Name = "reblogged-root-avatar-url-40", EmitDefaultValue = false)]
        public string RebloggedRootAvatarUrl40 { get; set; }

        [DataMember(Name = "reblogged-root-avatar-url-48", EmitDefaultValue = false)]
        public string RebloggedRootAvatarUrl48 { get; set; }

        [DataMember(Name = "reblogged-root-avatar-url-64", EmitDefaultValue = false)]
        public string RebloggedRootAvatarUrl64 { get; set; }

        [DataMember(Name = "reblogged-root-avatar-url-96", EmitDefaultValue = false)]
        public string RebloggedRootAvatarUrl96 { get; set; }

        [DataMember(Name = "reblogged-root-avatar-url-128", EmitDefaultValue = false)]
        public string RebloggedRootAvatarUrl128 { get; set; }

        [DataMember(Name = "reblogged-root-avatar-url-512", EmitDefaultValue = false)]
        public string RebloggedRootAvatarUrl512 { get; set; }

        [DataMember(Name = "tumblelog", EmitDefaultValue = false)]
        public TumbleLog2 Tumblelog { get; set; }

        [DataMember(Name = "quote-text", EmitDefaultValue = false)]
        public string QuoteText { get; set; }

        [DataMember(Name = "quote-source", EmitDefaultValue = false)]
        public string QuoteSource { get; set; }

        [DataMember(Name = "tags", EmitDefaultValue = false)]
        public List<string> Tags { get; set; }

        [DataMember(Name = "photo-caption", EmitDefaultValue = false)]
        public string PhotoCaption { get; set; }

        [DataMember(Name = "width", EmitDefaultValue = false)]
        public object Width { get; set; }

        [DataMember(Name = "height", EmitDefaultValue = false)]
        public object Height { get; set; }

        [DataMember(Name = "photo-url-1280", EmitDefaultValue = false)]
        public string PhotoUrl1280 { get; set; }

        [DataMember(Name = "photo-url-500", EmitDefaultValue = false)]
        public string PhotoUrl500 { get; set; }

        [DataMember(Name = "photo-url-400", EmitDefaultValue = false)]
        public string PhotoUrl400 { get; set; }

        [DataMember(Name = "photo-url-250", EmitDefaultValue = false)]
        public string PhotoUrl250 { get; set; }

        [DataMember(Name = "photo-url-100", EmitDefaultValue = false)]
        public string PhotoUrl100 { get; set; }

        [DataMember(Name = "photo-url-75", EmitDefaultValue = false)]
        public string PhotoUrl75 { get; set; }

        [DataMember(Name = "photos", EmitDefaultValue = false)]
        public List<Photo> Photos { get; set; }

        [DataMember(Name = "photo-link-url", EmitDefaultValue = false)]
        public string PhotoLinkUrl { get; set; }

        [DataMember(Name = "id3-artist", EmitDefaultValue = false)]
        public string Id3Artist { get; set; }

        [DataMember(Name = "id3-album", EmitDefaultValue = false)]
        public string Id3Album { get; set; }

        [DataMember(Name = "id3-year", EmitDefaultValue = false)]
        public string Id3Year { get; set; }

        [DataMember(Name = "id3-track", EmitDefaultValue = false)]
        public string Id3Track { get; set; }

        [DataMember(Name = "id3-title", EmitDefaultValue = false)]
        public string Id3Title { get; set; }

        [DataMember(Name = "audio-caption", EmitDefaultValue = false)]
        public string AudioCaption { get; set; }

        [DataMember(Name = "audio-player", EmitDefaultValue = false)]
        public string AudioPlayer { get; set; }

        [DataMember(Name = "audio-embed", EmitDefaultValue = false)]
        public string AudioEmbed { get; set; }

        [DataMember(Name = "audio-plays", EmitDefaultValue = false)]
        public int? AudioPlays { get; set; }

        [DataMember(Name = "regular-title", EmitDefaultValue = false)]
        public string RegularTitle { get; set; }

        [DataMember(Name = "regular-body", EmitDefaultValue = false)]
        public string RegularBody { get; set; }

        [DataMember(Name = "link-text", EmitDefaultValue = false)]
        public string LinkText { get; set; }

        [DataMember(Name = "link-url", EmitDefaultValue = false)]
        public string LinkUrl { get; set; }

        [DataMember(Name = "link-description", EmitDefaultValue = false)]
        public string LinkDescription { get; set; }

        [DataMember(Name = "conversation-title", EmitDefaultValue = false)]
        public string ConversationTitle { get; set; }

        [DataMember(Name = "conversation-text", EmitDefaultValue = false)]
        public string ConversationText { get; set; }

        [DataMember(Name = "video-caption", EmitDefaultValue = false)]
        public string VideoCaption { get; set; }

        [DataMember(Name = "video-source", EmitDefaultValue = false)]
        public string VideoSource { get; set; }

        [DataMember(Name = "video-player", EmitDefaultValue = false)]
        public string VideoPlayer { get; set; }

        [DataMember(Name = "video-player-500", EmitDefaultValue = false)]
        public string VideoPlayer500 { get; set; }

        [DataMember(Name = "video-player-250", EmitDefaultValue = false)]
        public string VideoPlayer250 { get; set; }

        [DataMember(Name = "conversation", EmitDefaultValue = false)]
        public List<Conversation> Conversation { get; set; }

        [DataMember(Name = "submitter", EmitDefaultValue = false)]
        public string Submitter { get; set; }

        [DataMember(Name = "question", EmitDefaultValue = false)]
        public string Question { get; set; }

        [DataMember(Name = "answer", EmitDefaultValue = false)]
        public string Answer { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }

        [OnDeserializing]
        internal void OnDeserializingMethod(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            Id = string.Empty;
            Url = string.Empty;
            UrlWithSlug = string.Empty;
            Type = string.Empty;
            DateGmt = string.Empty;
            Date = string.Empty;
            Bookmarklet = 0;
            Mobile = 0;
            FeedItem = string.Empty;
            FromFeedId = 0;
            UnixTimestamp = 0;
            Format = string.Empty;
            ReblogKey = string.Empty;
            Slug = string.Empty;
            IsSubmission = false;
            LikeButton = string.Empty;
            ReblogButton = string.Empty;
            NoteCount = string.Empty;
            RebloggedFromUrl = string.Empty;
            RebloggedFromName = string.Empty;
            RebloggedFromTitle = string.Empty;
            RebloggedFromAvatarUrl16 = string.Empty;
            RebloggedFromAvatarUrl24 = string.Empty;
            RebloggedFromAvatarUrl30 = string.Empty;
            RebloggedFromAvatarUrl40 = string.Empty;
            RebloggedFromAvatarUrl48 = string.Empty;
            RebloggedFromAvatarUrl64 = string.Empty;
            RebloggedFromAvatarUrl96 = string.Empty;
            RebloggedFromAvatarUrl128 = string.Empty;
            RebloggedFromAvatarUrl512 = string.Empty;
            RebloggedRootUrl = string.Empty;
            RebloggedRootName = string.Empty;
            RebloggedRootTitle = string.Empty;
            RebloggedRootAvatarUrl16 = string.Empty;
            RebloggedRootAvatarUrl24 = string.Empty;
            RebloggedRootAvatarUrl30 = string.Empty;
            RebloggedRootAvatarUrl40 = string.Empty;
            RebloggedRootAvatarUrl48 = string.Empty;
            RebloggedRootAvatarUrl64 = string.Empty;
            RebloggedRootAvatarUrl96 = string.Empty;
            RebloggedRootAvatarUrl128 = string.Empty;
            RebloggedRootAvatarUrl512 = string.Empty;
            Tumblelog = new TumbleLog2();
            QuoteText = string.Empty;
            QuoteSource = string.Empty;
            Tags = new List<string>();
            PhotoCaption = string.Empty;
            Width = 0;
            Height = 0;
            PhotoUrl1280 = string.Empty;
            PhotoUrl500 = string.Empty;
            PhotoUrl400 = string.Empty;
            PhotoUrl250 = string.Empty;
            PhotoUrl100 = string.Empty;
            PhotoUrl75 = string.Empty;
            Photos = new List<Photo>();
            PhotoLinkUrl = string.Empty;
            Id3Artist = string.Empty;
            Id3Album = string.Empty;
            Id3Year = string.Empty;
            Id3Track = string.Empty;
            Id3Title = string.Empty;
            AudioCaption = string.Empty;
            AudioPlayer = string.Empty;
            AudioEmbed = string.Empty;
            AudioPlays = 0;
            RegularTitle = string.Empty;
            RegularBody = string.Empty;
            LinkText = string.Empty;
            LinkUrl = string.Empty;
            LinkDescription = string.Empty;
            ConversationTitle = string.Empty;
            ConversationText = string.Empty;
            VideoCaption = string.Empty;
            VideoSource = string.Empty;
            VideoPlayer = string.Empty;
            VideoPlayer500 = string.Empty;
            VideoPlayer250 = string.Empty;
            Conversation = new List<Conversation>();
            Submitter = string.Empty;
            Question = string.Empty;
            Answer = string.Empty;
        }
    }
}