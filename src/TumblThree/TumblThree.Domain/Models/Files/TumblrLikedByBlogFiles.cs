using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class TumblrLikedByBlogFiles : Files
    {
        public TumblrLikedByBlogFiles(string name, string location) : base(name, location)
        {
            BlogType = BlogTypes.tlb;
        }
    }
}
