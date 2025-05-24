using System.Text.Json.Serialization;

namespace SpaceWarDiscordApp.AI.Models;

public class OpenRouterRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "openai/gpt-4o-mini";

    [JsonPropertyName("messages")]
    public List<OpenRouterMessage> Messages { get; set; } = [];

    [JsonPropertyName("tools")]
    public List<object>? Tools { get; set; }

    [JsonPropertyName("tool_choice")]
    public string? ToolChoice { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
}

public class OpenRouterMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }

    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class OpenRouterResponse
{
    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public Usage? Usage { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")]
    public OpenRouterMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public FunctionCall Function { get; set; } = new();
}

public class FunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "";
}

public class Usage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
} 