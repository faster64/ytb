using FFmpegArgs.Cores.Enums;
using FFMpegCore.Enums;
using System.Diagnostics;
using Ytb;
using Ytb.Enums;
using Ytb.Extensions;
using Ytb.Services;

await StartupService.InitializeAsync();

var options = Enum.GetValues<OptionEnum>().OrderBy(x => (int)x).ToList();

//if (true)
//{
//    var items1 = Directory.EnumerateFiles(PathManager.InputOriginVideoPath, "*.jpg").OrderBy(x => x.Length).ThenBy(x => x).ToList();
//    var items2 = Directory.EnumerateFiles(PathManager.InputOriginVideoPath, "*.mp4").OrderBy(x => x.Length).ThenBy(x => x).ToList();

//    foreach (var item in items1)
//    {
//        var name = Path.GetFileName(item);
//        var newName = ChannelService.SanitizeFileName(name);

//        File.Move(Path.Combine(PathManager.InputOriginVideoPath, name), Path.Combine(PathManager.InputOriginVideoPath, newName));
//    }

//    foreach (var item in items2)
//    {
//        var name = Path.GetFileName(item);
//        var newName = ChannelService.SanitizeFileName(name);

//        File.Move(Path.Combine(PathManager.InputOriginVideoPath, name), Path.Combine(PathManager.InputOriginVideoPath, newName));
//    }
//}

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
            await ChannelService.DownloadVideosAsync();
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
            Console.Write("Nhập path: ");
            var path3 = Console.ReadLine();

            await TrimVideoAudioAsync(path3);
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
        await ChannelService.GetVideoUrlsAsync(channelHandle);
    }
    else
    {
        while (string.IsNullOrWhiteSpace(channelHandle))
        {
            Console.Write("Nhập channel handle (ví dụ: @line4091): ");
            channelHandle = Console.ReadLine() ?? "";
        }
        await ChannelService.GetVideoUrlsAsync(channelHandle);
    }
}

