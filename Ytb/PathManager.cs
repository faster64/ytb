namespace Ytb
{
    public class PathManager
    {
        public static string ConfigPath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "cau-hinh");
        public static string ConfigFilePath => Path.Combine(ConfigPath, "file-cau-hinh.txt");

        public static string ChannelsPath => Path.Combine(Directory.GetCurrentDirectory(), "IO", "kenh-goc");
        public static string ChannelsFileHandlePath => Path.Combine(ChannelsPath, "ten-kenh.txt");
        public static string ChannelsFileDurationPath => Path.Combine(ChannelsPath, "cau-hinh-thoi-luong-video.txt");


        public static string InputPath => Path.Combine(Directory.GetCurrentDirectory(), "IO");

        public static string InputLineBackgroundPath => Path.Combine(InputPath, "LINE", "video-nen");
        public static string InputLineOriginVideoPath => Path.Combine(InputPath, "LINE", "video-goc");
        public static string InputLineFileDownloadPath => Path.Combine(InputPath, "LINE", "link-tai-video.txt");
        public static string InputLineFileChromaKeyPath => Path.Combine(InputPath, "LINE", "chroma-key.txt");
        public static string OutputLinePath => Path.Combine(InputPath, "LINE", "video-dang");


        public static string InputOlderBackgroundPath => Path.Combine(InputPath, "nguoi-gia", "video-nen");
        public static string InputOlderOriginVideoPath => Path.Combine(InputPath, "nguoi-gia", "video-goc");
        public static string InputOlderFileDownloadPath => Path.Combine(InputPath, "nguoi-gia", "link-tai-video.txt");
        public static string OutputOlderPath => Path.Combine(InputPath, "nguoi-gia", "video-dang");
    }
}
