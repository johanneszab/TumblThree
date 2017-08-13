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
                return new Blog(blogUrl, path, BlogTypes.tlb);
            //if (Validator.IsValidTumblrSearchUrl(blogUrl))
            //    return new Blog(blogUrl, path, BlogTypes.ts);
            throw new ArgumentException("Website is not supported!", nameof(blogUrl));
        }
    }
}
