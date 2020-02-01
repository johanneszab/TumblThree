using System;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Domain.Models
{
    [Export(typeof(IBlogFactory))]
    public class BlogFactory : IBlogFactory
    {
        private readonly IUrlValidator urlValidator;
        private readonly Regex tumbexRegex = new Regex("(http[A-Za-z0-9_/:.]*www.tumbex.com/([A-Za-z0-9_/:.-]*)\\.tumblr/)");

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
                   || urlValidator.IsValidTumblrTagSearchUrl(blogUrl)
                   || urlValidator.IsTumbexUrl(blogUrl);

        }

        public IBlog GetBlog(string blogUrl, string path)
        {
            blogUrl = urlValidator.AddHttpsProtocol(blogUrl);
            if (urlValidator.IsValidTumblrUrl(blogUrl))
                return TumblrBlog.Create(blogUrl, path);
            if (urlValidator.IsTumbexUrl(blogUrl))
                return TumblrBlog.Create(CreateTumblrUrlFromTumbex(blogUrl), path);
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

        //TODO: Refactor out.
        private string CreateTumblrUrlFromTumbex(string blogUrl)
        {
            Match match = tumbexRegex.Match(blogUrl);
            String tumblrBlogName = match.Groups[2].Value;

            return $"https://{tumblrBlogName}.tumblr.com/";
        }
    }
}
