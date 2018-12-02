using System.Threading.Tasks;

namespace TumblThree.Applications.Services
{
    public interface ILoginService
    {
        Task PerformTumblrLoginAsync(string login, string password);

        void PerformTumblrLogout();

        Task PerformTumblrTFALoginAsync(string login, string tumblrTFAAuthCode);

        bool CheckIfTumblrTFANeeded();

        bool CheckIfLoggedInAsync();

        Task<string> GetTumblrUsernameAsync();
    }
}
