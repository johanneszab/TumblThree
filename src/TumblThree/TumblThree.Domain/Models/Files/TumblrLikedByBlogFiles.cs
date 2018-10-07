using System.Runtime.Serialization;

namespace TumblThree.Domain.Models.Files
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
