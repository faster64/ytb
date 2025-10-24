using Newtonsoft.Json;
using Ytb.Models;

namespace Ytb.Services
{
    public class ConfigService
    {
        public static Config GetConfig()
        {
            var json = File.ReadAllText(PathManager.ConfigFilePath);
            return JsonConvert.DeserializeObject<Config>(json);
        }

        public static void SaveConfig(Config config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(PathManager.ConfigFilePath, json);
        }

        public static string GetApiKey()
        {
            return GetConfig().ApiKey;
        }

        public static void SetApiKey(string apiKey)
        {
            var config = GetConfig();
            config.ApiKey = apiKey;
            SaveConfig(config);
        }
    }
}
