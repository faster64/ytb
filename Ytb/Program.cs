using FFmpegArgs.Cores.Enums;
using System.Diagnostics;
using Ytb;
using Ytb.Enums;
using Ytb.Extensions;
using Ytb.Services;

StartupService.Initialize();

var options = Enum.GetValues<OptionEnum>().OrderBy(x => (int)x).ToList();
var choice = SelectOption();
//var choice = OptionEnum.RenderAudioVideos;

switch (choice)
{
    case OptionEnum.UpdateAPIKey:
        var apiKey = "";
        while (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.Write("Nhập API Key mới: ");
            apiKey = Console.ReadLine() ?? "";
        }
        new ConfigService().SetApiKey(apiKey);

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
        Console.Write("Nhập path: ");
        var path = Console.ReadLine();

        new VideoService().AddPrefix(path);
        Console.WriteLine("Thêm STT thành công");
        break;

    case OptionEnum.RemovePrefixToVideo:
        Console.Write("Nhập path: ");
        var path2 = Console.ReadLine();

        new VideoService().RemovePrefix(path2);
        Console.WriteLine("Xóa STT thành công");
        break;

    case OptionEnum.RenderAudioVideos:
        await RenderAudioVideosAsync();
        break;

    case OptionEnum.RenderLineVideos:
        break;

    case OptionEnum.CreateVideoFromImage:
        Console.Write("Nhập path: ");
        var path3 = Console.ReadLine();

        await CreateVideosFromImagesAsync(path3);
        Console.WriteLine("Tao video thành công");
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

async Task RenderAudioVideosAsync()
{
    var sw = Stopwatch.StartNew();

    var config = new ConfigService().GetConfig().AudioConfig;
    var backgroundFolders = Directory.EnumerateDirectories(PathManager.InputBackgroundPath).ToList();
    var originVideos = Directory.EnumerateFiles(PathManager.InputOriginVideoPath, "*.mp4").ToList();
    var requiredVideoCount = config.NumberOfChannels * config.NumberOfVideosPerChannelDaily;

    if (originVideos.Count < requiredVideoCount)
    {
        Console.WriteLine($"Số video không đủ để render. Cần {requiredVideoCount} videos");
        return;
    }

    if (backgroundFolders.Count < config.NumberOfChannels)
    {
        Console.WriteLine($"{PathManager.InputBackgroundPath} không đủ {config.NumberOfChannels} thư mục background.");
        return;
    }

    foreach (var bgFolder in backgroundFolders)
    {
        var bgFiles = Directory.EnumerateFiles(bgFolder, "*.jpg").ToList();
        if (bgFiles.Count < config.NumberOfVideosPerChannelDaily)
        {
            Console.WriteLine($"Thư mục {bgFolder} không có đủ {config.NumberOfVideosPerChannelDaily} ảnh nền.");
            return;
        }
    }

    var currentIndex = config.LastRenderIndex + 1;
    var videoService = new VideoService();
    var random = new Random();

    foreach (var backgroundFolder in backgroundFolders)
    {
        var folder = backgroundFolder.Split(Path.DirectorySeparatorChar).Last();
        var folderPath = Path.Combine(PathManager.OutputsPath, folder);

        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, recursive: true);
        }
        Directory.CreateDirectory(folderPath);

        var videoPaths = originVideos.Take(config.NumberOfVideosPerChannelDaily).Skip(config.LastRenderIndex * config.NumberOfVideosPerChannelDaily).ToList();
        var backgroundPaths = Directory.EnumerateFiles(backgroundFolder, "*.jpg").ToList();
        var tasks = new List<Task>();

        var selectedBackgroundPaths = new List<string>();
        while (selectedBackgroundPaths.Count < config.NumberOfVideosPerChannelDaily)
        {
            var randomNumber = random.Next(0, backgroundPaths.Count - 1);
            while (selectedBackgroundPaths.Contains(backgroundPaths[randomNumber]))
            {
                randomNumber = random.Next(0, backgroundPaths.Count - 1);
            }

            selectedBackgroundPaths.Add(backgroundPaths[randomNumber]);
        }

        for (int i = 0; i < videoPaths.Count; i++)
        {
            var videoPath = videoPaths[i];
            var videoTitle = videoPath.Split(Path.DirectorySeparatorChar).Last();
            var outputVideo = Path.Combine(PathManager.OutputsPath, folder, videoTitle);

            await videoService.OverlayTextOnBackgroundAsync(videoPath, outputVideo, selectedBackgroundPaths[i]);
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    sw.Stop();
    Console.WriteLine($"Render took {sw.Elapsed.TotalSeconds}s.");
}

async Task CreateVideosFromImagesAsync(string path)
{
    for (int i = 1; i <= 500; i++)
    {
        var folderPath = $"D:\\Zutube\\cut-video-3000\\backgrounds\\{i}";
        if (!Directory.Exists(folderPath))
        {
            continue;
        }

        var sw = Stopwatch.StartNew();
        var images = Directory.EnumerateFiles(folderPath, "*.jpg").ToList();

        foreach (var imagePath in images)
        {
            Directory.CreateDirectory(folderPath);
            await new VideoService().CreateVideoFromImage(imagePath, Path.Combine(folderPath, imagePath.Split(Path.DirectorySeparatorChar).Last().Replace(".jpg", ".mp4")));
        }

        sw.Stop();
        Console.WriteLine($"Render took {sw.Elapsed.TotalSeconds}s.");
    }
}