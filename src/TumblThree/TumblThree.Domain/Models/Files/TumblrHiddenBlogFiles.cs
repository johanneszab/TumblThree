using System.Runtime.Serialization;

namespace TumblThree.Domain.Models.Files
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
