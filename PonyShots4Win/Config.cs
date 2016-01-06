using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PonyShots4Win
{
    public class Config
    {
        public string UploadUrl { get; set; }
        public string BaseUrl { get; set; }
        public string Username { get; set; }
        public string ApiKey { get; set; }

        public void Save(string filePath)
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(this, new JsonSerializerSettings { Formatting = Formatting.Indented }));
        }

        public static Config Parse(string filePath)
        {
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(filePath));
        }

        public static Config Default()
        {
            return (Config)DefaultConfig.MemberwiseClone();
        }

        private static Config DefaultConfig = new Config { UploadUrl = "https://dashie.in/ps/upload", BaseUrl = "http://dash.sh/" };
    }
}
