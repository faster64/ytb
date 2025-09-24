namespace Ytb
{
    public class PathManager
    {
        public static string ConfigPath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "configs");
        public static string ConfigFileApiKeyPath => Path.Combine(ConfigPath, "api_key.txt");

        public static string ChannelsPath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "channels");
        public static string ChannelsFileHandlePath => Path.Combine(ChannelsPath, "channel_handle.txt");

        public static string InputPath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "inputs");
        public static string InputBackgroundPath => Path.Combine(InputPath, "backgrounds");
        public static string InputOriginVideoPath => Path.Combine(InputPath, "origin-videos");
        public static string InputFileDownloadPath => Path.Combine(InputPath, "download_urls.txt");

        public static string OutputsPath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "outputs");
    }
}
