using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class TumblrHiddenBlogFiles : Files
    {
        public TumblrHiddenBlogFiles(string name, string location) : base(name, location)
        {
            BlogType = BlogTypes.tmblrpriv;
        }
    }
}
