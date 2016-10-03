using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TumblThree.Applications.DataModels
{
    public class TumblrPost
    {
        private Post post;
        private TumblrJson json;
        private long identification;
        private string url;
        private string shortUrl;
        private string type;
        private string date;
        private DateTime isoDate;
        private DateTime localDate;
        private object title;
        private List<object> tags;
        private int noteCount;
        private string sourceTitle;
        private string sourceUrl;
        private string fileName;
        private string llink;

        public TumblrPost(Post post)
        {
            this.title = String.Empty;
            this.post = post;
            this.identification = post.id;
            this.url = post.post_url;
            this.shortUrl = post.short_url;
            this.type = post.type;
            this.date = post.date;
            this.isoDate = new System.DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((Convert.ToDouble(post.date)));
            this.localDate = isoDate.ToLocalTime();
            this.tags = post.tags;
            this.noteCount = post.note_count;
            this.sourceTitle = post.reblogged_root_title;
            this.sourceUrl = post.reblogged_root_url;
        }

        private bool saveContent()
        {
            /// <summary>
            /// parse the content of a TumblrPost
            /// <para>the blog for the url</para>
            /// </summary>
            /// 
            List<object> content = new List<object>();

            if (type.Equals("photo"))
            {
                foreach (Photo photo in post.photos)
                {
                    //url = photo.original_size.url;
                }
            }

            if (type.Equals("link"))
            {

            }

            if (type.Equals("text"))
            {
                this.title = post.title.ToString();
                content.Add(post.body);

            }

            if (type.Equals("quote"))
            {

            }

            if (type.Equals("chat"))
            {

            }

            if (type.Equals("answer"))
            {

            }

            if (type.Equals("audio"))
            {

            }

            if (type.Equals("video"))
            {

            }
            return true;
        }

    }
}
