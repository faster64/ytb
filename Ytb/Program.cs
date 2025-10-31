using Newtonsoft.Json;
using System.Diagnostics;
using Ytb;
using Ytb.Contants;
using Ytb.Enums;
using Ytb.Extensions;
using Ytb.Models;
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

        case OptionEnum.DownloadLineVideo:
            await ChannelService.DownloadVideosAsync(PathManager.InputLineFileDownloadPath, PathManager.InputLineOriginVideoPath);
            if (File.Exists(PathManager.InputLineFileChromaKeyPath))
            {
                File.Delete(PathManager.InputLineFileChromaKeyPath);
            }
            File.Create(PathManager.InputLineFileChromaKeyPath).Close();
            break;

        case OptionEnum.DownloadOlderVideo:
            await ChannelService.DownloadVideosAsync(PathManager.InputOlderFileDownloadPath, PathManager.InputOlderOriginVideoPath);
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

        case OptionEnum.RenderOlderVideos:
            await RenderVideosAsync(GlobalConstant.OLDER);
            break;

        case OptionEnum.RenderLineVideos:
            await RenderVideosAsync(GlobalConstant.LINE);
            break;

        case OptionEnum.CreateLineVideoFromImage:
            await CreateVideosFromImagesAsync(PathManager.InputLineBackgroundPath);
            break;

        case OptionEnum.CreateOlderVideoFromImage:
            await CreateVideosFromImagesAsync(PathManager.InputOlderBackgroundPath);
            break;

        case OptionEnum.CutVideo:
            Console.Write("Nhập đường dẫn: ");
            var path3 = Console.ReadLine();

            await TrimVideosAsync(path3);
            break;

        case OptionEnum.UpdateYtDlp:
            await StartupService.UpdateYtDlpAsync();
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
    var durationLines = await File.ReadAllLinesAsync(PathManager.ChannelsFileDurationPath);
    var duration = int.Parse(durationLines.FirstOrDefault() ?? "10");

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

        await ChannelService.GetVideoUrlsAsync(channelHandle, duration);
    }
    else
    {
        while (string.IsNullOrWhiteSpace(channelHandle))
        {
            Console.Write("Nhập channel handle (ví dụ: @line4091): ");
            channelHandle = Console.ReadLine() ?? "";
        }
        await ChannelService.GetVideoUrlsAsync(channelHandle, duration);
    }
}

