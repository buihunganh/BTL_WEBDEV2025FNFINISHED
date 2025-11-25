namespace BTL_WEBDEV2025.Models
{
    public class FlowPromptResult
    {
        public string Prompt { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public string? VideoUrl { get; set; }
        public string? RequestId { get; set; }
        public string? Error { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public bool Success => string.IsNullOrWhiteSpace(Error) && !string.IsNullOrWhiteSpace(VideoUrl);
    }
}
