namespace WakuPlugin
{
    public class DebugInfoResponse
    {
        public string[] listenAddresses { get; set; } = Array.Empty<string>();
        public string enrUri { get; set; } = string.Empty;
    }
}
