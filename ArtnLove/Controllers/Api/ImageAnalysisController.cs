using Microsoft.AspNetCore.Mvc;

namespace ArtnLove.Controllers.Api;

[ApiController]
[Route("api/v1/image-analysis")]
public class ImageAnalysisController : ControllerBase
{
    private readonly ILogger<ImageAnalysisController> _logger;
    private readonly ArtnLove.Services.ImageAnalysisService _analyzer;
    private readonly IHttpClientFactory _httpFactory;

    public ImageAnalysisController(ILogger<ImageAnalysisController> logger, ArtnLove.Services.ImageAnalysisService analyzer, IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _analyzer = analyzer;
        _httpFactory = httpFactory;
    }

    [HttpPost("{artworkId:guid}")]
    public async Task<IActionResult> Analyze(Guid artworkId)
    {
        // This endpoint accepts either multipart/form-data with an image file
        // or application/json with { "imageUrl": "https://..." }.

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            var file = form.Files.FirstOrDefault();
            if (file == null) return BadRequest(new { message = "No file provided" });

            using var stream = file.OpenReadStream();
            var result = await _analyzer.AnalyzeAsync(stream);
            return Ok(result);
        }

        // Try JSON body
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body)) return BadRequest(new { message = "Empty body" });

            var doc = System.Text.Json.JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("imageUrl", out var urlElem)) return BadRequest(new { message = "imageUrl not provided" });
            var url = urlElem.GetString();
            if (string.IsNullOrEmpty(url)) return BadRequest(new { message = "imageUrl is empty" });

            // Download remote image
            var client = _httpFactory.CreateClient();
            using var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return StatusCode((int)resp.StatusCode, new { message = "Failed to fetch image" });
            await using var ms = new MemoryStream();
            await resp.Content.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var result = await _analyzer.AnalyzeAsync(ms);
            return Ok(result);
        }
        catch (System.Text.Json.JsonException)
        {
            return BadRequest(new { message = "Invalid JSON" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image from URL");
            return StatusCode(500, new { message = "Internal error" });
        }
    }
}