async Task RenderAudioVideosAsync()
{
    var sw = Stopwatch.StartNew();
    var config = ConfigService.GetConfig();
    var audioConfig = config.AudioConfig;
    ConsoleService.WriteLineSuccess($"Bắt đầu render, ngày hiện tại: {audioConfig.CurrentRenderDay}/{audioConfig.MaxRenderDays}");

    var backgroundFolders = Directory.EnumerateDirectories(PathManager.InputBackgroundPath).OrderBy(x => x.Length).ThenBy(x => x).ToList();
    var originVideos = Directory.EnumerateFiles(PathManager.InputOriginVideoPath, "*.mp4").OrderBy(x => x.Length).ThenBy(x => x).ToList();

    var numberOfChannels = backgroundFolders.Count;
    var videosPerChannel = audioConfig.NumberOfVideosPerChannelDaily;
    var requiredVideoCount = numberOfChannels * videosPerChannel;

    #region Validate resources
    if (originVideos.Count < requiredVideoCount)
    {
        ConsoleService.WriteLineError($"Số kênh đang là {numberOfChannels}, mỗi kênh cần {videosPerChannel} videos => nên sẽ cần {requiredVideoCount} videos. Kiểm tra lại");
        return;
    }

    foreach (var bgFolder in backgroundFolders)
    {
        var bgFiles = Directory.EnumerateFiles(bgFolder, "*.mp4").ToList();
        if (bgFiles.Count < videosPerChannel)
        {
            ConsoleService.WriteLineError($"Thư mục {bgFolder} không có đủ {videosPerChannel} video nền.");
            return;
        }
    }
    #endregion

    Console.WriteLine();

    var renderService = new RenderService();
    var random = new Random();

    var startIndex = (audioConfig.CurrentRenderDay - 1) * videosPerChannel;
    for (int channelIndex = 0; channelIndex < numberOfChannels; channelIndex++)
    {
        var videoPaths = new List<string>();
        while (videoPaths.Count < videosPerChannel)
        {
            if (startIndex >= originVideos.Count)
            {
                startIndex = 0;
            }

            videoPaths.Add(originVideos[startIndex++]);
        }

        var backgroundFolder = backgroundFolders[channelIndex];
        var channelName = backgroundFolder.Split(Path.DirectorySeparatorChar).Last();
        if (audioConfig.IgnoreChannels.Any(x => backgroundFolder.EndsWith("\\" + x)))
        {
            ConsoleService.WriteLineWarning($"Bỏ qua kênh {channelName} theo cấu hình.");
            continue;
        }

        var outputPath = Path.Combine(PathManager.OutputsPath, channelName);

        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, recursive: true);
        }

        var backgroundPaths = Directory.EnumerateFiles(backgroundFolder, "*.mp4").ToList();
        var selectedBackgroundPaths = new List<string>();

        while (selectedBackgroundPaths.Count < videosPerChannel)
        {
            var randomNumber = random.Next(0, backgroundPaths.Count - 1);
            while (selectedBackgroundPaths.Contains(backgroundPaths[randomNumber]))
            {
                randomNumber = random.Next(0, backgroundPaths.Count - 1);
            }

            selectedBackgroundPaths.Add(backgroundPaths[randomNumber]);
        }

        var semaphore = new SemaphoreSlim(audioConfig.CCT);
        var tasks = new List<Task>();
        for (int i = 0; i < videoPaths.Count; i++)
        {
            var videoPath = videoPaths[i];
            var videoTitle = Path.GetFileName(videoPath);
            var outputVideo = Path.Combine(outputPath, videoTitle);
            var index = i;

            Directory.CreateDirectory(outputPath);

            await semaphore.WaitAsync();
            var task = Task.Run(async () =>
            {
                try
                {
                    var sw2 = Stopwatch.StartNew();
                    await renderService.OverlayTextOnBackgroundAsync(videoPath, outputVideo, selectedBackgroundPaths[index], $"[Kênh {channelName}] ");
                    sw2.Stop();

                    var seconds = Math.Floor(sw2.Elapsed.TotalSeconds);
                    var minutes = Math.Floor(seconds / 60);
                    seconds %= 60;

                    ConsoleService.WriteLineSuccess($"[Kênh {channelName}]: video {videoTitle.Substring(0, Math.Min(20, videoTitle.Length))}: {minutes}m{seconds}s");
                }
                catch (Exception ex)
                {
                    ConsoleService.WriteLineError($"[Kênh {channelName}]: Lỗi render video {videoTitle}: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            });

            tasks.Add(task);
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    if (audioConfig.CurrentRenderDay >= audioConfig.MaxRenderDays)
    {
        audioConfig.CurrentRenderDay = 1;
        ConfigService.SaveConfig(ConfigService.GetConfig());

        Directory.Delete(PathManager.InputOriginVideoPath, recursive: true);
        Directory.CreateDirectory(PathManager.InputOriginVideoPath);

        ConsoleService.WriteLineError("Đã render xong hết video phase này. Thị Hoa, bạn hãy tải video mới!");
    }
    else
    {
        audioConfig.CurrentRenderDay++;
    }

    ConfigService.SaveConfig(config);

    sw.Stop();
    ConsoleService.WriteLineSuccess($"Hoàn thành sau {sw.Elapsed.TotalMinutes:N0}m.");
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
            await new RenderService().CreateVideoFromImage(imagePath, outputVideo);
        }
    }

    sw.Stop();
    ConsoleService.WriteLineSuccess($"Hoàn thành sau: {sw.Elapsed.TotalSeconds:N0}s");
}

async Task TrimVideoAudioAsync(string path)
{
    var folderPath = !string.IsNullOrEmpty(path) ? path : PathManager.InputOriginVideoPath;
    var videos = Directory.EnumerateFiles(folderPath, "*.mp4").ToList();

    foreach (var videoPath in videos)
    {
        var videoTitle = videoPath.Split(Path.DirectorySeparatorChar).Last().Replace(".mp4", "");
        var outputVideo = Path.Combine(folderPath, "cutted_" + videoTitle + ".mp4");
        await new RenderService().TrimVideoAsync(videoPath, outputVideo, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(60));

        File.Delete(videoPath);
        File.Move(outputVideo, videoPath);
    }

    ConsoleService.WriteLineSuccess("Ok!");
}
