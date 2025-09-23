using FFMpegCore;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ytb.FFmpeg
{
    //nuget TqkLibrary.FFmpeg.Runtimes, hoặc xài ffmpeg chạy trên ubuntu
    public class FFmepgHandler
    {
        private readonly string _ffmpegDir;
        private readonly string _fileName;

        public FFmepgHandler()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _ffmpegDir = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native");
                _fileName = Path.Combine(_ffmpegDir, "ffmpeg.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _ffmpegDir = "/usr/bin/ffmpeg";
                _fileName = "/usr/bin/ffmpeg";
            }
            else
            {
                throw new PlatformNotSupportedException("OS Platform is not supported");
            }

            GlobalFFOptions.Current.BinaryFolder = _ffmpegDir;
        }

        public async Task ConvertToHlsAsync(string inputPath, string outputPath, string fileName)
        {
            var arguments = $"-i \"{inputPath}\" -c:v libx264 -b:v 1M -hls_time 10 -hls_list_size 0 -hls_segment_filename \"{outputPath}\\{fileName}%04d.ts\" \"{outputPath}\\{fileName}.m3u8\"";
            await ExecuteFFmpegProcess(arguments, $"{outputPath}\\{fileName}.m3u8");
        }

        public async Task ResizeImageAsync(string inputPath, string outputPath, int width = 240, int height = 240)
        {
            var arguments = $"-i \"{inputPath}\" -vf scale={width}:{height} \"{outputPath}\"";
            await ExecuteFFmpegProcess(arguments, outputPath);
        }

        public async Task AddWatermarkAsync(string inputPath, string watermarkPath, string outputPath, int x = 10, int y = 10)
        {
            var arguments = $"-i \"{inputPath}\" -i \"{watermarkPath}\" -filter_complex \"overlay=W-w-{x}:{y}\" \"{outputPath}\"";
            // var centerArguments = $"-i \"{inputPath}\" -i \"{watermarkPath}\" -filter_complex \"overlay=(main_w-overlay_w)/2:(main_h-overlay_h)/2\" \"{outputPath}\"";
            await ExecuteFFmpegProcess(arguments, outputPath);
        }

        #region Core
        private async Task ExecuteFFmpegProcess(string arguments, string outputPath)
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            _ = ConsumeReader(process.StandardOutput);
            _ = ConsumeReader(process.StandardError);

            var sw = Stopwatch.StartNew();
            Console.WriteLine("Calling WaitForExitAsync()...");

            await process.WaitForExitAsync();

            sw.Stop();
            Console.WriteLine("Process has exited. Exit code: {ExitCode} {ProcessTime}ms", process.ExitCode, sw.ElapsedMilliseconds);
        }

        private async static Task ConsumeReader(TextReader reader)
        {
            string text;

            while ((text = await reader.ReadLineAsync()) != null)
            {
                Console.WriteLine(text);
            }
        }
        #endregion
    }
}
