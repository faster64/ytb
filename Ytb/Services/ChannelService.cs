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
            Console.WriteLine("Đang lấy urls...");

            var directoryPath = Path.Combine(PathManager.ChannelsPath, handle.TrimStart('@'));
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var filePath = Path.Combine(directoryPath, "only_video_urls.txt");
            if (File.Exists(filePath)) File.Delete(filePath);

            var filePath2 = Path.Combine(directoryPath, "video_infos.txt");
            if (File.Exists(filePath2)) File.Delete(filePath2);

            string nextPageToken = null;

            do
            {
                var playlistItemsRequest = youtubeService.PlaylistItems.List("snippet");
                playlistItemsRequest.PlaylistId = uploadsPlaylistId;
                playlistItemsRequest.MaxResults = 50;
                playlistItemsRequest.PageToken = nextPageToken;

                var playlistItemsResponse = await playlistItemsRequest.ExecuteAsync();

                // gom videoId lại để query 1 lần
                var videoIds = playlistItemsResponse.Items
                    .Select(i => i.Snippet.ResourceId.VideoId)
                    .ToList();

                if (videoIds.Count > 0)
                {
                    var videosRequest = youtubeService.Videos.List("contentDetails,snippet");
                    videosRequest.Id = string.Join(",", videoIds);

                    var videosResponse = await videosRequest.ExecuteAsync();

                    foreach (var video in videosResponse.Items)
                    {
                        // parse duration dạng PT#M#S
                        var duration = System.Xml.XmlConvert.ToTimeSpan(video.ContentDetails.Duration);
                        if (duration.TotalSeconds <= 600)
                        {
                            // Bỏ short
                            continue;
                        }

                        var videoUrl = $"https://www.youtube.com/watch?v={video.Id}";
                        File.AppendAllText(filePath, videoUrl + Environment.NewLine);

                        File.AppendAllText(filePath2,
                            $"{videoUrl}\t{duration}\t{video.Snippet.PublishedAtRaw}\t{video.Snippet.Title}{Environment.NewLine}");

                        Console.WriteLine(videoUrl);
                    }
                }

                nextPageToken = playlistItemsResponse.NextPageToken;
            }
            while (nextPageToken != null);

            Console.WriteLine("\nHoàn thành!");
        }
    }
}
