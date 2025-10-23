using System.Diagnostics;

namespace Ytb.Services
{
    public class DownloadService
    {
        public async Task DownloadVideosAsync()
        {
            var urls = await File.ReadAllLinesAsync(PathManager.InputFileDownloadPath);
            if (urls.Length == 0)
            {
                Console.WriteLine("Không tìm thấy urls trong file.");
                return;
            }

            var sw = Stopwatch.StartNew();
            Console.WriteLine($"Bắt đầu tải {urls.Count()} videos...");

            var outputDir = PathManager.InputOriginVideoPath;
            if (Directory.Exists(outputDir))
            {
                Console.WriteLine("Xóa thư mục output cũ...");
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
            Console.WriteLine($"All downloads completed. {sw.ElapsedMilliseconds:N0}ms");
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());

            // Trim dấu . hoặc khoảng trắng ở cuối
            cleaned = cleaned.TrimEnd('.', ' ');

            var forbiddenWords = new List<string> { "…", "？" };
            foreach (var str in forbiddenWords)
            {
                cleaned = cleaned.Replace(str, " ");
            }

            // Nếu là tên bị cấm trên Windows (CON, PRN, …) thì thêm _ để an toàn
            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

            if (reservedNames.Contains(cleaned.ToUpper()))
                cleaned += "_";

            return cleaned.Trim();
        }
    }
}
