using System.ComponentModel;

namespace Ytb.Enums
{
    public enum OptionEnum
    {
        [Description("Cập nhật API Key")]
        UpdateAPIKey = 1,

        [Description("Lấy danh sách video urls từ channel")]
        GetVideoUrlsFromChannel = 2,

        [Description("Tải video")]
        DownloadVideo = 3,

        [Description("Thêm STT vào tên video")]
        AddPrefixToVideo = 4,

        [Description("Xóa STT trong tên video")]
        RemovePrefixToVideo = 5,
    }
}
