using System;
using System.ComponentModel;
using System.Resources;

namespace TumblThree.Domain.Attributes
{
    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private readonly ResourceManager resourceManager;
        private readonly string resourceKey;

        public LocalizedDescriptionAttribute(string resourceKey, Type resourceType)
        {
            this.resourceManager = new ResourceManager(resourceType);
            this.resourceKey = resourceKey;
        }

        public override string Description
        {
            get
            {
                string description = resourceManager.GetString(resourceKey);
                return string.IsNullOrWhiteSpace(description) ? $"[[{resourceKey}]]" : description;
            }
        }
    }
}
