namespace TumblThree.Applications.DataModels
{
	public enum PostType
	{
		Binary,
		Text
	}

	public enum UrlType
	{
		none,
		Mega
	}

	public abstract class TumblrPost
	{
		public PostType PostType { get; protected set; }

		public string Url { get; }

		public UrlType urltype { get; protected set;}

		public string Id { get; }

		public string Date { get; }

		public string DbType { get; protected set; }

		public string TextFileLocation { get; protected set; }

		public TumblrPost(string url, string id, string date, UrlType utype)
		{
			Url = url;
			Id = id;
			Date = date;
			urltype = utype;
		}
	}
}