async Task RenderVideosAsync(string type)
{
    var c = ConfigService.GetConfig();
    var backgroundOutsidePath = "";
    var originVideoPath = "";
    var outputOutsidePath = "";

    RenderConfig config;

    switch (type)
    {
        case GlobalConstant.LINE:
            config = c.LineConfig;
            backgroundOutsidePath = PathManager.InputLineBackgroundPath;
            originVideoPath = PathManager.InputLineOriginVideoPath;
            outputOutsidePath = PathManager.OutputLinePath;
            break;

        case GlobalConstant.OLDER:
            config = c.OlderConfig;
            backgroundOutsidePath = PathManager.InputOlderBackgroundPath;
            originVideoPath = PathManager.InputOlderOriginVideoPath;
            outputOutsidePath = PathManager.OutputOlderPath;
            break;

        default:
            ConsoleService.WriteLineError("Chưa hỗ trợ key này");
            return;
    }

    var sw = Stopwatch.StartNew();
    var startTime = DateTime.Now;

    var backgroundFolders = Directory.EnumerateDirectories(backgroundOutsidePath).OrderBy(x => x.Length).ThenBy(x => x).ToList();
    if (backgroundFolders.Count == 0)
    {
        ConsoleService.WriteLineError("Chưa có kênh");
        return;
    }

    if (type == GlobalConstant.LINE && !File.Exists(PathManager.InputLineFileChromaKeyPath))
    {
        ConsoleService.WriteLineError("Chưa có file chroma-key.txt");
        return;
    }

    for (int i = 0; i < backgroundFolders.Count; i++)
    {
        var folder = backgroundFolders[i];
        var exist = Directory.EnumerateFiles(folder, "*.mp4").FirstOrDefault();
        if (exist == null)
        {
            ConsoleService.WriteLineError($"{folder}: chưa có video nền");
            return;
        }
    }

    var originVideos = Directory.EnumerateFiles(originVideoPath, "*.mp4").ToList();
    var chromaKeys = type == GlobalConstant.LINE ? File.ReadAllLines(PathManager.InputLineFileChromaKeyPath) : new string[] { };

    if (type == GlobalConstant.LINE && originVideos.Count > chromaKeys.Length)
    {
        ConsoleService.WriteLineError("Chưa đủ mã màu trong chroma-key.txt");
        return;
    }

    var numberOfChannels = backgroundFolders.Count;
    var videosPerChannel = config.NumberOfVideosPerChannelDaily;
    var requiredVideoCount = numberOfChannels * videosPerChannel;

    #region Validate resources
    if (originVideos.Count < requiredVideoCount)
    {
        ConsoleService.WriteLineError($"Số kênh đang là {numberOfChannels}, mỗi kênh cần {videosPerChannel} videos => nên sẽ cần {requiredVideoCount} videos. Kiểm tra lại");
        return;
    }
    #endregion

    ConsoleService.WriteLineSuccess($"Bắt đầu render, ngày hiện tại: {config.CurrentRenderDay}/{config.MaxRenderDays}");
    Console.WriteLine();

    var renderService = new RenderService();
    var random = new Random();

    var startIndex = (config.CurrentRenderDay - 1) * videosPerChannel;
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
        if (config.IgnoreChannels.Any(x => backgroundFolder.EndsWith("\\" + x)))
        {
            ConsoleService.WriteLineWarning($"Bỏ qua kênh {channelName} theo cấu hình.");
            continue;
        }

        var outputPath = Path.Combine(outputOutsidePath, channelName);

        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, recursive: true);
        }

        var backgroundPath = Directory.EnumerateFiles(backgroundFolder, "*.mp4").FirstOrDefault();
        var semaphore = new SemaphoreSlim(config.CCT);
        var tasks = new List<Task>();
        for (int i = 0; i < videoPaths.Count; i++)
        {
            var videoPath = videoPaths[i];
            var videoTitle = Path.GetFileName(videoPath);
            var outputVideo = Path.Combine(outputPath, videoTitle);
            var index = i;

            Directory.CreateDirectory(outputPath);

            var task = Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var sw2 = Stopwatch.StartNew();
                    switch (type)
                    {
                        case GlobalConstant.LINE:
                            var chromaKey = chromaKeys[index];
                            await renderService.RenderLineAsync(videoPath, outputVideo, backgroundPath, chromaKey, $"[Kênh {channelName}] ");
                            break;
                        case GlobalConstant.OLDER:
                            await renderService.RenderOlderAsync(videoPath, outputVideo, backgroundPath, $"[Kênh {channelName}] ");
                            break;
                    }
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

    if (config.CurrentRenderDay >= config.MaxRenderDays)
    {
        config.CurrentRenderDay = 1;
        ConfigService.SaveConfig(ConfigService.GetConfig());

        Directory.Delete(PathManager.InputLineOriginVideoPath, recursive: true);
        Directory.CreateDirectory(PathManager.InputLineOriginVideoPath);

        ConsoleService.WriteLineError("Đã render xong hết video phase này. Bạn hãy tải video mới!");
    }
    else
    {
        config.CurrentRenderDay++;
    }

    ConfigService.SaveConfig(c);

    sw.Stop();
    var endTime = DateTime.Now;

    ConsoleService.WriteLineSuccess($"{startTime.ToString("HH:mm:ss")} - {endTime.ToString("HH:mm:ss")}");
    ConsoleService.WriteLineSuccess($"Hoàn thành sau {sw.Elapsed.TotalMinutes:N0}m.");
}

async Task CreateVideosFromImagesAsync(string path)
{
    var sw = Stopwatch.StartNew();
    for (int i = 1; i <= 500; i++)
    {
        var folderPath = Path.Combine(path, i.ToString());
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

async Task TrimVideosAsync(string folderPath)
{
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

















