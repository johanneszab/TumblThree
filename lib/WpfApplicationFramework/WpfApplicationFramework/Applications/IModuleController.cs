using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Waf.Applications
{
    /// <summary>
    /// Interface for a module controller which is responsible for the module lifecycle.
    /// </summary>
    public interface IModuleController
    {
        /// <summary>
        /// Initializes the module controller.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Run the module controller.
        /// </summary>
        void Run();

        /// <summary>
        /// Shutdown the module controller.
        /// </summary>
        void Shutdown();
    }
}
