using System.ComponentModel;

namespace Ytb.Enums
{
    public enum OptionEnum
    {
        //[Description("Cập nhật API Key")]
        //UpdateAPIKey = 10,

        [Description("Lấy danh sách link video từ kênh")]
        GetVideoUrlsFromChannel = 1,

        [Description("     LINE: Tải video")]
        DownloadLineVideo,

        [Description("     LINE: Render video")]
        RenderLineVideos,

        [Description("     LINE: Tạo video nền từ ảnh")]
        CreateLineVideoFromImage,

        [Description("     LINE: Kéo dài video nền")]
        ExtendLineBackgroundVideo,

        [Description("NGƯỜI GIÀ: Tải video")]
        DownloadOlderVideo,

        [Description("NGƯỜI GIÀ: Render video ")]
        RenderOlderVideos,

        [Description("NGƯỜI GIÀ: Tạo video nền từ ảnh")]
        CreateOlderVideoFromImage,

        [Description("NGƯỜI GIÀ: Kéo dài video nền")]
        ExtendOlderBackgroundVideo,

        [Description("Cắt video")]
        CutVideo = 20,

        //[Description("Thêm STT vào tên video")]
        //AddPrefixToVideo = 21,

        //[Description("Xóa STT trong tên video")]
        //RemovePrefixToVideo = 22,

        [Description("Cập nhật yt-dlp.exe")]
        UpdateYtDlp = 80
    }
}
