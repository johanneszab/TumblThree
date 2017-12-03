using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class TumblrTagSearchBlogFiles : Files
    {
        public TumblrTagSearchBlogFiles(string name, string location) : base(name, location)
        {
            BlogType = BlogTypes.tumblrtagsearch;
        }
    }
}
