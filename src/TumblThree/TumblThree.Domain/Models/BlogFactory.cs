using System;
using System.ComponentModel.Composition;

namespace TumblThree.Domain.Models
{
    [Export(typeof(IBlogFactory))]
    public class BlogFactory : IBlogFactory
    {
        [ImportingConstructor]
        internal BlogFactory()
        {
        }

        public bool IsValidTumblrBlogUrl(string blogUrl)
        {
            return Validator.IsValidTumblrUrl(blogUrl)
                || Validator.IsValidTumblrLikedByUrl(blogUrl)
                || Validator.IsValidTumblrSearchUrl(blogUrl)
                || Validator.IsValidTumblrTagSearchUrl(blogUrl);
        }

        public IBlog GetBlog(string blogUrl, string path)
        {
            if (Validator.IsValidTumblrUrl(blogUrl))
                return TumblrBlog.Create(blogUrl, path);
            if (Validator.IsValidTumblrLikedByUrl(blogUrl))
                return TumblrLikedByBlog.Create(blogUrl, path);
            if (Validator.IsValidTumblrSearchUrl(blogUrl))
                return TumblrSearchBlog.Create(blogUrl, path);
            if (Validator.IsValidTumblrTagSearchUrl(blogUrl))
                return TumblrTagSearchBlog.Create(blogUrl, path);
            throw new ArgumentException("Website is not supported!", nameof(blogUrl));
        }
    }
}
