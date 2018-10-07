using System.Collections.Generic;
using System.Runtime.Serialization;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Properties
{
    [DataContract]
    public sealed class QueueSettings : IExtensibleDataObject
    {
        [DataMember(Name = "Names")]
        private readonly List<string> names;

        [DataMember(Name = "Types")]
        private readonly List<BlogTypes> types;

        public QueueSettings()
        {
            names = new List<string>();
            types = new List<BlogTypes>();
        }

        [DataMember] public string LastCrawledBlogName { get; set; }

        [DataMember] public BlogTypes LastCrawledBlogType { get; set; }

        public IReadOnlyList<string> Names => names;

        public IReadOnlyList<BlogTypes> Types => types;

        ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

        public void ReplaceAll(IEnumerable<string> newBlogNames, IEnumerable<BlogTypes> newBlogTypes)
        {
            names.Clear();
            names.AddRange(newBlogNames);
            types.Clear();
            types.AddRange(newBlogTypes);
        }
    }
}
