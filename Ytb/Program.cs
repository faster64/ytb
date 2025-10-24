using System.Diagnostics;
using Ytb;
using Ytb.Enums;
using Ytb.Extensions;
using Ytb.Services;

await StartupService.InitializeAsync();

var options = Enum.GetValues<OptionEnum>().OrderBy(x => (int)x).ToList();

await Main();
async Task Main()
{
    var choice = SelectOption();

    switch (choice)
    {
        //case OptionEnum.UpdateAPIKey:
        //    var apiKey = "";
        //    while (string.IsNullOrWhiteSpace(apiKey))
        //    {
        //        Console.Write("Nhập API Key mới: ");
        //        apiKey = Console.ReadLine() ?? "";
        //    }
        //    new ConfigService().SetApiKey(apiKey);

        //    Console.WriteLine("Cập nhật API Key thành công.");
        //    break;

        case OptionEnum.GetVideoUrlsFromChannel:
            await GetVideoUrlsFromChannelAsync();
            break;

        case OptionEnum.DownloadVideo:
            var downloader = new DownloadService();
            await downloader.DownloadVideosAsync();
            break;

        //case OptionEnum.AddPrefixToVideo:
        //    Console.Write("Nhập path: ");
        //    var path = Console.ReadLine();

        //    new VideoService().AddPrefix(path);
        //    Console.WriteLine("Thêm STT thành công");
        //    break;

        //case OptionEnum.RemovePrefixToVideo:
        //    Console.Write("Nhập path: ");
        //    var path2 = Console.ReadLine();

        //    new VideoService().RemovePrefix(path2);
        //    Console.WriteLine("Xóa STT thành công");
        //    break;

        case OptionEnum.RenderAudioVideos:
            await RenderAudioVideosAsync();
            break;

        //case OptionEnum.RenderLineVideos:
        //    break;

        case OptionEnum.CreateVideoFromImage:
            await CreateVideosFromImagesAsync();
            break;

        case OptionEnum.CutAudioVideo:
            await TrimVideoAudioAsync();
            break;
    }

    Console.WriteLine();
    Console.WriteLine();
    ConsoleService.WriteLineSuccess("----- Bấm nút bất kỳ để tiếp tục -----");
    Console.ReadKey();
    Console.Clear();
    await Main();
}

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
            ConsoleService.WriteLineError("Lựa chọn không hợp lệ, chọn lại nào: ");
            result = Console.ReadLine();
        }
    }

    return (OptionEnum)choice;
}

async Task GetVideoUrlsFromChannelAsync()
{
    var channelService = new ChannelService();
    var channelHandle = "";
    var path = PathManager.ChannelsFileHandlePath;

    Console.Clear();
    Console.WriteLine($"Lấy channel handle trong {path}");
    Console.WriteLine("1. Đồng ý");
    Console.WriteLine("2. Không, tự nhập");
    Console.Write("Nhập lựa chọn: ");

    var specialCharChoice = Console.ReadLine();

    Console.Clear();
    if (specialCharChoice == "1")
    {
        if (!File.Exists(path))
        {
            ConsoleService.WriteLineError($"Không tìm thấy file {path}");
            return;
        }
        var lines = await File.ReadAllLinesAsync(path);
        channelHandle = lines.FirstOrDefault() ?? "";

        if (string.IsNullOrWhiteSpace(channelHandle))
        {
            ConsoleService.WriteLineError($"File {path} không có channel handle hợp lệ");
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
        ConsoleService.WriteLineError($"Số video không đủ để render. Cần {requiredVideoCount} videos");
        return;
    }

    if (backgroundFolders.Count < config.NumberOfChannels)
    {
        ConsoleService.WriteLineError($"{PathManager.InputBackgroundPath} không đủ {config.NumberOfChannels} thư mục background.");
        return;
    }

    foreach (var bgFolder in backgroundFolders)
    {
        var bgFiles = Directory.EnumerateFiles(bgFolder, "*.jpg").ToList();
        if (bgFiles.Count < config.NumberOfVideosPerChannelDaily)
        {
            ConsoleService.WriteLineError($"Thư mục {bgFolder} không có đủ {config.NumberOfVideosPerChannelDaily} ảnh nền.");
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
    ConsoleService.WriteLineSuccess($"Render thành công sau {sw.Elapsed.TotalMinutes}m.");
}

async Task CreateVideosFromImagesAsync()
{
    var sw = Stopwatch.StartNew();
    for (int i = 1; i <= 500; i++)
    {
        var folderPath = Path.Combine(PathManager.InputBackgroundPath, i.ToString());
        if (!Directory.Exists(folderPath))
        {
            continue;
        }

        var images = Directory.EnumerateFiles(folderPath, "*.jpg").ToList();

        foreach (var imagePath in images)
        {
            var outputVideo = Path.Combine(folderPath, imagePath.Split(Path.DirectorySeparatorChar).Last().Replace(".jpg", ".mp4"));
            await new VideoService().CreateVideoFromImage(imagePath, outputVideo);
        }
    }

    sw.Stop();
    ConsoleService.WriteLineSuccess($"Hoàn thành sau: {sw.Elapsed.TotalSeconds}s");
}

async Task TrimVideoAudioAsync()
{
    var folderPath = PathManager.InputOriginVideoPath;
    var sw = Stopwatch.StartNew();
    var videos = Directory.EnumerateFiles(folderPath, "*.mp4").ToList();

    foreach (var videoPath in videos)
    {
        var videoTitle = videoPath.Split(Path.DirectorySeparatorChar).Last().Replace(".mp4", "");
        var outputVideo = Path.Combine(folderPath, "cutted_" + videoTitle + ".mp4");
        await new VideoService().TrimVideoAsync(videoPath, outputVideo, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10));

        File.Delete(videoPath);
        File.Move(outputVideo, videoPath);
    }

    sw.Stop();
    ConsoleService.WriteLineSuccess($"Trim video processes have been took {sw.Elapsed.TotalSeconds}s.");
}