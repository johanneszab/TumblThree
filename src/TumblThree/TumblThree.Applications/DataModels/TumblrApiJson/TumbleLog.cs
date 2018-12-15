using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Applications.DataModels.TumblrApiJson
{
    public class TumbleLog
    {
        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Title { get; set; }

        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string Description { get; set; }

        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "timezone", EmitDefaultValue = false)]
        public string Timezone { get; set; }

        [DataMember(Name = "cname", EmitDefaultValue = false)]
        public object CName { get; set; }

        [DataMember(Name = "feeds", EmitDefaultValue = false)]
        public List<object> Feeds { get; set; }
    }
}
