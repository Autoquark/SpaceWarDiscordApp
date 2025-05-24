using SpaceWarDiscordApp.AI.Services;

namespace SpaceWarDiscordApp.AI;

/// <summary>
/// Factory for creating AI services with proper dependency injection
/// </summary>
public static class AIServiceFactory
{
    /// <summary>
    /// Creates a SpaceWarAIService instance with the OpenRouter API key.
    /// Note: You need to add "OpenRouterApiKey" property to your Secrets.cs file.
    /// </summary>
    /// <param name="openRouterApiKey">OpenRouter API key</param>
    /// <returns>Configured AI service</returns>
    public static SpaceWarAIService CreateAIService(string openRouterApiKey)
    {
        var httpClient = new HttpClient();
        var openRouterService = new OpenRouterService(httpClient, openRouterApiKey);
        var commandExecutor = new FixupCommandExecutor();
        
        return new SpaceWarAIService(openRouterService, commandExecutor);
    }

    /// <summary>
    /// Creates a SpaceWarAIService instance using the secrets configuration.
    /// This method would work if you add an OpenRouterApiKey property to Secrets.cs:
    /// 
    /// internal class Secrets
    /// {
    ///     public string FirestoreProjectId { get; set; } = "";
    ///     public string DiscordToken { get; set; } = "";
    ///     public string OpenRouterApiKey { get; set; } = "";
    ///     public ulong TestGuildId { get; set; } = 0;
    /// }
    /// </summary>
    /// <param name="secrets">Secrets configuration object</param>
    /// <returns>Configured AI service</returns>
    public static SpaceWarAIService CreateAIServiceFromSecrets(dynamic secrets)
    {
        // This assumes secrets has an OpenRouterApiKey property
        string apiKey = secrets.OpenRouterApiKey?.ToString() ?? throw new ArgumentException("OpenRouterApiKey not found in secrets");
        return CreateAIService(apiKey);
    }
} 