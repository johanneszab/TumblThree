using System;
using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class TumblrFiles : Files
    {

        public TumblrFiles(string name, string location, BlogTypes blogType) : base(name, location, blogType)
        {
            Version = "1";
        }

        [DataMember]
        public string Version { get; set; }
    }
}
