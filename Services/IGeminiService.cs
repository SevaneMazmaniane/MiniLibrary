namespace MiniLibrary.Services;

public interface IGeminiService
{
    Task<string> GenerateBookInsightsAsync(string prompt, CancellationToken cancellationToken = default);
}
