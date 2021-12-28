using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSApp.Model;

namespace TSApp.StaticData
{
    public static class Settings
    {
        private static string Path = Environment.ExpandEnvironmentVariables(@"%HOMEPATH%\.tsapp");
        private static string FileExtension = ".settings.json";
        public static StoredParameters value { get; set; }

        static Settings()
        {
            value = new StoredParameters();
        }
        public static void Load()
        {
            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
                value.InitDefault();
                value.MustInitInvoke("Созданы параметры по-умолчанию");
                return;
            }

            string username = Environment.ExpandEnvironmentVariables("%USERNAME%");
            string filename = $"{Path}\\{username}{FileExtension}";
            try
            {
                using (var file = File.OpenText(filename))
                {
                    string s = file.ReadToEnd();
                    value = JsonConvert.DeserializeObject<StoredParameters>(s);
                }
            }
            catch (Exception ex)
            {
                value.InitDefault();
                value.MustInitInvoke(ex.Message);
            }
        }
        public static void Save()
        {
            string username = Environment.ExpandEnvironmentVariables("%USERNAME%");
            string filename = $"{Path}\\{username}.settings.json";
            using (StreamWriter file = File.CreateText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, value);
            }
        }
        public static void EraseFile()
        {
            string username = Environment.ExpandEnvironmentVariables("%USERNAME%");
            string filename = $"{Path}\\{username}.settings.json";
            File.Delete(filename);
        }
    }
}
