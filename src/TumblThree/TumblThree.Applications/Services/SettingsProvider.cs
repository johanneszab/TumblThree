using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace TumblThree.Applications.Services
{
    [Export(typeof(ISettingsProvider))]
    internal class SettingsProvider : ISettingsProvider
    {
        private static readonly Type[] knownTypes = new[] { typeof(string[]) };

        public T LoadSettings<T>(string fileName) where T : class, new()
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("String must not be null or empty.", nameof(fileName));
            }

            if (!Path.IsPathRooted(fileName))
            {
                throw new ArgumentException("Invalid path. The path must be rooted.", nameof(fileName));
            }

            if (File.Exists(fileName))
            {
                using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(T), knownTypes);
                    return (T)serializer.ReadObject(stream) ?? new T();
                }
            }

            return new T();
        }

        public void SaveSettings(string fileName, object settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("String must not be null or empty.", nameof(fileName));
            }

            if (!Path.IsPathRooted(fileName))
            {
                throw new ArgumentException("Invalid path. The path must be rooted.", nameof(fileName));
            }

            string directory = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                    stream, Encoding.UTF8, true, true, "  "))
                {
                    var serializer = new DataContractJsonSerializer(settings.GetType(), knownTypes);
                    serializer.WriteObject(writer, settings);
                    writer.Flush();
                }
            }
        }
    }
}
