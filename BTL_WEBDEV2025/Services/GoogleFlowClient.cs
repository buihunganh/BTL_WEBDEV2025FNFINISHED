using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BTL_WEBDEV2025.Models;

namespace BTL_WEBDEV2025.Services
{
    public class GoogleFlowClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleFlowClient> _logger;
        private readonly string? _baseUrl;
        private readonly string? _apiKey;
        private readonly string? _flowId;
        private readonly string _model;

        public GoogleFlowClient(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleFlowClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["GoogleFlow:BaseUrl"];
            _apiKey = configuration["GoogleFlow:ApiKey"];
            _flowId = configuration["GoogleFlow:FlowId"];
            _model = configuration["GoogleFlow:Model"] ?? "veo-3";

            var timeout = configuration.GetValue<int?>("GoogleFlow:TimeoutSeconds") ?? 180;
            _httpClient.Timeout = TimeSpan.FromSeconds(timeout);
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl)
            && !string.IsNullOrWhiteSpace(_apiKey)
            && !string.IsNullOrWhiteSpace(_flowId);

        public string Model => _model;

        public async Task<FlowPromptResult> GenerateVideoAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var result = new FlowPromptResult
            {
                Prompt = prompt,
                StartedAt = DateTimeOffset.UtcNow,
                RequestId = $"req-{Guid.NewGuid():N}"
            };

            if (!IsConfigured)
            {
                result.VideoUrl = $"https://storage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4?prompt={Uri.EscapeDataString(prompt)}";
                result.Status = "mock";
                result.CompletedAt = DateTimeOffset.UtcNow;
                return result;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, BuildEndpoint("flows:run"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var payload = new
                {
                    flow_id = _flowId,
                    prompt,
                    model = _model,
                    output_format = "mp4"
                };

                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    result.Error = $"Flow API trả về {(int)response.StatusCode}: {response.ReasonPhrase}. {responseBody}".Trim();
                    result.Status = "failed";
                    return result;
                }

                using var doc = JsonDocument.Parse(responseBody);
                result.VideoUrl = TryExtractVideoUrl(doc.RootElement);
                result.Status = "completed";
                result.CompletedAt = DateTimeOffset.UtcNow;

                if (string.IsNullOrWhiteSpace(result.VideoUrl))
                {
                    result.Error = "Flow API không trả về videoUrl hợp lệ.";
                    result.Status = "failed";
                }

                return result;
            }
            catch (TaskCanceledException ex)
            {
                result.Error = "Flow API mất quá nhiều thời gian (timeout).";
                result.Status = "failed";
                _logger.LogWarning(ex, "Flow API timeout for prompt {Prompt}", prompt);
            }
            catch (Exception ex)
            {
                result.Error = "Không thể gửi prompt tới Google Flow.";
                result.Status = "failed";
                _logger.LogError(ex, "Flow API error for prompt {Prompt}", prompt);
            }

            return result;
        }

        private string BuildEndpoint(string path)
        {
            var trimmed = _baseUrl?.TrimEnd('/') ?? string.Empty;
            return string.IsNullOrEmpty(trimmed) ? path : $"{trimmed}/{path}";
        }

        private static string? TryExtractVideoUrl(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Undefined || element.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (element.TryGetProperty("videoUrl", out var videoProp) && videoProp.ValueKind == JsonValueKind.String)
            {
                return videoProp.GetString();
            }

            if (element.TryGetProperty("result", out var resultProp))
            {
                var nestedUrl = TryExtractVideoUrl(resultProp);
                if (!string.IsNullOrWhiteSpace(nestedUrl))
                {
                    return nestedUrl;
                }
            }

            if (element.TryGetProperty("data", out var dataProp))
            {
                if (dataProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var child in dataProp.EnumerateArray())
                    {
                        var nestedUrl = TryExtractVideoUrl(child);
                        if (!string.IsNullOrWhiteSpace(nestedUrl))
                        {
                            return nestedUrl;
                        }
                    }
                }
                else
                {
                    var nestedUrl = TryExtractVideoUrl(dataProp);
                    if (!string.IsNullOrWhiteSpace(nestedUrl))
                    {
                        return nestedUrl;
                    }
                }
            }

            return null;
        }
    }
}
