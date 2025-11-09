namespace Ytb.Models
{
    public class Config
    {
        public int CutDuration { get; set; }

        public string ApiKey { get; set; }

        public bool AutoUpdateYtDlp { get; set; }

        public OlderRenderConfig OlderConfig { get; set; }

        public RenderConfig LineConfig { get; set; }
    }
}
