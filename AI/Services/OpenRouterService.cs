using System.Text;
using System.Text.Json;
using SpaceWarDiscordApp.AI.Models;

namespace SpaceWarDiscordApp.AI.Services;

public class OpenRouterService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public OpenRouterService(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/spacewar-discord-bot");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "SpaceWar Discord Bot");
    }

    public async Task<OpenRouterResponse?> SendRequestAsync(OpenRouterRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OpenRouterResponse>(responseJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            // Log the exception in a real implementation
            Console.WriteLine($"Error calling OpenRouter API: {ex.Message}");
            return null;
        }
    }

    public async Task<List<object>> LoadToolDefinitionsAsync()
    {
        try
        {
            var toolDefinitionsPath = Path.Combine("AI", "ToolDefinitions.json");
            var json = await File.ReadAllTextAsync(toolDefinitionsPath);
            var tools = JsonSerializer.Deserialize<List<object>>(json, _jsonOptions);
            return tools ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading tool definitions: {ex.Message}");
            return [];
        }
    }
} 