using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace Ytb.Services
{
    public class ChannelService
    {
        public async Task GetVideoUrlsAsync(string handle)
        {
            handle = handle.Trim();

            var apiKey = GetApiKey();
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "Ytb"
            });

            // B1: lấy channelId từ handle
            var channelsRequest = youtubeService.Channels.List("id,contentDetails");
            channelsRequest.ForHandle = handle;
            var channelsResponse = await channelsRequest.ExecuteAsync();

            if (channelsResponse.Items == null || channelsResponse.Items.Count == 0)
            {
                Console.WriteLine($"Không tìm thấy channel cho handle: {handle}");
                return;
            }

            var channel = channelsResponse.Items[0];
            var channelId = channel.Id;
            var uploadsPlaylistId = channel.ContentDetails.RelatedPlaylists.Uploads;

            Console.WriteLine($"Channel ID: {channelId}");
            Console.WriteLine($"Uploads Playlist ID: {uploadsPlaylistId}");
            Console.WriteLine("Đang lấy urls...");

            // B2: duyệt qua playlist uploads để lấy tất cả video
            var nextPageToken = "";
            var directoryPath = Path.Combine(PathManager.ChannelsPath, handle.TrimStart('@'));
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var filePath = Path.Combine(directoryPath, "only_video_urls.txt");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            else
            {
                File.Create(filePath).Close();
            }

            var filePath2 = Path.Combine(directoryPath, "video_infos.txt");
            if (File.Exists(filePath2))
            {
                File.Delete(filePath2);
            }
            else
            {
                File.Create(filePath2).Close();
            }

            do
            {
                var playlistItemsRequest = youtubeService.PlaylistItems.List("snippet");
                playlistItemsRequest.PlaylistId = uploadsPlaylistId;
                playlistItemsRequest.MaxResults = 50;
                playlistItemsRequest.PageToken = nextPageToken;

                var playlistItemsResponse = await playlistItemsRequest.ExecuteAsync();
                foreach (var item in playlistItemsResponse.Items)
                {
                    var vid = item.Snippet.ResourceId.VideoId;
                    var videoUrl = $"https://www.youtube.com/watch?v={vid}";

                    File.AppendAllText(filePath, videoUrl + Environment.NewLine);
                    File.AppendAllText(filePath2, videoUrl + "\t" + item.Snippet.PublishedAtRaw + "\t" + item.Snippet.Title + Environment.NewLine);

                    Console.WriteLine(videoUrl);
                }

                nextPageToken = playlistItemsResponse.NextPageToken;
            }
            while (nextPageToken != null);

            Console.WriteLine("\nHoàn thành!");
        }

        private string GetApiKey()
        {
            var path = PathManager.ResourcesFileApiKeyPath;
            if (File.Exists(path))
            {
                var key = File.ReadAllText(path);
                if (!string.IsNullOrEmpty(key))
                {
                    return key.Trim();
                }
                throw new Exception("File api_key.txt rỗng. Vui lòng cập nhật API Key.");
            }
            throw new FileNotFoundException("Không tìm thấy file api_key.txt");
        }
    }
}
