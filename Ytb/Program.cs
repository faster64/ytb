using Ytb;
using Ytb.Enums;
using Ytb.Extensions;
using Ytb.Services;

StartupService.Initialize();

var options = Enum.GetValues<OptionEnum>().OrderBy(x => (int) x).ToList();
var videoService = new VideoService();
// await videoService.CutVideoAsync(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(20));
// await videoService.OverlayImageAsync();

var choice = SelectOption();

Console.Clear();
switch (choice)
{
    case OptionEnum.UpdateAPIKey:
        if (!File.Exists(PathManager.ConfigFileApiKeyPath))
        {
            File.Create(PathManager.ConfigFileApiKeyPath).Close();
        }

        var currentApiKey = ChannelService.GetApiKeyAllowEmpty();
        var newApiKey = "";

        Console.WriteLine("API Key hiện tại: " + (string.IsNullOrEmpty(currentApiKey) ? "Chưa có" : currentApiKey));
        while (string.IsNullOrWhiteSpace(newApiKey))
        {
            Console.Write("Nhập API Key mới: ");
            newApiKey = Console.ReadLine() ?? "";
        }

        await File.WriteAllTextAsync(PathManager.ConfigFileApiKeyPath, newApiKey.Trim());
        Console.WriteLine("Cập nhật API Key thành công.");
        break;

    case OptionEnum.GetVideoUrlsFromChannel:
        await GetVideoUrlsFromChannelAsync();
        break;

    case OptionEnum.DownloadVideo:
        var downloader = new DownloadService();
        await downloader.DownloadVideosAsync();
        break;

    case OptionEnum.AddPrefixToVideo:
        VideoService.AddPrefix();
        Console.WriteLine("Thêm STT thành công");
        break;

    case OptionEnum.RemovePrefixToVideo:
        VideoService.RemovePrefix();
        Console.WriteLine("Xóa STT thành công");
        break;
}


Console.WriteLine();
Console.WriteLine();
Console.WriteLine("Press any key to close this console!");
Console.ReadKey();

OptionEnum SelectOption()
{
    Console.WriteLine("Quẹo lựa, quẹo lựa: ");
    foreach (var option in options)
    {
        Console.WriteLine((int)option + ". " + option.GetDescription());
    }
    Console.WriteLine();
    Console.WriteLine();

    Console.Write("Nhập lựa chọn: ");
    var result = Console.ReadLine();
    var validChoice = int.TryParse(result, out var choice) && options.Exists(x => (int)x == choice);

    while (!validChoice)
    {
        validChoice = int.TryParse(result, out choice) && options.Exists(x => (int)x == choice);
        if (!validChoice)
        {
            Console.Write("Lựa chọn không hợp lệ, chọn lại nào: ");
            result = Console.ReadLine();
        }
    }

    return (OptionEnum)choice;
}

async Task GetVideoUrlsFromChannelAsync()
{
    var channelService = new ChannelService();
    var channelHandle = "";

    Console.WriteLine("Kênh có chứa ký tự ngoài English Alphabet và số không?");
    Console.WriteLine("1. Có");
    Console.WriteLine("2. Không");
    Console.Write("Nhập lựa chọn: ");

    var specialCharChoice = Console.ReadLine();

    Console.Clear();
    if (specialCharChoice == "1")
    {
        var path = PathManager.ChannelsFileHandlePath;
        Console.WriteLine($"Hệ thống sẽ lấy channel handle trong {path}. Nhấn Enter để xác nhận?");
        Console.ReadLine();

        if (!File.Exists(path))
        {
            Console.WriteLine($"Không tìm thấy file {path}");
            return;
        }
        var lines = await File.ReadAllLinesAsync(path);
        channelHandle = lines.FirstOrDefault() ?? "";

        if (string.IsNullOrWhiteSpace(channelHandle))
        {
            Console.WriteLine($"File {path} không có channel handle hợp lệ");
            return;
        }
        await channelService.GetVideoUrlsAsync(channelHandle);
    }
    else
    {
        while (string.IsNullOrWhiteSpace(channelHandle))
        {
            Console.Write("Nhập channel handle (ví dụ: @line4091): ");
            channelHandle = Console.ReadLine() ?? "";
        }
        await channelService.GetVideoUrlsAsync(channelHandle);
    }
}

