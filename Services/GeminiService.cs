using System.Net.Http.Json;
using System.Text.Json;

namespace MiniLibrary.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> GenerateBookInsightsAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "Gemini API key is not configured. Set Gemini:ApiKey in appsettings or GEMINI_API_KEY env var.";
        }

        var model = "models/gemini-2.0-flash"; // or models/gemini-2.5-flash
        var url = $"https://generativelanguage.googleapis.com/v1/{model}:generateContent?key={apiKey}";
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return $"Gemini request failed: {(int)response.StatusCode} - {error}";
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return string.IsNullOrWhiteSpace(text) ? "Gemini returned an empty response." : text;
    }
}
