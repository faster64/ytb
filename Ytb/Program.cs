using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Ytb;
using Ytb.Contants;
using Ytb.Enums;
using Ytb.Extensions;
using Ytb.Models;
using Ytb.Runtimes.Filters;
using Ytb.Services;

await StartupService.InitializeAsync();

var options = Enum.GetValues<OptionEnum>().OrderBy(x => (int)x).ToList();

await Main();

async Task Main()
{
    var choice = SelectOption();

    switch (choice)
    {
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

        case OptionEnum.AddPrefixToVideo:
            Console.Write("Nhập đường dẫn: ");
            var path = Console.ReadLine();

            RenderService.AddPrefix(path);
            Console.WriteLine("Thêm STT thành công");
            break;

        case OptionEnum.RemovePrefixToVideo:
            Console.Write("Nhập đường dẫn: ");
            var path2 = Console.ReadLine();

            RenderService.RemovePrefix(path2);
            Console.WriteLine("Xóa STT thành công");
            break;

        case OptionEnum.ChangeThumbnailColor:
            ChangeThumbnailColor();
            break;

        case OptionEnum.RenderOlderVideos:
            await RenderVideosAsync(GlobalConstant.OLDER);
            await RenderVideosAsync(GlobalConstant.LINE);
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

        case OptionEnum.ExtendLineBackgroundVideo:
            await ExtendVideosAsync(PathManager.InputLineBackgroundPath);
            break;

        case OptionEnum.ExtendOlderBackgroundVideo:
            await ExtendVideosAsync(PathManager.InputOlderBackgroundPath);
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
    var config = ConfigService.GetConfig();
    Console.WriteLine($"     [LINE]: Ngày thứ {config.LineConfig.CurrentRenderDay}/{config.LineConfig.MaxRenderDays}");
    Console.WriteLine($"[NGƯỜI GIÀ]: Ngày thứ {config.OlderConfig.CurrentRenderDay}/{config.OlderConfig.MaxRenderDays}");
    Console.WriteLine();

    foreach (var option in options)
    {
        Console.WriteLine((int)option + ". " + option.GetDescription());

        var memberInfo = option.GetType().GetMember(option.ToString()).FirstOrDefault();
        var breakLineAttribute = memberInfo?.GetCustomAttributes<BreakLineAttribute>(false).FirstOrDefault();
        if (breakLineAttribute != null)
        {
            for (int i = 0; i < breakLineAttribute.NumberOfBreakLines; i++)
            {
                Console.WriteLine();
            }
        }
    }

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
    var numberOfChannels = backgroundFolders.Count;
    var videosPerChannel = config.NumberOfVideosPerChannelDaily;
    var requiredVideoCount = numberOfChannels * videosPerChannel;

    var chromaKeys = type == GlobalConstant.LINE ? File.ReadAllLines(PathManager.InputLineFileChromaKeyPath) : new string[] { };

    if (type == GlobalConstant.LINE && requiredVideoCount > chromaKeys.Length)
    {
        ConsoleService.WriteLineError($"Chưa đủ mã màu trong chroma-key.txt {chromaKeys.Length}/{requiredVideoCount}");
        return;
    }

    #region Validate resources
    //if (originVideos.Count < requiredVideoCount)
    //{
    //    ConsoleService.WriteLineError($"Số kênh đang là {numberOfChannels}, mỗi kênh cần {videosPerChannel} videos => nên sẽ cần {requiredVideoCount} videos. Kiểm tra lại");
    //    return;
    //}
    #endregion

    ConsoleService.WriteLineSuccess($"[{startTime.ToString("HH:mm:ss")}] Bắt đầu render, ngày hiện tại: {config.CurrentRenderDay}/{config.MaxRenderDays}");
    Console.WriteLine();

    var renderService = new RenderService();
    var random = new Random();
    var semaphore = new SemaphoreSlim(config.CCT);
    var tasks = new List<Task>();
    var startIndex = (config.CurrentRenderDay - 1) * videosPerChannel;

    for (int channelIndex = 0; channelIndex < numberOfChannels; channelIndex++)
    {
        var videoPaths = new Dictionary<string, string>();
        while (videoPaths.Count < videosPerChannel)
        {
            if (startIndex >= originVideos.Count)
            {
                startIndex = 0;
            }

            var chromaKey = type == GlobalConstant.LINE ? chromaKeys[startIndex] : "";
            videoPaths.Add(originVideos[startIndex++], chromaKey);
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
        foreach (var item in videoPaths)
        {
            var videoPath = item.Key;
            var chromaKey = item.Value;
            var videoTitle = Path.GetFileName(videoPath);
            var outputVideo = Path.Combine(outputPath, videoTitle);

            Directory.CreateDirectory(outputPath);

            var task = Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var sw2 = Stopwatch.StartNew();

                    var title = videoTitle.Substring(0, Math.Min(20, videoTitle.Length));
                    switch (type)
                    {
                        case GlobalConstant.LINE:
                            await renderService.RenderLineAsync(videoPath, outputVideo, backgroundPath, chromaKey, $"[Kênh {channelName}] {title} ");
                            break;
                        case GlobalConstant.OLDER:
                            await renderService.RenderOlderAsync(videoPath, outputVideo, backgroundPath, $"[Kênh {channelName}] {title} ");
                            break;
                    }
                    sw2.Stop();

                    var seconds = Math.Floor(sw2.Elapsed.TotalSeconds);
                    var minutes = Math.Floor(seconds / 60);
                    seconds %= 60;

                    ConsoleService.WriteLineSuccess($"[Kênh {channelName}]: video {title}: {minutes}m{seconds}s");
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
    }

    if (tasks.Count > 0)
    {
        await Task.WhenAll(tasks);
    }

    if (config.CurrentRenderDay >= config.MaxRenderDays)
    {
        config.CurrentRenderDay = 1;
        ConfigService.SaveConfig(ConfigService.GetConfig());
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
    var folders = Directory.EnumerateDirectories(path).OrderBy(x => x.Length).ThenBy(x => x).ToList();
    if (folders.Count == 0)
    {
        ConsoleService.WriteLineError("Chưa có kênh");
        return;
    }

    var sw = Stopwatch.StartNew();
    foreach (var folder in folders)
    {
        var folderPath = Path.Combine(path, folder);
        if (!Directory.Exists(folderPath))
        {
            continue;
        }

        var images = Directory.EnumerateFiles(folderPath, "*.jpg").ToList();

        foreach (var inputImage in images)
        {
            var outputVideo = Path.Combine(folderPath, inputImage.Split(Path.DirectorySeparatorChar).Last().Replace(".jpg", ".mp4"));
            await new RenderService().CreateVideoFromImage(inputImage, outputVideo);
        }
    }
    sw.Stop();
    ConsoleService.WriteLineSuccess($"Hoàn thành sau: {sw.Elapsed.TotalSeconds:N0}s");
}

async Task ExtendVideosAsync(string path)
{
    var folders = Directory.EnumerateDirectories(path).OrderBy(x => x.Length).ThenBy(x => x).ToList();
    if (folders.Count == 0)
    {
        ConsoleService.WriteLineError("Chưa có kênh");
        return;
    }

    var sw = Stopwatch.StartNew();
    foreach (var folder in folders)
    {
        var folderPath = Path.Combine(path, folder);
        if (!Directory.Exists(folderPath))
        {
            continue;
        }

        var videos = Directory.EnumerateFiles(folderPath, "*.mp4").ToList();

        foreach (var outputVideo in videos)
        {
            var name = Path.GetFileName(outputVideo);
            var tmp = Path.Combine(folderPath, $"{new Random().Next(1000000)}_{name}");

            File.Move(outputVideo, tmp);

            await new RenderService().ExtendVideo(tmp, outputVideo);

            File.Delete(tmp);
        }
    }
    sw.Stop();
    ConsoleService.WriteLineSuccess($"Hoàn thành sau: {sw.Elapsed.TotalSeconds:N0}s");
}

async Task TrimVideosAsync(string folderPath)
{
    if (string.IsNullOrEmpty(folderPath))
    {
        folderPath = PathManager.InputLineOriginVideoPath;
    }

    var videos = Directory.EnumerateFiles(folderPath, "*.mp4").ToList();
    foreach (var videoPath in videos)
    {
        var videoTitle = videoPath.Split(Path.DirectorySeparatorChar).Last().Replace(".mp4", "");
        var outputVideo = Path.Combine(folderPath, "cutted_" + videoTitle + ".mp4");
        await new RenderService().TrimVideoAsync(videoPath, outputVideo, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(ConfigService.GetConfig().CutDuration));

        File.Delete(videoPath);
        File.Move(outputVideo, videoPath);
    }

    ConsoleService.WriteLineSuccess("Ok!");
}

void ChangeThumbnailColor()
{
    ConsoleService.WriteLineSuccess("Hồng hồng: #FFC0CB #FFC0CB");
    ConsoleService.WriteLineSuccess("Tím rực: #A100FF #FBC2EB");
    ConsoleService.WriteLineSuccess("Xanh điện: #007CF0 #00DFD8");
    ConsoleService.WriteLineSuccess("Hồng neon: #FF5EDF #FFB6FF");
    ConsoleService.WriteLineSuccess("Cam cháy: #FF7F50 #FFB347");
    ConsoleService.WriteLineSuccess("Đỏ năng lượng: #FF4B4B #FFD93D");

    Console.WriteLine();
    Console.Write("Nhập gradients: ");
    var gradients = Console.ReadLine()?.Trim();
    var match = Regex.Match(gradients, @"^(\#[\w\d]{6})\s+(\#[\w\d]{6})$");
    var thumbnails = Directory.EnumerateFiles(PathManager.InputLineOriginVideoPath, "*.jpg").ToList();

    var bakFolder = Path.Combine(PathManager.InputLineOriginVideoPath, "thumbnail-bak");
    Directory.CreateDirectory(bakFolder);
    foreach (var thumbnail in thumbnails)
    {
        var des = Path.Combine(Path.GetDirectoryName(thumbnail), "thumbnail-bak", Path.GetFileName(thumbnail));
        if (!File.Exists(des))
        {
            File.Copy(thumbnail, des);
        }
    }

    string gradientStart = null;
    string gradientEnd = null;

    if (match.Success)
    {
        gradientStart = match.Groups[1].Value;
        gradientEnd = match.Groups[2].Value;
    }
    else
    {
        match = Regex.Match(gradients, @"^(\#[\w\d]{6})$");
        if (match.Success)
        {
            gradientStart = gradientEnd = match.Groups[1].Value;
        }
    }

    ConsoleService.WriteLineWarning("Đang thay đổi...");
    foreach (var thumbnail in thumbnails)
    {
        var tmp = Path.Combine(Path.GetDirectoryName(thumbnail), "tmp_tmp.jpg");
        try
        {
            File.Copy(thumbnail, tmp);
            RenderService.ReplaceBackgroundWithGradient(tmp, thumbnail, gradientStart: gradientStart, gradientEnd: gradientEnd);
        }
        finally
        {
            if (File.Exists(tmp))
            {
                File.Delete(tmp);
            }
        }
    }

    ConsoleService.WriteLineSuccess("Thay đổi màu thumbnail thành công");

    Console.WriteLine("Nhập số 0 để hoàn tác: ");
    var isRollback = Console.ReadLine() == "0";
    if (isRollback)
    {
        var originThumbnails = Directory.EnumerateFiles(Path.Combine(PathManager.InputLineOriginVideoPath, "thumbnail-bak"), "*.jpg").ToList();
        foreach (var thumbnail in originThumbnails)
        {
            var des = Path.Combine(PathManager.InputLineOriginVideoPath, Path.GetFileName(thumbnail));
            File.Copy(thumbnail, des, true);
        }
    }
}







