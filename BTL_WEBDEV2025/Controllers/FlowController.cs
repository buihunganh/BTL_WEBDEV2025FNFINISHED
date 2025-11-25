using System.Net.Mime;
using System.Text.RegularExpressions;
using BTL_WEBDEV2025.Models;
using BTL_WEBDEV2025.Services;
using Microsoft.AspNetCore.Mvc;

namespace BTL_WEBDEV2025.Controllers
{
    public class FlowController : Controller
    {
        private readonly GoogleFlowClient _flowClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FlowController> _logger;

        public FlowController(GoogleFlowClient flowClient, IHttpClientFactory httpClientFactory, IWebHostEnvironment env, ILogger<FlowController> logger)
        {
            _flowClient = flowClient;
            _httpClientFactory = httpClientFactory;
            _env = env;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.FlowConfigured = _flowClient.IsConfigured;
            ViewBag.FlowModel = _flowClient.Model;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RunPrompt([FromBody] FlowPromptRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { message = "Prompt không được để trống." });
            }

            var trimmed = request.Prompt.Trim();
            var result = await _flowClient.GenerateVideoAsync(trimmed, HttpContext.RequestAborted);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> Download(string url, string? prompt)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest("Thiếu đường dẫn tải xuống.");
            }

            if (Uri.TryCreate(url, UriKind.Relative, out _))
            {
                var root = _env.WebRootPath ?? string.Empty;
                var targetPath = Path.Combine(root, url.TrimStart(Path.DirectorySeparatorChar, '/'));
                if (!System.IO.File.Exists(targetPath))
                {
                    return NotFound("Tệp không tồn tại trên máy chủ.");
                }

                var fileName = BuildFileName(prompt ?? "veo3-video");
                return PhysicalFile(targetPath, MediaTypeNames.Application.Octet, fileName);
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest("Đường dẫn tải xuống không hợp lệ.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(uri, HttpContext.RequestAborted);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "Không thể tải video từ máy chủ nguồn.");
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(HttpContext.RequestAborted);
                var contentType = response.Content.Headers.ContentType?.ToString() ?? MediaTypeNames.Application.Octet;
                var fileName = BuildFileName(prompt ?? "veo3-video", response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName);

                return File(bytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải xuống video từ {Url}", url);
                return StatusCode(500, "Có lỗi xảy ra khi tải xuống video.");
            }
        }

        private static string BuildFileName(string prompt, string? fallback = null)
        {
            var baseName = string.IsNullOrWhiteSpace(prompt) ? (fallback ?? "veo3-video") : prompt;
            baseName = Regex.Replace(baseName, "\\s+", "-");
            baseName = Regex.Replace(baseName, "[^a-zA-Z0-9-_]", string.Empty);
            baseName = baseName.Length > 60 ? baseName.Substring(0, 60) : baseName;
            return string.IsNullOrWhiteSpace(baseName) ? (fallback ?? "veo3-video.mp4") : $"{baseName}.mp4";
        }
    }
}
