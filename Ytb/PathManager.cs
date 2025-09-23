namespace Ytb
{
    public class PathManager
    {
        public static string ResourcesPath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "resources");
        public static string ResourcesFileApiKeyPath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "resources", "api_key.txt");

        public static string ChannelsPath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "channels");
        public static string ChannelsFileHandlePath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "channels", "channel_handle.txt");

        public static string DownloadsPath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "downloads");
        public static string DownloadInputPath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "downloads", "input");
        public static string DownloadFileUrlInputPath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "downloads", "input", "urls.txt");
        public static string DownloadOutputPath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "downloads", "output");
    }
}
