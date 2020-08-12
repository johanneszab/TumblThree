using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Applications.DataModels.TumblrSearchJson
{
    [DataContract]
    public class TumblrSearchJson
    {
        [DataMember(Name = "meta", EmitDefaultValue = false)]
        public Meta Meta { get; set; }

        [DataMember(Name = "response", EmitDefaultValue = false)]
        public Response Response { get; set; }
    }

    [DataContract]
    public class Meta
    {
        [DataMember(Name = "status", EmitDefaultValue = false)]
        public int Status { get; set; }

        [DataMember(Name = "msg", EmitDefaultValue = false)]
        public string Msg { get; set; }
    }

    [DataContract]
    public class Response
    {
        [DataMember(Name = "posts_html", EmitDefaultValue = false)]
        public string PostsHtml { get; set; }

        [DataMember(Name = "blogs_html", EmitDefaultValue = false)]
        public object BlogsHtml { get; set; }

        [DataMember(Name = "tracking_html", EmitDefaultValue = false)]
        public string TrackingHtml { get; set; }

        [DataMember(Name = "related_searches", EmitDefaultValue = false)]
        public List<string> RelatedSearches { get; set; }

        [DataMember(Name = "show_psa", EmitDefaultValue = false)]
        public bool ShowPsa { get; set; }

        [DataMember(Name = "tracked_tag", EmitDefaultValue = false)]
        public bool TrackedTag { get; set; }

        [DataMember(Name = "yahoo_view_data", EmitDefaultValue = false)]
        public List<object> YahooViewData { get; set; }
    }
}
