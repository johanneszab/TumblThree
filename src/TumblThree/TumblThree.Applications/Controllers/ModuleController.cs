using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Windows.Threading;

using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export(typeof(IModuleController)), Export]
    internal class ModuleController : IModuleController
    {
        private const string appSettingsFileName = "Settings.json";
        private const string managerSettingsFileName = "Manager.json";
        private const string queueSettingsFileName = "Queuelist.json";
        private const string cookiesFileName = "Cookies.json";
        private readonly Lazy<CrawlerController> crawlerController;
        private readonly Lazy<DetailsController> detailsController;
        private readonly IEnvironmentService environmentService;
        private readonly IConfirmTumblrPrivacyConsent confirmTumblrPrivacyConsent;
        private readonly Lazy<ManagerController> managerController;
        private readonly Lazy<QueueController> queueController;
        private readonly QueueManager queueManager;
        private readonly ISettingsProvider settingsProvider;
        private readonly ISharedCookieService cookieService;

        private readonly Lazy<ShellService> shellService;
        private readonly Lazy<ShellViewModel> shellViewModel;
        private AppSettings appSettings;
        private ManagerSettings managerSettings;
        private QueueSettings queueSettings;
        private List<Cookie> cookieList;

        [ImportingConstructor]
        public ModuleController(Lazy<ShellService> shellService, IEnvironmentService environmentService,
            IConfirmTumblrPrivacyConsent confirmTumblrPrivacyConsent, ISettingsProvider settingsProvider,
            ISharedCookieService cookieService, Lazy<ManagerController> managerController, Lazy<QueueController> queueController,
            Lazy<DetailsController> detailsController, Lazy<CrawlerController> crawlerController,
            Lazy<ShellViewModel> shellViewModel)
        {
            this.shellService = shellService;
            this.environmentService = environmentService;
            this.confirmTumblrPrivacyConsent = confirmTumblrPrivacyConsent;
            this.settingsProvider = settingsProvider;
            this.cookieService = cookieService;
            this.detailsController = detailsController;
            this.managerController = managerController;
            this.queueController = queueController;
            this.crawlerController = crawlerController;
            this.shellViewModel = shellViewModel;
            queueManager = new QueueManager();
        }

        private ShellService ShellService
        {
            get { return shellService.Value; }
        }

        private ManagerController ManagerController
        {
            get { return managerController.Value; }
        }

        private QueueController QueueController
        {
            get { return queueController.Value; }
        }

        private DetailsController DetailsController
        {
            get { return detailsController.Value; }
        }

        private CrawlerController CrawlerController
        {
            get { return crawlerController.Value; }
        }

        private ShellViewModel ShellViewModel
        {
            get { return shellViewModel.Value; }
        }

        public void Initialize()
        {
            if (CheckIfPortableMode(appSettingsFileName))
            {
                appSettings = LoadSettings<AppSettings>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appSettingsFileName));
                queueSettings = LoadSettings<QueueSettings>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, queueSettingsFileName));
                managerSettings = LoadSettings<ManagerSettings>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, managerSettingsFileName));
                cookieList = LoadSettings<List<Cookie>>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cookiesFileName));
            }
            else
            {
                appSettings = LoadSettings<AppSettings>(Path.Combine(environmentService.AppSettingsPath, appSettingsFileName));
                queueSettings = LoadSettings<QueueSettings>(Path.Combine(environmentService.AppSettingsPath, queueSettingsFileName));
                managerSettings = LoadSettings<ManagerSettings>(Path.Combine(environmentService.AppSettingsPath, managerSettingsFileName));
                cookieList = LoadSettings<List<Cookie>>(Path.Combine(environmentService.AppSettingsPath, cookiesFileName));
            }

            ShellService.Settings = appSettings;
            ShellService.ShowErrorAction = ShellViewModel.ShowError;
            ShellService.ShowDetailsViewAction = ShowDetailsView;
            ShellService.ShowQueueViewAction = ShowQueueView;
            ShellService.UpdateDetailsViewAction = UpdateDetailsView;
            ShellService.InitializeOAuthManager();

            ManagerController.QueueManager = queueManager;
            ManagerController.ManagerSettings = managerSettings;
            ManagerController.BlogManagerFinishedLoadingLibrary += OnBlogManagerFinishedLoadingLibrary;
            Task managerControllerInit = ManagerController.Initialize();
            QueueController.QueueSettings = queueSettings;
            QueueController.QueueManager = queueManager;
            QueueController.Initialize();
            DetailsController.QueueManager = queueManager;
            DetailsController.Initialize();
            CrawlerController.QueueManager = queueManager;
            CrawlerController.Initialize();
            cookieService.SetUriCookie(cookieList);
        }

        public async void Run()
        {
            ShellViewModel.IsQueueViewVisible = true;
            ShellViewModel.Show();

            // Let the UI to initialize first before loading the queuelist.
            await Dispatcher.CurrentDispatcher.InvokeAsync(ManagerController.RestoreColumn, DispatcherPriority.ApplicationIdle);
            await Dispatcher.CurrentDispatcher.InvokeAsync(QueueController.Run, DispatcherPriority.ApplicationIdle);
            await confirmTumblrPrivacyConsent.ConfirmPrivacyConsent();
        }

        public void Shutdown()
        {
            DetailsController.Shutdown();
            QueueController.Shutdown();
            ManagerController.Shutdown();
            CrawlerController.Shutdown();

            if (appSettings.PortableMode)
            {
                SaveSettings(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appSettingsFileName), appSettings);
                SaveSettings(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, queueSettingsFileName), queueSettings);
                SaveSettings(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, managerSettingsFileName), managerSettings);
                SaveSettings(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cookiesFileName), new List<Cookie>(cookieService.GetAllCookies()));
            }
            else
            {
                SaveSettings(Path.Combine(environmentService.AppSettingsPath, appSettingsFileName), appSettings);
                SaveSettings(Path.Combine(environmentService.AppSettingsPath, queueSettingsFileName), queueSettings);
                SaveSettings(Path.Combine(environmentService.AppSettingsPath, managerSettingsFileName), managerSettings);
                SaveSettings(Path.Combine(environmentService.AppSettingsPath, cookiesFileName), new List<Cookie>(cookieService.GetAllCookies()));
            }
        }

        private void OnBlogManagerFinishedLoadingLibrary(object sender, EventArgs e)
        {
            QueueController.LoadQueue();
        }

        private static bool CheckIfPortableMode(string fileName)
        {
            return File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName));
        }

        private T LoadSettings<T>(string fileName) where T : class, new()
        {
            try
            {
                return settingsProvider.LoadSettings<T>(fileName);
            }
            catch (Exception ex)
            {
                Logger.Error("Could not read the settings file: {0}", ex);
                return new T();
            }
        }

        private void SaveSettings(string fileName, object settings)
        {
            try
            {
                settingsProvider.SaveSettings(fileName, settings);
            }
            catch (Exception ex)
            {
                Logger.Error("Could not save the settings file: {0}", ex);
            }
        }

        private void ShowDetailsView()
        {
            ShellViewModel.IsDetailsViewVisible = true;
        }

        private void ShowQueueView()
        {
            ShellViewModel.IsQueueViewVisible = true;
        }

        private void UpdateDetailsView()
        {
            if (!ShellViewModel.IsQueueViewVisible)
                ShellViewModel.IsDetailsViewVisible = true;
        }
    }
}
