using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class TumblrLikeByBlog : Blog
    {
        public TumblrLikeByBlog(string url, string location, BlogTypes blogType) : base(url, location, blogType)
        {            
        }

        protected override string ExtractName()
        {
            return Url.Split('/')[5];
        }

        protected override string ExtractUrl()
        {
            int index = Url.Split('/')[5].Length;
            var lengthOfUrl = 32;
            return Url.Substring(0, index + lengthOfUrl);
        }
    }
}
