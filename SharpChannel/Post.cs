using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SharpChannel
{
    public class Post
    {
        [JsonProperty("no")]
        public int ID { get; set; }

        [JsonProperty("com")]
        public string RawComment { get; set; }

        public string Comment 
        { 
            get
            {
                if (string.IsNullOrWhiteSpace(RawComment))
                    return "";

                return Regex.Replace(HttpUtility.HtmlDecode(RawComment.Replace("<br>", "\n")), "<.*?>", String.Empty);
            }
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("trip")]
        public string Tripcode { get; set; }

        [JsonProperty("tim")]
        public ulong FileName { get; set; }

        [JsonProperty("ext")]
        public string Extension { get; set; }

        public string FileUrl
        {
            get
            {
                if (FileName == null ||
                    string.IsNullOrWhiteSpace(Extension))
                    return null;

                return string.Format("http://i.4cdn.org/{0}/{1}{2}", Parent.Parent.Name, FileName, Extension);
            }
        }

        public Thread Parent { get; set; }
        public bool Alive { get; internal set; }
        public OnPostDeleted Deleted;

        public Post()
        {

        }

        internal void Removal()
        {
            if (Deleted != null)
                Deleted(this);

            Alive = false;
        }
    }
}
