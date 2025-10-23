using Newtonsoft.Json;
using System.Reflection;
using System.Text;
using Ytb.Models;

namespace Ytb.Services
{
    public class StartupService
    {
        public static void Initialize()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            var assembly = Assembly.GetExecutingAssembly();
            var rootPath = Path.GetDirectoryName(assembly.Location) ?? "";
            var binIndex = rootPath.IndexOf("\\bin\\", StringComparison.Ordinal);

            if (binIndex > 0)
            {
                rootPath = rootPath.Substring(0, binIndex);
            }

            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            Directory.SetCurrentDirectory(rootPath);

            var folderPaths = new List<string>
            {
                PathManager.ConfigPath,
                PathManager.ConfigPath,
                PathManager.ChannelsPath,
                PathManager.InputPath,
                PathManager.InputOriginVideoPath,
                PathManager.InputBackgroundPath,
                PathManager.OutputsPath,
            };

            var filePaths = new List<string>
            {
                PathManager.ConfigFilePath,
                PathManager.ChannelsFileHandlePath,
                PathManager.InputFileDownloadPath
            };

            foreach (var path in folderPaths)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            foreach (var path in filePaths)
            {
                if (!File.Exists(path))
                {
                    File.Create(path).Close();

                    if (path == PathManager.ConfigFilePath)
                    {
                        var config = new Config
                        {
                            ApiKey = "",
                            AudioConfig = new RenderConfig
                            {
                                LastRenderIndex = 0,
                                NumberOfChannels = 5,
                                NumberOfVideosPerChannelDaily = 5,
                                CCT = 1
                            },
                            LineConfig = new RenderConfig
                            {
                                LastRenderIndex = 0,
                                NumberOfChannels = 5,
                                NumberOfVideosPerChannelDaily = 5,
                                CCT = 1
                            }
                        };
                        File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
                    }
                }
            }

            new ConfigService().SetApiKey("AIzaSyDZTsPGvG0u5du3t7YGueGgnNi7IiulMus");
        }
    }
}
