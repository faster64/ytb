using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace Ytb.Services
{
    public class ChannelService
    {
        public async Task GetVideoUrlsAsync(string handle)
        {
            handle = handle.Trim();

            var configService = new ConfigService();
            var apiKey = configService.GetApiKey();
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
                var url = $"https://www.youtube.com/watch?v={video.Id}";
                File.AppendAllText(filePath, url + Environment.NewLine);

                File.AppendAllText(filePath2,
                    $"{video.PublishedAt}\t{url}\t{video.ViewCount}\t{video.Title}\t{video.Duration}{Environment.NewLine}");
            }

            Console.WriteLine("\nHoàn thành!");
        }
    }
}
