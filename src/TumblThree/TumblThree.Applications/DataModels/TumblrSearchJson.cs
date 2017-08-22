using System.Collections.Generic;

namespace TumblThree.Applications.DataModels.TumblrSearchJson
{
    public class TumblrSearchJson
    {
        public Meta meta { get; set; }
        public Response response { get; set; }
    }

    public class Meta
    {
        public int status { get; set; }
        public string msg { get; set; }
    }

    public class Response
    {
        public string posts_html { get; set; }
        public object blogs_html { get; set; }
        public string tracking_html { get; set; }
        public List<string> related_searches { get; set; }
        public bool show_psa { get; set; }
        public bool tracked_tag { get; set; }
        public List<object> yahoo_view_data { get; set; }
    }
}
