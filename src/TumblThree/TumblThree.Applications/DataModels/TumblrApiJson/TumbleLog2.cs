using System.Runtime.Serialization;

namespace TumblThree.Applications.DataModels.TumblrApiJson
{
    public class TumbleLog2
    {
        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Title { get; set; }
        
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "cname", EmitDefaultValue = false)]
        public object CName { get; set; }
        
        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }
        
        [DataMember(Name = "timezone", EmitDefaultValue = false)]
        public string Timezone { get; set; }
        
        [DataMember(Name = "avatar_url_16", EmitDefaultValue = false)]
        public string AvatarUrl16 { get; set; }
        
        [DataMember(Name = "avatar_url_24", EmitDefaultValue = false)]
        public string AvatarUrl24 { get; set; }
        
        [DataMember(Name = "avatar_url_30", EmitDefaultValue = false)]
        public string AvatarUrl30 { get; set; }
        
        [DataMember(Name = "avatar_url_40", EmitDefaultValue = false)]
        public string AvatarUrl40 { get; set; }
        
        [DataMember(Name = "avatar_url_48", EmitDefaultValue = false)]
        public string AvatarUrl48 { get; set; }
        
        [DataMember(Name = "avatar_url_64", EmitDefaultValue = false)]
        public string AvatarUrl64 { get; set; }
        
        [DataMember(Name = "avatar_url_96", EmitDefaultValue = false)]
        public string AvatarUrl96 { get; set; }
        
        [DataMember(Name = "avatar_url_128", EmitDefaultValue = false)]
        public string AvatarUrl128 { get; set; }
        
        [DataMember(Name = "avatar_url_512", EmitDefaultValue = false)]
        public string AvatarUrl512 { get; set; }
    }
}