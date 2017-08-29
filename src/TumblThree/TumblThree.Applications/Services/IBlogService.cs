using System;

namespace TumblThree.Applications.Services
{
    public interface IBlogService
    {
        void UpdateBlogProgress();

        void UpdateBlogPostCount(string propertyName);

        void UpdateBlogDB(string fileName);

        bool CreateDataFolder();

        bool CheckIfFileExistsInDB(string url);

        bool CheckIfBlogShouldCheckDirectory(string url);

        bool CheckIfFileExistsInDirectory(string url);

        void SaveFiles();
    }
}
