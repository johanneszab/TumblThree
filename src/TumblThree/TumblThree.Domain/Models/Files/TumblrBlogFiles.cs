using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class TumblrBlogFiles : Files
    {
        public TumblrBlogFiles(string name, string location) : base(name, location)
        {
            BlogType = BlogTypes.tumblr;
        }
    }
}
