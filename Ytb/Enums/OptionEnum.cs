using System.ComponentModel;

namespace Ytb.Enums
{
    public enum OptionEnum
    {
        //[Description("Cập nhật API Key")]
        //UpdateAPIKey = 10,

        [Description("Lấy danh sách video urls từ channel")]
        GetVideoUrlsFromChannel = 1,

        [Description("Tải video từ urls")]
        DownloadVideo = 2,

        //[Description("Thêm STT vào tên video")]
        //AddPrefixToVideo = 3,

        //[Description("Xóa STT trong tên video")]
        //RemovePrefixToVideo = 4,

        [Description("Render")]
        RenderAudioVideos = 5,

        //[Description("Render LINE videos")]
        //RenderLineVideos = 6,

        [Description("Tạo video nền từ ảnh")]
        CreateVideoFromImage = 7,

        [Description("Cắt video")]
        CutAudioVideo = 8
    }
}
