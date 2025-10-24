using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Ytb.Models;

namespace Ytb.Services
{
    public class StartupService
    {
        public static async Task InitializeAsync()
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

            if (!File.Exists(VideoService._ffmpegPath))
            {
                ConsoleService.WriteLineError("Chưa cài đặt ffmpeg. Vui lòng kiểm tra lại");
                throw new Exception();
            }

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
                            ApiKey = "AIzaSyDZTsPGvG0u5du3t7YGueGgnNi7IiulMus",
                            AutoGetNewYtDlp = true,
                            AudioConfig = new AudioRenderConfig
                            {
                                LastRenderIndex = 0,
                                NumberOfChannels = 5,
                                NumberOfVideosPerChannelDaily = 5,
                                CCT = 1,
                                CropValue = "in_w:190:0:650",
                                OverlayValue = "(main_w-overlay_w)/2:490"
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

            if (ConfigService.GetConfig().AutoGetNewYtDlp)
            {
                await UpdateYtDlpAsync();
            }
        }

        private static async Task UpdateYtDlpAsync()
        {
            const string ytDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
            const string ytDlpFile = "yt-dlp.exe";

            try
            {
                var sw = Stopwatch.StartNew();

                ConsoleService.WriteLineSuccess("Đang tải yt-dlp.exe mới nhất...");

                using var httpClient = new HttpClient();
                var bytes = await httpClient.GetByteArrayAsync(ytDlpUrl);

                if (File.Exists(ytDlpFile))
                {
                    ConsoleService.WriteLineSuccess("Đang xóa yt-dlp.exe cũ...");
                    File.Delete(ytDlpFile);
                }

                await File.WriteAllBytesAsync(ytDlpFile, bytes);

                sw.Stop();
                ConsoleService.WriteLineSuccess($"Cập nhật yt-dlp.exe thành công sau {sw.Elapsed.TotalSeconds:N0}s!");
                Console.WriteLine();
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                ConsoleService.WriteLineError($"Lỗi khi cập nhật yt-dlp.exe: {ex.Message}");
            }
        }
    }
}
