using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Diagnostics;
using System.Text;

namespace Ytb.Services
{
    public class ChannelService
    {
        public static async Task GetVideoUrlsAsync(string handle)
        {
            handle = handle.Trim();

            var apiKey = ConfigService.GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("Chưa có api key. Vui lòng cấu hình trong file config.txt");
                return;
            }

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "Ytb"
            });

            // Lấy channelId từ handle
            var channelsRequest = youtubeService.Channels.List("id,contentDetails");
            channelsRequest.ForHandle = handle;
            var channelsResponse = await channelsRequest.ExecuteAsync();

            if (channelsResponse.Items == null || channelsResponse.Items.Count == 0)
            {
                Console.WriteLine($"Không tìm thấy channel cho handle: {handle}");
                return;
            }

            var channel = channelsResponse.Items[0];
            var uploadsPlaylistId = channel.ContentDetails.RelatedPlaylists.Uploads;

            Console.WriteLine($"Uploads Playlist ID: {uploadsPlaylistId}");
            Console.WriteLine("Đang lấy danh sách video...");

            var directoryPath = Path.Combine(PathManager.ChannelsPath, handle.TrimStart('@'));
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var filePath = Path.Combine(directoryPath, "only_video_urls.txt");
            if (File.Exists(filePath)) File.Delete(filePath);

            var filePath2 = Path.Combine(directoryPath, "video_infos.txt");
            if (File.Exists(filePath2)) File.Delete(filePath2);

            string nextPageToken = null;
            var allVideos = new List<(string Id, string Title, TimeSpan Duration, string PublishedAt, ulong ViewCount)>();

            do
            {
                var playlistItemsRequest = youtubeService.PlaylistItems.List("snippet");
                playlistItemsRequest.PlaylistId = uploadsPlaylistId;
                playlistItemsRequest.MaxResults = 50;
                playlistItemsRequest.PageToken = nextPageToken;

                var playlistItemsResponse = await playlistItemsRequest.ExecuteAsync();

                var videoIds = playlistItemsResponse.Items
                    .Select(i => i.Snippet.ResourceId.VideoId)
                    .ToList();

                if (videoIds.Count > 0)
                {
                    // Lấy thêm thống kê view
                    var videosRequest = youtubeService.Videos.List("contentDetails,snippet,statistics");
                    videosRequest.Id = string.Join(",", videoIds);

                    var videosResponse = await videosRequest.ExecuteAsync();

                    foreach (var video in videosResponse.Items)
                    {
                        var duration = System.Xml.XmlConvert.ToTimeSpan(video.ContentDetails.Duration);
                        if (duration.TotalSeconds <= 600)
                        {
                            continue;
                        }

                        var viewCount = video.Statistics?.ViewCount ?? 0;
                        allVideos.Add((
                            Id: video.Id,
                            Title: video.Snippet.Title,
                            Duration: duration,
                            PublishedAt: video.Snippet.PublishedAtRaw,
                            ViewCount: viewCount
                        ));

                    }
                }

                nextPageToken = playlistItemsResponse.NextPageToken;
            }
            while (nextPageToken != null);

            // Sắp xếp giảm dần theo view
            var sortedVideos = allVideos.OrderByDescending(v => v.ViewCount).ToList();

            Console.WriteLine($"\nTổng số video hợp lệ: {sortedVideos.Count}");
            Console.WriteLine("Đang ghi file...");

            foreach (var video in sortedVideos)
            {
                Console.WriteLine($"https://www.youtube.com/watch?v={video.Id} | {video.Duration} | Views: {video.ViewCount:N0}");

                var url = $"https://www.youtube.com/watch?v={video.Id}";
                File.AppendAllText(filePath, url + Environment.NewLine);

                File.AppendAllText(filePath2,
                    $"{video.PublishedAt}\t{url}\t{video.ViewCount}\t{video.Title}\t{video.Duration}{Environment.NewLine}");
            }

            Console.WriteLine("\nHoàn thành!");
        }

        public static async Task DownloadVideosAsync()
        {
            var urls = await File.ReadAllLinesAsync(PathManager.InputFileDownloadPath);
            if (urls.Length == 0)
            {
                ConsoleService.WriteLineError("Không tìm thấy urls trong file.");
                return;
            }

            var sw = Stopwatch.StartNew();
            Console.WriteLine($"Bắt đầu tải {urls.Count()} videos...");

            var outputDir = PathManager.InputOriginVideoPath;
            if (Directory.Exists(outputDir))
            {
                Console.WriteLine("Đang xóa thư mục origin-videos cũ...");
                Directory.Delete(outputDir, recursive: true);
            }

            Directory.CreateDirectory(outputDir);

            var chunks = urls.Chunk(10);

            foreach (var chunk in chunks)
            {
                var tasks = new List<Task>();
                foreach (var url in chunk)
                {
                    if (string.IsNullOrWhiteSpace(url))
                        continue;

                    tasks.Add(Task.Run(async () =>
                    {
                        var processFileName = Path.Combine(Directory.GetCurrentDirectory(), "yt-dlp.exe");
                        var cookiePath = Path.Combine(Directory.GetCurrentDirectory(), "cookies.txt");

                        //var arguments = $"--cookies \"C:\\temp\\youtube_cookies.txt\" -f \"bestvideo+bestaudio/best\" --merge-output-format mp4 -o \"{outputDir}\\%(title)s.%(ext)s\" --write-thumbnail --convert-thumbnails jpg \"{url.Trim()}\"";
                        var arguments = $"-f \"bestvideo+bestaudio/best\" --merge-output-format mp4 -o \"{outputDir}\\%(title)s.%(ext)s\" --write-thumbnail --convert-thumbnails jpg {url.Trim()}";
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = processFileName,
                                Arguments = arguments,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true,
                                StandardOutputEncoding = System.Text.Encoding.UTF8,

                            }
                        };

                        process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                        process.ErrorDataReceived += (sender, e) => Console.WriteLine("ERR: " + e.Data);

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();
                    }));
                }

                await Task.WhenAll(tasks);
            }

            var videos = Directory.GetFiles(outputDir, "*.mp4", SearchOption.TopDirectoryOnly);
            var images = Directory.GetFiles(outputDir, "*.jpg", SearchOption.TopDirectoryOnly);


            foreach (var file in videos)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var safeName = SanitizeFileName(fileName);

                var newName = $"{safeName}.mp4";
                var newPath = Path.Combine(outputDir, newName);

                if (!File.Exists(newPath))
                {
                    File.Move(file, newPath);
                }
            }

            foreach (var file in images)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var safeName = SanitizeFileName(fileName);

                var newName = $"{safeName}.jpg";
                var newPath = Path.Combine(outputDir, newName);

                if (!File.Exists(newPath))
                {
                    File.Move(file, newPath);
                }
            }

            sw.Stop();
            ConsoleService.WriteLineSuccess($"Tải thành công {videos.Count()} videos. {sw.Elapsed.TotalSeconds:N0}s");
        }

        public static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());

            // Trim dấu . hoặc khoảng trắng ở cuối
            cleaned = cleaned.TrimEnd('.', ' ');

            var forbiddenWords = new Dictionary<string, string> {
                {"…", "..."},
                {"？", " "}
            };
            foreach (var word in forbiddenWords)
            {
                cleaned = cleaned.Replace(word.Key, word.Value);
            }

            // Nếu là tên bị cấm trên Windows (CON, PRN, …) thì thêm _ để an toàn
            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

            if (reservedNames.Contains(cleaned.ToUpper()))
            {
                cleaned += "_";
            }

            return cleaned.Trim().Normalize(NormalizationForm.FormC);
        }
    }
}
