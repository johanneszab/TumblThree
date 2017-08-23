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
                return new Blog(blogUrl, path, BlogTypes.tumblr);
            if (Validator.IsValidTumblrLikedByUrl(blogUrl))
                return new TumblrLikeByBlog(blogUrl, path, BlogTypes.tlb);
            if (Validator.IsValidTumblrSearchUrl(blogUrl))
                return new TumblrSearchBlog(blogUrl, path, BlogTypes.tumblrsearch);
            if (Validator.IsValidTumblrTagSearchUrl(blogUrl))
                return new TumblrTagSearchBlog(blogUrl, path, BlogTypes.tumblrtagsearch);
            throw new ArgumentException("Website is not supported!", nameof(blogUrl));
        }
    }
}
