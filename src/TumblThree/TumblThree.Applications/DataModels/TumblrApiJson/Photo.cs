using System.Runtime.Serialization;

namespace TumblThree.Applications.DataModels.TumblrApiJson
{
    [DataContract]
    public class Photo
    {
        [DataMember(Name = "offset", EmitDefaultValue = false)]
        public string Offset { get; set; }

        [DataMember(Name = "caption", EmitDefaultValue = false)]
        public string Caption { get; set; }

        [DataMember(Name = "width", EmitDefaultValue = false)]
        public int Width { get; set; }

        [DataMember(Name = "height", EmitDefaultValue = false)]
        public int Height { get; set; }

        [DataMember(Name = "photo-url-1280", EmitDefaultValue = false)]
        public string PhotoUrl1280 { get; set; }

        [DataMember(Name = "photo-url-500", EmitDefaultValue = false)]
        public string PhotoUrl500 { get; set; }

        [DataMember(Name = "photo-url-400", EmitDefaultValue = false)]
        public string PhotoUrl400 { get; set; }

        [DataMember(Name = "photo-url-250", EmitDefaultValue = false)]
        public string PhotoUrl250 { get; set; }

        [DataMember(Name = "photo-url-100", EmitDefaultValue = false)]
        public string PhotoUrl100 { get; set; }

        [DataMember(Name = "photo-url-75", EmitDefaultValue = false)]
        public string PhotoUrl75 { get; set; }
    }
}