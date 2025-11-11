using System.ComponentModel;
using Ytb.Runtimes.Filters;

namespace Ytb.Enums
{
    public enum OptionEnum
    {
        //[Description("Cập nhật API Key")]
        //UpdateAPIKey = 10,

        [Description("Lấy danh sách link video từ kênh")]
        GetVideoUrlsFromChannel = 1,

        [Description("Cắt video")]
        CutVideo,

        [Description("Cập nhật yt-dlp.exe"), BreakLine(1)]
        UpdateYtDlp,

        [Description("[LINE]: Tải video")]
        DownloadLineVideo = 20,

        [Description("[LINE]: Render video")]
        RenderLineVideos,

        [Description("[LINE]: Tạo video nền từ ảnh")]
        CreateLineVideoFromImage,

        [Description("[LINE]: Kéo dài video nền")]
        ExtendLineBackgroundVideo,

        [Description("[LINE]: Đổi màu thumbnail")]
        ChangeThumbnailColor,

        [Description("[LINE]: Thêm STT vào tên video")]
        AddPrefixToVideo,

        [Description("[LINE]: Xóa STT trong tên video"), BreakLine(1)]
        RemovePrefixToVideo,

        [Description("[NGƯỜI GIÀ]: Tải video")]
        DownloadOlderVideo,

        [Description("[NGƯỜI GIÀ]: Render video ")]
        RenderOlderVideos,

        [Description("[NGƯỜI GIÀ]: Tạo video nền từ ảnh")]
        CreateOlderVideoFromImage,

        [Description("[NGƯỜI GIÀ]: Kéo dài video nền"), BreakLine(1)]
        ExtendOlderBackgroundVideo,
    }
}
