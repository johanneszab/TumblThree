using System.Reflection;
using System.IO;

namespace System.Waf.Applications
{
    /// <summary>
    /// This class provides information about the running application.
    /// </summary>
    public static class ApplicationInfo
    {
        private static readonly Lazy<string> productName = new Lazy<string>(GetProductName);
        private static readonly Lazy<string> version = new Lazy<string>(GetVersion);
        private static readonly Lazy<string> company = new Lazy<string>(GetCompany);
        private static readonly Lazy<string> copyright = new Lazy<string>(GetCopyright);
        private static readonly Lazy<string> applicationPath = new Lazy<string>(GetApplicationPath);


        /// <summary>
        /// Gets the product name of the application.
        /// </summary>
        public static string ProductName { get { return productName.Value; } }

        /// <summary>
        /// Gets the version number of the application.
        /// </summary>
        public static string Version { get { return version.Value; } }

        /// <summary>
        /// Gets the company of the application.
        /// </summary>
        public static string Company { get { return company.Value; } }
        
        /// <summary>
        /// Gets the copyright information of the application.
        /// </summary>
        public static string Copyright { get { return copyright.Value; } }

        /// <summary>
        /// Gets the path for the executable file that started the application, not including the executable name.
        /// </summary>
        public static string ApplicationPath { get { return applicationPath.Value; } }


        private static string GetProductName()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                AssemblyProductAttribute attribute = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(
                    entryAssembly, typeof(AssemblyProductAttribute)));
                return (attribute != null) ? attribute.Product : "";
            }
            return "";
        }

        private static string GetVersion()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                return entryAssembly.GetName().Version.ToString();
            }
            return "";
        }

        private static string GetCompany()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                AssemblyCompanyAttribute attribute = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(
                    entryAssembly, typeof(AssemblyCompanyAttribute)));
                return (attribute != null) ? attribute.Company : "";
            }
            return "";
        }

        private static string GetCopyright()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                AssemblyCopyrightAttribute attribute = (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(
                    entryAssembly, typeof(AssemblyCopyrightAttribute));
                return attribute != null ? attribute.Copyright : "";
            }
            return "";
        }

        private static string GetApplicationPath()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                return Path.GetDirectoryName(entryAssembly.Location);
            }
            return "";
        }
    }
}
