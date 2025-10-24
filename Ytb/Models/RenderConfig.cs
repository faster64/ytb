namespace Ytb.Models
{
    public class RenderConfig
    {
        public int CurrentRenderDay { get; set; }

        public int MaxRenderDays { get; set; }

        public int NumberOfVideosPerChannelDaily { get; set; }

        public int CCT { get; set; }
    }

    public class AudioRenderConfig : RenderConfig
    {
        public string CropValue { get; set; }

        public string OverlayValue { get; set; }
    }
}
