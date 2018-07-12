using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Applications.DataModels.TumblrSearchJson
{
    [DataContract]
    public class TumblrSearchJson
    {
        [DataMember(EmitDefaultValue = false)]
        public Meta meta { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Response response { get; set; }
    }

    [DataContract]
    public class Meta
    {
        [DataMember(EmitDefaultValue = false)]
        public int status { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string msg { get; set; }
    }

    [DataContract]
    public class Response
    {
        [DataMember(EmitDefaultValue = false)]
        public string posts_html { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public object blogs_html { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string tracking_html { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<string> related_searches { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool show_psa { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool tracked_tag { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<object> yahoo_view_data { get; set; }
    }
}
