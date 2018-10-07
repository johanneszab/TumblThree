using System.Runtime.Serialization;

namespace TumblThree.Domain.Models.Files
{
    [DataContract]
    public class TumblrSearchBlogFiles : Files
    {
        public TumblrSearchBlogFiles(string name, string location) : base(name, location)
        {
            BlogType = BlogTypes.tumblrsearch;
        }
    }
}
