using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Properties
{
    [DataContract]
    public sealed class QueueSettings : IExtensibleDataObject
    {
        [DataMember(Name = "Names")]
        private readonly List<Tuple<string, BlogTypes>> names;

        public QueueSettings()
        {
            names = new List<Tuple<string, BlogTypes>>();
        }

        [DataMember]
        public string LastCrawledBlogName { get; set; }

        public IReadOnlyList<Tuple<string, BlogTypes>> Names
        {
            get { return names; }
        }

        ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

        public void ReplaceAll(IEnumerable<Tuple<string, BlogTypes>> newBlogNames)
        {
            names.Clear();
            names.AddRange(newBlogNames);
        }
    }
}
