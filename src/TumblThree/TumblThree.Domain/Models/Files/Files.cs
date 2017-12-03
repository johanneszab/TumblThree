using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Waf.Foundation;
using System.Xml;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class Files : Model, IFiles
    {
        [DataMember(Name="Links")]
        protected List<string> links;

        private object lockObjectProgress = new object();
        private object lockObjectDb = new object();

        public Files()
        {
        }

        public Files(string name, string location)
        {
            Name = name;
            Location = location;
            Version = "1";
            links = new List<string>();
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Location { get; set; }

        [DataMember]
        public BlogTypes BlogType { get; set; }

        [DataMember]
        public string Version { get; set; }

        public IList<string> Links
        {
            get { return links; }
            protected set { }
        }

        public void AddFileToDb(string fileName)
        {
            lock (lockObjectProgress)
            {
                Links.Add(fileName);
            }
        }

        public virtual bool CheckIfFileExistsInDB(string url)
        {
            string fileName = url.Split('/').Last();
            Monitor.Enter(lockObjectDb);
            if (Links.Contains(fileName))
            {
                Monitor.Exit(lockObjectDb);
                return true;
            }
            Monitor.Exit(lockObjectDb);
            return false;
        }

        public IFiles Load(string fileLocation)
        {
            try
            {
                using (var stream = new FileStream(fileLocation,
                    FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var serializer = new DataContractJsonSerializer(GetType());
                    var file = (Files)serializer.ReadObject(stream);
                    file.Location = Path.Combine((Directory.GetParent(fileLocation).FullName));
                    return file;
                }
            }
            catch (SerializationException ex)
            {
                ex.Data.Add("Filename", fileLocation);
                throw;
            }
        }

        public bool Save()
        {
            string currentIndex = Path.Combine(Location, Name + "_files." + BlogType);
            string newIndex = Path.Combine(Location, Name + "_files." + BlogType + ".new");
            string backupIndex = Path.Combine(Location, Name + "_files." + BlogType + ".bak");

            try
            {
                if (File.Exists(currentIndex))
                {
                    using (var stream = new FileStream(newIndex, FileMode.Create, FileAccess.Write))
                    {
                        using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                            stream, Encoding.UTF8, true, true, "  "))
                        {
                            var serializer = new DataContractJsonSerializer(GetType());
                            serializer.WriteObject(writer, this);
                            writer.Flush();
                        }
                    }
                    File.Replace(newIndex, currentIndex, backupIndex, true);
                    File.Delete(backupIndex);
                }
                else
                {
                    using (var stream = new FileStream(currentIndex, FileMode.Create, FileAccess.Write))
                    {
                        using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                            stream, Encoding.UTF8, true, true, "  "))
                        {
                            var serializer = new DataContractJsonSerializer(GetType());
                            serializer.WriteObject(writer, this);
                            writer.Flush();
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Files:Save: {0}", ex);
                throw;
            }
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            lockObjectDb = new object();
            lockObjectProgress = new object();
        }
    }
}
