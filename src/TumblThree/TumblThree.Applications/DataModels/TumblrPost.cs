using System;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.DataModels
{
    public struct TumblrPost
    {
        public readonly PostTypes PostType;
        public readonly string Url;
        public readonly string Id;
        public readonly string Date;

        public TumblrPost(PostTypes postType, string url, string id, string date)
        {
            this.PostType = postType;
            this.Url = url;
            this.Id = id;
            this.Date = date;
        }

        public TumblrPost(PostTypes postType, string url, string id)
        {
            this.PostType = postType;
            this.Url = url;
            this.Id = id;
            this.Date = string.Empty;
        }
    }
}
