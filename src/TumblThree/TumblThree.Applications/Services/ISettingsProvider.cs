using System;

namespace TumblThree.Applications.Services
{
    internal interface ISettingsProvider
    {
        T LoadSettings<T>(string fileName) where T : class, new();

        void SaveSettings(string fileName, object settings);
    }
}
