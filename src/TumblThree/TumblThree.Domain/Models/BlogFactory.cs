using System;
using System.ComponentModel.Composition;

namespace TumblThree.Domain.Models
{
    [Export(typeof(IBlogFactory))]
    public class BlogFactory : IBlogFactory
    {
        IUrlValidator urlValidator;

        [ImportingConstructor]
        internal BlogFactory(IUrlValidator urlValidator)
        {
            this.urlValidator = urlValidator;
        }

        public bool IsValidTumblrBlogUrl(string blogUrl)
        {
            blogUrl = urlValidator.AddHttpsProtocol(blogUrl);
            return urlValidator.IsValidTumblrUrl(blogUrl)
                || urlValidator.IsValidTumblrHiddenUrl(blogUrl)
                || urlValidator.IsValidTumblrLikedByUrl(blogUrl)
                || urlValidator.IsValidTumblrSearchUrl(blogUrl)
                || urlValidator.IsValidTumblrTagSearchUrl(blogUrl);
        }

        public IBlog GetBlog(string blogUrl, string path)
        {
            blogUrl = urlValidator.AddHttpsProtocol(blogUrl);
            if (urlValidator.IsValidTumblrUrl(blogUrl))
                return TumblrBlog.Create(blogUrl, path);
            if (urlValidator.IsValidTumblrHiddenUrl(blogUrl))
                return TumblrHiddenBlog.Create(blogUrl, path);
            if (urlValidator.IsValidTumblrLikedByUrl(blogUrl))
                return TumblrLikedByBlog.Create(blogUrl, path);
            if (urlValidator.IsValidTumblrSearchUrl(blogUrl))
                return TumblrSearchBlog.Create(blogUrl, path);
            if (urlValidator.IsValidTumblrTagSearchUrl(blogUrl))
                return TumblrTagSearchBlog.Create(blogUrl, path);
            throw new ArgumentException("Website is not supported!", nameof(blogUrl));
        }
    }
}
