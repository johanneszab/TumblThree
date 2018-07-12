using System.Threading.Tasks;

namespace TumblThree.Applications.Services
{
    public interface ILoginService
    {
        Task PerformTumblrLogin(string login, string password);

        void PerformTumblrLogout();

        Task PerformTumblrTFALogin(string login, string tumblrTFAAuthCode);

        bool CheckIfTumblrTFANeeded();

        bool CheckIfLoggedInAsync();

        Task<string> GetTumblrUsername();
    }
}