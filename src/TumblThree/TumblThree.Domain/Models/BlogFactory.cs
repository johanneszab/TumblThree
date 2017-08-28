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

        public IBlog GetBlog(string blogUrl, string path)
        {
            if (Validator.IsValidTumblrUrl(blogUrl))
                return Blog.Create(blogUrl, path, BlogTypes.tumblr);
            if (Validator.IsValidTumblrLikedByUrl(blogUrl))
                return TumblrLikeByBlog.Create(blogUrl, path, BlogTypes.tlb);
            if (Validator.IsValidTumblrSearchUrl(blogUrl))
                return TumblrSearchBlog.Create(blogUrl, path, BlogTypes.tumblrsearch);
            if (Validator.IsValidTumblrTagSearchUrl(blogUrl))
                return TumblrTagSearchBlog.Create(blogUrl, path, BlogTypes.tumblrtagsearch);
            throw new ArgumentException("Website is not supported!", nameof(blogUrl));
        }
    }
}
