using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Ytb.Services
{
    public class VideoService
    {
        private string _ffmpegPath = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native", "ffmpeg.exe");
        private string _ffprobePath = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native", "ffprobe.exe");

        public async Task ReplaceBackgroundAsync()
        {
            var inputVideo = Path.Combine(PathManager.InputOriginVideoPath, "cutted.mp4");
            var outputVideo = Path.Combine(PathManager.OutputsPath, "output_with_new_bg.mp4");
            var backgroundImage = "D:\\12.jpg";

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

            await RunProcessAsync(arguments);
        }

        public async Task RemoveBackgroundAsync()
        {
            var inputVideo = Path.Combine(PathManager.InputOriginVideoPath, "cutted.mp4");
            var outputVideo = Path.Combine(PathManager.OutputsPath, "output_transparent.mov");

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

            await RunProcessAsync(arguments);
        }

        public async Task OverlayTextOnBackgroundAsync(string inputVideo, string outputVideo, string backgroundImagePath)
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
                if (hasNvidia)
                {
                    cropArgs = $"-hwaccel cuda -i \"{inputVideo}\" " +
                        "-vf \"scale=1280:720,crop=in_w:190:0:650,lutyuv=y='if(gt(val,180),255,0)':u=128:v=128\" " +
                        "-c:v h264_nvenc -preset fast -b:v 5M " +
                        $"\"{croppedText}\"";

                    alphaArgs =
                        $"-hwaccel cuda -i \"{croppedText}\" " +
                        "-vf \"format=yuva420p,chromakey=0x202020:0.2:0.1\" " +
                        "-c:v h264_nvenc -preset fast -b:v 5M " +
                        $"\"{overlayAlpha}\"";

                    finalArgs = $"-loop 1 -i \"{backgroundImagePath}\" -i \"{overlayAlpha}\" " +
                        "-filter_complex \"[0:v][1:v] overlay=(main_w-overlay_w)/2:650\" " +
                        "-c:v h264_nvenc -b:v 5M -preset fast -shortest " +
                        $"\"{outputVideo}\"";
                }
                else
                {
                    cropArgs = $"-i \"{inputVideo}\" -vf \"scale=1280:720,crop=in_w:190:0:650,lutyuv=y='if(gt(val,180),255,0)':u=128:v=128\" -c:v libx264 -crf 18 -preset ultrafast \"{croppedText}\"";
                    alphaArgs = $"-i \"{croppedText}\" -vf \"format=yuva420p,chromakey=0x202020:0.2:0.1\" -c:v libvpx-vp9 -auto-alt-ref 0 -speed 8 \"{overlayAlpha}\"";
                    finalArgs = $"-loop 1 -i \"{backgroundImagePath}\" -i \"{overlayAlpha}\" " +
                        "-filter_complex \"[0:v][1:v] overlay=(main_w-overlay_w)/2:600\" " +
                        "-c:v libx264 -crf 18 -preset ultrafast -shortest " +
                        $"\"{outputVideo}\"";
                }

                var sw = Stopwatch.StartNew();

                // B1: Crop vùng chữ (scale về 1280x720 rồi crop lại vùng cần thiết)
                await RunProcessAsync(cropArgs);

                sw.Stop();
                Console.WriteLine("S1: " + sw.ElapsedMilliseconds + "ms");

                // B2: Biến nền tối thành trong suốt (alpha)
                var sw2 = Stopwatch.StartNew();
                await RunProcessAsync(alphaArgs);

                sw2.Stop();
                Console.WriteLine("S2: " + sw2.ElapsedMilliseconds + "ms");

                var sw3 = Stopwatch.StartNew();
                // B3: Overlay chữ vào giữa background
                await RunProcessAsync(finalArgs);

                sw3.Stop();
                Console.WriteLine("S3: " + sw3.ElapsedMilliseconds + "ms");
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

        public async Task CreateVideoFromImage(string imagePath, string outputVideo, int fps = 1, int width = 1280, int height = 720)
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"tmp_1s_{Guid.NewGuid():N}.mp4");
            var args = $"-y -loop 1 -i \"{imagePath}\" -t 1 -vf \"scale={width}:{height},setsar=1\" -r {fps} -c:v libx264 -preset ultrafast -crf 23 -pix_fmt yuv420p \"{tmp}\"";
            await RunProcessAsync(args);

            var durationSeconds = 3600;
            var arguments = $"-y -stream_loop -1 -i \"{tmp}\" -t {durationSeconds} -c copy -movflags +faststart \"{outputVideo}\"";

            await RunProcessAsync(arguments);

            if (File.Exists(tmp))
            {
                File.Delete(tmp);
            }
        }

        public void AddPrefix(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = PathManager.InputOriginVideoPath;
            }
            var mp4Files = Directory.EnumerateFiles(path, "*.mp4").ToList();

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

        public void RemovePrefix(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = PathManager.InputOriginVideoPath;
            }
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

        private async Task RunProcessAsync(string arguments)
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

            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("ERR: " + e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
        }

        private async Task<string> RunProcessWithOutputAsync(string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _ffprobePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Process failed with exit code {process.ExitCode}: {error}");
                }

                return output;
            }
        }

        public bool HasNvidiaGpu()
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
