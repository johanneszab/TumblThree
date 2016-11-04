namespace System.Waf.Applications
{
    /// <summary>
    /// Represents a view
    /// </summary>
    public interface IView
    {
        /// <summary>
        /// Gets or sets the data context of the view.
        /// </summary>
        object DataContext { get; set; }
    }
}
