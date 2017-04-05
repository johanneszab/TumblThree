using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Waf.Applications;

using TumblThree.Applications.Services;

namespace TumblThree.Presentation.Services
{
    [Export(typeof(IEnvironmentService))]
    internal class EnvironmentService : IEnvironmentService
    {
        private readonly Lazy<string> appSettingsPath;
        private readonly Lazy<string> profilePath;
        private readonly Lazy<IReadOnlyList<string>> queueList;

        public EnvironmentService()
        {
            queueList = new Lazy<IReadOnlyList<string>>(() => Environment.GetCommandLineArgs().Skip(1).ToArray());
            profilePath = new Lazy<string>(() =>
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationInfo.Company,
                    ApplicationInfo.ProductName, "ProfileOptimization"));
            appSettingsPath = new Lazy<string>(() =>
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationInfo.Company,
                    ApplicationInfo.ProductName, "Settings"));
        }

        public string ProfilePath
        {
            get { return profilePath.Value; }
        }

        public string AppSettingsPath
        {
            get { return appSettingsPath.Value; }
        }
    }
}
