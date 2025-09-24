using System.ComponentModel;

namespace Ytb.Enums
{
    public enum OptionEnum
    {
        [Description("Cập nhật API Key")]
        UpdateAPIKey = 10,

        [Description("Lấy danh sách video urls từ channel")]
        GetVideoUrlsFromChannel = 1,

        [Description("Tải video")]
        DownloadVideo = 2,

        [Description("Thêm STT vào tên video")]
        AddPrefixToVideo = 3,

        [Description("Xóa STT trong tên video")]
        RemovePrefixToVideo = 4,
    }
}
