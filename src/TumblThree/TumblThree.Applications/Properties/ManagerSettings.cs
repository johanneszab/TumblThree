using System.Runtime.Serialization;

namespace TumblThree.Applications.Properties
{
    [DataContract]
    public sealed class ManagerSettings : IExtensibleDataObject
    {
        public ManagerSettings()
        {
        }

        ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }
    }
}
