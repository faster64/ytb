using System.ComponentModel;

namespace Ytb.Enums
{
    public enum OptionEnum
    {
        //[Description("Cập nhật API Key")]
        //UpdateAPIKey = 10,

        [Description("Lấy danh sách link video từ kênh")]
        GetVideoUrlsFromChannel = 1,

        [Description("Tải video LINE")]
        DownloadLineVideo = 2,

        [Description("Tải video NGƯỜI GIÀ")]
        DownloadOlderVideo = 3,

        [Description("Render video NGƯỜI GIÀ")]
        RenderOlderVideos = 5,

        [Description("Render video LINE")]
        RenderLineVideos = 6,

        [Description("LINE: Tạo video nền từ ảnh")]
        CreateLineVideoFromImage = 7,

        [Description("NGƯỜI GIÀ: Tạo video nền từ ảnh")]
        CreateOlderVideoFromImage = 8,

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
