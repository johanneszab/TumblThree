using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Applications.Properties
{
    [DataContract]
    public sealed class QueueSettings : IExtensibleDataObject
    {
        [DataMember(Name = "Names")]
        private readonly List<string> names;


        public QueueSettings()
        {
            this.names = new List<string>();
        }


        [DataMember]
        public string LastCrawledBlogName { get; set; }

        public IReadOnlyList<string> Names { get { return names; } }

        ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

        public void ReplaceAll(IEnumerable<string> newBlogNames)
        {
            names.Clear();
            names.AddRange(newBlogNames);
        }
    }
}
