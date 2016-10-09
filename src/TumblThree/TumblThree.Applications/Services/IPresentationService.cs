namespace TumblThree.Applications.Services
{
    /// <summary>
    /// Service for initializing the presentation layer. These services are called before any ModuleController is initialized.
    /// </summary>
    /// <remarks>
    /// This service can be used to initialize the culture settings (must be done before the first view is created) 
    /// or to register resource dictionaries.
    /// </remarks>
    public interface IPresentationService
    {
        /// <summary>
        /// Initializes the service.
        /// </summary>
        void Initialize();
    }
}
