namespace TumblThree.Applications.DataModels
{
    public class FolderItem
    {
        public FolderItem(string path, string displayName)
        {
            Path = path;
            DisplayName = displayName;
        }

        public string Path { get; }

        public string DisplayName { get; }
    }
}
