namespace YtSharp.Server.Models
{
    // Model to receive download requests
    public class DownloadRequest
    {
        public required string? Url { get; set; }
        public bool AudioOnly { get; set; }
        public string? Options { get; set; }
        public required string DownloadId { get; set; }
        public required string OutputFolder { get; set; }
    }

    // Model to track download status
    public class DownloadStatus
    {
        public string? Id { get; set; }
        public string? Url { get; set; }
        public string? State { get; set; }
        public double Progress { get; set; }
        public string? DownloadSpeed { get; set; }
        public string? ETA { get; set; }
        public List<string> Output { get; set; } = new List<string>();
        public bool IsCompleted { get; set; }
        public bool IsSuccessful { get; set; }
        public string? FilePath { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

