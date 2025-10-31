﻿using System.Diagnostics;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace Ytb.Services
{
    public class RenderService
    {
        public static string _ffmpegPath = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native", "ffmpeg.exe");
        public static string _ffprobePath = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native", "ffprobe.exe");

        public static double GetVideoDuration(string filePath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            var output = process!.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return double.TryParse(output, out var duration) ? duration : 0;
        }

        public async Task RenderLineAsync(string inputVideo, string outputVideo, string backgroundVideoPath, string chromaKey, string logPrefix = "")
        {
            if (File.Exists(outputVideo))
            {
                File.Delete(outputVideo);
            }

            var overlayDuration = GetVideoDuration(inputVideo);

            var arguments =
                $"-i \"{backgroundVideoPath}\" " +
                $"-i \"{inputVideo}\" " +
                $"-filter_complex \"[1:v]scale=1280:720," +
                $"colorkey=0x{chromaKey}:0.3:0.1," +
                "format=yuva420p[overlay_video];" +
                "[0:v][overlay_video]overlay=0:H-h[combined_video];" +
                "[1:a]volume=1.0[overlay_audio]\" " +
                "-map \"[combined_video]\" -map \"[overlay_audio]\" " +
                "-c:v libx264 -c:a aac -preset ultrafast " +
                $"-t {overlayDuration} " +
                $"\"{outputVideo}\"";

            await RunProcessAsync(arguments, logPrefix);
        }

        public async Task RenderOlderAsync(string inputVideo, string outputVideo, string backgroundVideoPath, string logPrefix = "")
        {
            var videoTitle = inputVideo.Split(Path.DirectorySeparatorChar).Last();
            var croppedText = $"cropped_text_{videoTitle}";
            var overlayAlpha = "overlay_alpha.webm";

            if (File.Exists(outputVideo)) File.Delete(outputVideo);
            if (File.Exists(croppedText)) File.Delete(croppedText);
            if (File.Exists(overlayAlpha)) File.Delete(overlayAlpha);

            var hasNvidia = HasNvidiaGpu();

            try
            {
                var cropArgs = "";
                var alphaArgs = "";
                var finalArgs = "";
                var scale = "1280:720";
                var chromaKey = "0x202020:0.2:0.1";
                var cropValue = ConfigService.GetConfig().OlderConfig.CropValue; // "in_w:190:0:650"
                var overlayValue = ConfigService.GetConfig().OlderConfig.OverlayValue; // "(main_w-overlay_w)/2:490";
                var arg =
                    $"-i \"{backgroundVideoPath}\" -i \"{inputVideo}\" " +
                    $"-filter_complex \"[1:v]scale={scale},crop={cropValue}," +
                    "lutyuv=y='if(gt(val,180),255,0)':u=128:v=128," +
                    "format=yuva420p,colorchannelmixer=aa=1.0[overlay];" +
                    $"[0:v][overlay]overlay={overlayValue}\" " +
                    "-c:v libx264 -crf 18 -preset ultrafast -shortest " +
                    $"\"{outputVideo}\"";

                if (hasNvidia)
                {
                    cropArgs = $"-hwaccel cuda -i \"{inputVideo}\" " +
                        $"-vf \"scale={scale},crop={cropValue},lutyuv=y='if(gt(val,180),255,0)':u=128:v=128\" " +
                        "-c:v h264_nvenc -preset fast -b:v 5M " +
                        $"\"{croppedText}\"";

                    alphaArgs =
                        $"-hwaccel cuda -i \"{croppedText}\" " +
                        $"-vf \"format=yuva420p,chromakey={chromaKey}\" " +
                        "-c:v h264_nvenc -preset fast -b:v 5M " +
                        $"\"{overlayAlpha}\"";

                    finalArgs = $"-loop 1 -i \"{backgroundVideoPath}\" -i \"{overlayAlpha}\" " +
                        $"-filter_complex \"[0:v][1:v] overlay={overlayValue}\" " +
                        "-c:v h264_nvenc -b:v 5M -preset fast -shortest " +
                        $"\"{outputVideo}\"";
                }
                else
                {
                    cropArgs = $"-i \"{inputVideo}\" -vf \"scale={scale},crop={cropValue},lutyuv=y='if(gt(val,180),255,0)':u=128:v=128\" -c:v libx264 -crf 18 -preset ultrafast \"{croppedText}\"";

                    alphaArgs = $"-i \"{croppedText}\" -vf \"format=yuva420p,chromakey={chromaKey}\" -c:v libvpx-vp9 -auto-alt-ref 0 -speed 8 \"{overlayAlpha}\"";

                    finalArgs = $"-loop 1 -i \"{backgroundVideoPath}\" -i \"{overlayAlpha}\" -filter_complex \"[0:v][1:v] overlay={overlayValue}\" -c:v libx264 -crf 18 -preset ultrafast -shortest \"{outputVideo}\"";
                }


                // B1: Crop vùng chữ (scale về 1280x720 rồi crop lại vùng cần thiết)
                await RunProcessAsync(arg, logPrefix);

                // B2: Biến nền tối thành trong suốt (alpha)
                //var sw2 = Stopwatch.StartNew();
                //await RunProcessAsync(alphaArgs);

                //sw2.Stop();
                //ConsoleService.WriteLineSuccess("Alpha: " + sw2.Elapsed.TotalSeconds + "s");

                //var sw3 = Stopwatch.StartNew();
                //// B3: Overlay chữ vào giữa background
                //await RunProcessAsync(finalArgs);

                //sw3.Stop();
                //ConsoleService.WriteLineSuccess("Final: " + sw3.Elapsed.TotalSeconds + "s");
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (File.Exists(croppedText)) File.Delete(croppedText);
                if (File.Exists(overlayAlpha)) File.Delete(overlayAlpha);
                // if (File.Exists(cuttedVideo)) File.Delete(cuttedVideo);
            }
        }

        public async Task TrimVideoAsync(string inputVideo, string outputVideo, TimeSpan from, TimeSpan duration)
        {
            var durationArg = duration > TimeSpan.Zero ? $"-t {ToFfmpegTime(duration)}" : "";
            var arguments = $"-ss {ToFfmpegTime(from)} {durationArg} -i \"{inputVideo}\" -c copy \"{outputVideo}\"";

            if (File.Exists(outputVideo))
            {
                File.Delete(outputVideo);
            }

            await RunProcessAsync(arguments);
        }

        public async Task CreateVideoFromImage(string inputImage, string outputVideo, int duration = 5400, int fps = 1, int width = 1280, int height = 720)
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"tmp_1s_{Guid.NewGuid():N}.mp4");
            var args = $"-y -loop 1 -i \"{inputImage}\" -t 1 -vf \"scale={width}:{height},setsar=1\" -r {fps} -c:v libx264 -preset ultrafast -crf 23 -pix_fmt yuv420p \"{tmp}\"";
            await RunProcessAsync(args);

            var arguments = $"-y -stream_loop -1 -i \"{tmp}\" -t {duration} -c copy -movflags +faststart \"{outputVideo}\"";

            await RunProcessAsync(arguments);

            if (File.Exists(tmp))
            {
                File.Delete(tmp);
            }
        }

        public async Task ExtendVideo(string inputVideo, string outputVideo, int duration = 5400)
        {
            if (File.Exists(outputVideo))
                File.Delete(outputVideo);

            var arguments =
                $"-stream_loop -1 -i \"{inputVideo}\" " +
                $"-t {duration} -an " +
                "-c copy " +
                $"\"{outputVideo}\"";

            await RunProcessAsync(arguments);
        }

        public static void AddPrefix()
        {
            var path = PathManager.InputLineOriginVideoPath;
            var mp4Files = Directory.EnumerateFiles(path, "*.mp4").ToList();

            int counter = 1;
            foreach (var file in mp4Files)
            {
                var dir = Path.GetDirectoryName(file)!;
                var fileName = Path.GetFileName(file);

                if (Regex.IsMatch(fileName, @"^NUM\d+_"))
                    continue;

                var s = "";
                if (counter < 10)
                {
                    s = "00" + counter;
                }
                else if (counter < 100)
                {
                    s = "0" + counter;
                }

                var newName = $"NUM{s}_{fileName}";
                var newPath = Path.Combine(dir, newName);

                File.Move(file, newPath);
                counter++;
            }
        }

        public static void RemovePrefix()
        {
            var path = PathManager.InputLineOriginVideoPath;
            var mp4Files = Directory.EnumerateFiles(path, "*.mp4").ToList();

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

        private string ToFfmpegTime(TimeSpan ts)
        {
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }

        private async Task RunProcessAsync(string arguments, string logPrefix = "")
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var i = 0;
            var ei = 0;
            var li = 40;

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    i++;
                    if (i >= li && !string.IsNullOrEmpty(e.Data) && e.Data.Contains("time="))
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(logPrefix);
                        Console.ResetColor();
                        Console.WriteLine(e.Data);
                        i = 0;
                    }
                }
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    ei++;
                    if (ei >= li && !string.IsNullOrEmpty(e.Data) && e.Data.Contains("time="))
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(logPrefix);
                        Console.ResetColor();
                        Console.WriteLine("ERR: " + e.Data);
                        ei = 0;
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
        }

        public static bool HasNvidiaGpu()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "nvidia-smi",
                        Arguments = "-L",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return !string.IsNullOrWhiteSpace(output) && output.Contains("GPU");
            }
            catch
            {
                return false;
            }
        }
    }
}
