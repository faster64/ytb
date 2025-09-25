using Newtonsoft.Json;
using Ytb.Models;

namespace Ytb.Services
{
    public class ConfigService
    {
        public Config GetConfig()
        {
            var json = File.ReadAllText(PathManager.ConfigFilePath);
            return JsonConvert.DeserializeObject<Config>(json);
        }

        public void SaveConfig(Config config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(PathManager.ConfigFilePath, json);
        }

        public string GetApiKey()
        {
            return GetConfig().ApiKey;
        }

        public void SetApiKey(string apiKey)
        {
            var config = GetConfig();
            config.ApiKey = apiKey;
            SaveConfig(config);
        }
    }
}
