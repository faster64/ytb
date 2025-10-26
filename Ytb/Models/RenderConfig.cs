namespace Ytb.Models
{
    public class RenderConfig
    {
        public int CurrentRenderDay { get; set; }

        public int MaxRenderDays { get; set; }

        public int NumberOfVideosPerChannelDaily { get; set; }

        public List<int> IgnoreChannels { get; set; } = new();

        public int CCT { get; set; } = 2;
    }

    public class AudioRenderConfig : RenderConfig
    {
        public string CropValue { get; set; }

        public string OverlayValue { get; set; }
    }
}
