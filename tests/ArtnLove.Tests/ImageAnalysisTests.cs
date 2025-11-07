using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ArtnLove.Services;

namespace ArtnLove.Tests;

public class ImageAnalysisTests
{
    [Fact]
    public async Task Analyze_SampleWebImage_ReturnsPlaceholderResult()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var logger = new NullLogger<ImageAnalysisService>();
        var service = new ImageAnalysisService(config, logger);

        // Use a small public sample image
        var imageUrl = "https://upload.wikimedia.org/wikipedia/commons/4/47/PNG_transparency_demonstration_1.png";
    using var http = new HttpClient();
    http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36");
    var resp = await http.GetAsync(imageUrl);
        resp.EnsureSuccessStatusCode();
        await using var ms = await resp.Content.ReadAsStreamAsync();

        // Act
        var result = await service.AnalyzeAsync(ms);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Description));
    }
}
