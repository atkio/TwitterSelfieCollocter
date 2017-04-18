using Newtonsoft.Json;
using System;
using System.IO;

namespace TwitterSelfieCollocter
{
    public class SelfieBotConfig
    {

        /// <summary>
        /// 推特API定义
        /// </summary>
        public struct TwitterDefine
        {
            public string AccessToken;
            public string AccessTokenSecret;
            public string ConsumerKey;
            public string ConsumerSecret;
        }

        public TwitterDefine Twitter { get; set; }


        public struct OneDriveDefine
        {
            public bool IsValue;
            public string RemoteRootID;
        }

        public OneDriveDefine onedrive {get;set;}

        /// <summary>
        /// 本地临时保存图片的位置
        /// </summary>
        public string PhotoTempPath = Path.Combine(Environment.CurrentDirectory, "TEMP");

        /// <summary>
        /// 永久保存照片的位置
        /// </summary>
        public string PhotoPath { get; set; }


        public const string Define = "Define.conf";

        public static SelfieBotConfig Instance
        {
            get
            {
                if (!File.Exists(Define))
                {
                    File.WriteAllText(Define,
                     JsonConvert.SerializeObject(new SelfieBotConfig(),
                     Formatting.Indented, new BoolConverter()));
                }

                string PhotoTempPath = Path.Combine(Environment.CurrentDirectory, "TEMP");
                if (!Directory.Exists(PhotoTempPath))
                {
                    Directory.CreateDirectory(PhotoTempPath);
                }

                if (_Instance == null)
                {
                    try
                    {
                        _Instance = JsonConvert.DeserializeObject<SelfieBotConfig>(
                            File.ReadAllText(Define), new BoolConverter());
                    }
                    catch
                    {
                        throw new IOException("Define file failed.");
                    }
                }
                return _Instance;
            }
        }

        private static SelfieBotConfig _Instance = null;

    }

    public class BoolConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((bool)value) ? 1 : 0);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.Value.ToString() == "1";
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(bool);
        }
    }
}
