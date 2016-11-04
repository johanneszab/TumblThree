using System.Windows;
using System.ComponentModel;

namespace System.Waf
{
    /// <summary>
    /// Configuration settings for the WPF Application Framework (WAF).
    /// </summary>
    public static class WafConfiguration
    {
        private static readonly bool isInDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
#if (DEBUG)
        private static bool debug = true;
#else
        private static bool debug = false;
#endif


        /// <summary>
        /// Gets a value indicating whether the code is running in design mode.
        /// </summary>
        /// <value><c>true</c> if the code is running in design mode; otherwise, <c>false</c>.</value>
        public static bool IsInDesignMode { get { return isInDesignMode; } }

        /// <summary>
        /// Obsolete: Gets or sets a value indicating whether WAF should run in Debug mode.
        /// </summary>
        /// <remarks>
        /// The Debug mode helps to find errors in the application but it might reduce the performance.
        /// </remarks>
        [Obsolete("This property is not used anymore. Please remove the code that sets this property.")]
        public static bool Debug
        {
            get { return debug; }
            set { debug = value; }
        }
    }
}
