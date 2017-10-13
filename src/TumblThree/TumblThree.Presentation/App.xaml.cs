using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Waf;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Threading;

using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Presentation.Properties;

namespace TumblThree.Presentation
{
    public partial class App : Application
    {
        private AggregateCatalog catalog;
        private CompositionContainer container;
        private IEnumerable<IModuleController> moduleControllers;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            InitializeCultures();
            System.Net.ServicePointManager.DefaultConnectionLimit = 400;
            // Trust all SSL hosts since tumblr.com messed up their ssl cert on amazon s3.
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateCertificate);

            catalog = new AggregateCatalog();
            // Add the WpfApplicationFramework assembly to the catalog
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(WafConfiguration).Assembly));
            // Add the TumblThree.Applications assembly
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(ShellViewModel).Assembly));
            // Add the TumblThree.Domain assembly
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(IBlog).Assembly));
            // Add the TumblThree.Presentation assembly
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(App).Assembly));

            container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection);
            var batch = new CompositionBatch();
            batch.AddExportedValue(container);
            container.Compose(batch);

            // Initialize all presentation services
            IEnumerable<IPresentationService> presentationServices = container.GetExportedValues<IPresentationService>();
            foreach (IPresentationService presentationService in presentationServices)
            {
                presentationService.Initialize();
            }

            // Initialize and run all module controllers
            moduleControllers = container.GetExportedValues<IModuleController>();
            foreach (IModuleController moduleController in moduleControllers)
            {
                moduleController.Initialize();
            }
            foreach (IModuleController moduleController in moduleControllers)
            {
                moduleController.Run();
            }

            Applications.QueueOnDispatcher.Initialize();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Shutdown the module controllers in reverse order
            foreach (IModuleController moduleController in moduleControllers.Reverse())
            {
                moduleController.Shutdown();
            }

            // Wait until all registered tasks are finished
            var shellService = container.GetExportedValue<IShellService>();
            Task[] tasksToWait = shellService.TasksToCompleteBeforeShutdown.ToArray();
            while (tasksToWait.Any(t => !t.IsCompleted && !t.IsCanceled && !t.IsFaulted))
            {
                DispatcherHelper.DoEvents();
            }

            // Dispose
            container.Dispose();
            catalog.Dispose();
            base.OnExit(e);
        }

        private static void InitializeCultures()
        {
            if (!string.IsNullOrEmpty(Settings.Default.Culture))
            {
                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(Settings.Default.Culture);
            }
            if (!string.IsNullOrEmpty(Settings.Default.UICulture))
            {
                CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(Settings.Default.UICulture);
            }
        }

        private static void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception, false);
        }

        private static void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception, e.IsTerminating);
        }

        private static void HandleException(Exception e, bool isTerminating)
        {
            if (e == null)
            {
                return;
            }

            Logger.Error(e.ToString());

            if (!isTerminating)
            {
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture,
                    Presentation.Properties.Resources.UnknownError, e.ToString()),
                    ApplicationInfo.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var comException = e.Exception as System.Runtime.InteropServices.COMException;

            if (comException != null && comException.ErrorCode == -2147221040)
            {
                e.Handled = true;
            }
        }

        static bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }
    }
}
