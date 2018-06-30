using System.Threading.Tasks;

namespace TumblThree.Applications.Services
{
    public interface ILoginService
    {
        Task PerformTumblrLogin(string login, string password);

        bool CheckIfLoggedIn();

        Task<string> GetTumblrUsername();
    }
}