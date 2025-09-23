﻿using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Ytb.Services
{
    public class VideoService
    {
        private static string FfmpegPath = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native", "ffmpeg.exe");

        public async Task ReplaceBackgroundAsync()
        {
            var inputVideo = Path.Combine(PathManager.DownloadOutputPath, "cutted.mp4");
            var outputVideo = Path.Combine(PathManager.DownloadOutputPath, "output_with_new_bg.mp4");
            var backgroundImage = "D:\\Youtube\\resources\\audio\\12.jpg";

            if (File.Exists(outputVideo))
            {
                File.Delete(outputVideo);
            }

            var arguments = $"-i \"{inputVideo}\" -i \"{backgroundImage}\" " +
                            "-filter_complex " +
                            "\"[1:v][0:v]scale2ref=force_original_aspect_ratio=decrease[bg][fgtmp];" +
                            "[fgtmp]colorkey=0x7097B8:0.3:0.2[fg];" +
                            "[bg][fg]overlay=0:0:format=auto[out]\" " +
                            "-map \"[out]\" -map 0:a? -c:v libx264 -pix_fmt yuv420p -c:a aac -shortest -y " +
                            $"\"{outputVideo}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = FfmpegPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("ERR: " + e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
        }

        public async Task RemoveBackgroundAsync()
        {
            var inputVideo = Path.Combine(PathManager.DownloadOutputPath, "cutted.mp4");
            var outputVideo = Path.Combine(PathManager.DownloadOutputPath, "output_transparent.mov");

            if (File.Exists(outputVideo))
            {
                File.Delete(outputVideo);
            }

            // Lệnh FFmpeg: colorkey, xuất nền trong suốt
            var arguments = $"-i \"{inputVideo}\" " +
                            "-filter_complex " +
                            "\"[0:v]colorkey=0x7097B8:0.3:0.2[fg]\" " +  // bỏ màu xanh
                            "-map \"[fg]\" -c:v qtrle -pix_fmt argb -y " +
                            $"\"{outputVideo}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = FfmpegPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("ERR: " + e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
        }

        public async Task CutVideoAsync(TimeSpan startTime, TimeSpan duration)
        {
            var inputVideo = Directory.EnumerateFiles(PathManager.DownloadOutputPath, "*.mp4").FirstOrDefault();
            var outputVideo = Path.Combine(PathManager.DownloadOutputPath, "cutted.mp4");
            var arguments = $"-ss {ToFfmpegTime(startTime)} -t {ToFfmpegTime(duration)} -i \"{inputVideo}\" -c copy \"{outputVideo}\"";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = FfmpegPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("ERR: " + e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
        }

        private static string ToFfmpegTime(TimeSpan ts)
        {
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }

        public static void AddPrefix()
        {
            var mp4Files = Directory.EnumerateFiles(PathManager.DownloadOutputPath, "*.mp4").ToList();

            int counter = 1;
            foreach (var file in mp4Files)
            {
                var dir = Path.GetDirectoryName(file)!;
                var fileName = Path.GetFileName(file);

                if (Regex.IsMatch(fileName, @"^NUM\d+_"))
                    continue;

                var newName = $"NUM{counter}_{fileName}";
                var newPath = Path.Combine(dir, newName);

                File.Move(file, newPath);
                counter++;
            }
        }

        public static void RemovePrefix()
        {
            var mp4Files = Directory.EnumerateFiles(PathManager.DownloadOutputPath, "*.mp4").ToList();

            foreach (var file in mp4Files)
            {
                var dir = Path.GetDirectoryName(file)!;
                var fileName = Path.GetFileName(file);

                var newName = Regex.Replace(fileName, @"^NUM\d+_", "");
                if (newName != fileName)
                {
                    var newPath = Path.Combine(dir, newName);
                    File.Move(file, newPath);
                }
            }
        }
    }
}
