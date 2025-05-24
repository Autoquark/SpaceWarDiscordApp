using System.Text;
using System.Text.Json;
using SpaceWarDiscordApp.AI.Models;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.AI.Services;

/// <summary>
/// Service for making API calls to OpenRouter for AI-powered game state modifications
/// </summary>
public class OpenRouterService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly List<OpenRouterTool> _fixupTools;

    public OpenRouterService(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _fixupTools = LoadFixupTools();
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/SpaceWarDiscordBot");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "SpaceWar Discord Bot");
    }

    /// <summary>
    /// Sends a natural language request to the AI and returns suggested fixup commands
    /// </summary>
    public async Task<List<FixupCommandCall>> GetFixupCommandsAsync(string userRequest, string gameContext)
    {
        var systemPrompt = CreateSystemPrompt(gameContext);
        
        var request = new OpenRouterRequest
        {
            Messages =
            [
                new OpenRouterMessage { Role = "system", Content = systemPrompt },
                new OpenRouterMessage { Role = "user", Content = userRequest }
            ],
            Tools = _fixupTools,
            ToolChoice = "auto",
            Temperature = 0.3, // Lower temperature for more consistent results
            MaxTokens = 2000
        };

        var jsonRequest = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenRouter API call failed: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var openRouterResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseContent);

        if (openRouterResponse?.Error != null)
        {
            throw new Exception($"OpenRouter API error: {openRouterResponse.Error.Message}");
        }

        if (openRouterResponse?.Choices == null || openRouterResponse.Choices.Count == 0)
        {
            throw new Exception("No response choices received from OpenRouter API");
        }

        var toolCalls = openRouterResponse.Choices[0].Message.ToolCalls;
        if (toolCalls == null || toolCalls.Count == 0)
        {
            // AI didn't suggest any tool calls
            return [];
        }

        return ParseToolCalls(toolCalls);
    }

    private string CreateSystemPrompt(string gameContext)
    {
        return $"""
            You are an AI assistant for a Discord-based space strategy game called SpaceWar. Your role is to interpret natural language commands from players and convert them into specific game state modifications using the available fixup tools.

            Game Context:
            {gameContext}

            Available Tools:
            - setForces: Modify the number of military units on a planet
            - grantTech/removeTech: Add or remove technologies from players
            - setTechExhausted/setPlanetExhausted: Change exhausted state of techs/planets
            - setPlayerTurn: Change whose turn it is
            - setPlayerScience: Modify a player's science points
            - setPlayerVictoryPoints: Modify a player's victory points

            Guidelines:
            1. Hex coordinates are in format "q,r" (e.g., "0,1", "-1,2")
            2. Player IDs are integers, use -1 for current user if not specified
            3. Only suggest actions that make sense within the game context
            4. If a request involves moving forces, use setForces to remove from source and add to destination
            5. Be conservative - don't make major changes unless clearly requested
            6. If the request is unclear or impossible, don't suggest any tool calls

            Interpret the user's request and determine which tools to call to achieve their desired game state changes.
            """;
    }

    private List<OpenRouterTool> LoadFixupTools()
    {
        var toolsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AI", "Tools", "FixupToolDefinitions.json");
        var toolsJson = File.ReadAllText(toolsPath);
        
        using var document = JsonDocument.Parse(toolsJson);
        var tools = new List<OpenRouterTool>();
        
        foreach (var toolElement in document.RootElement.EnumerateArray())
        {
            var functionElement = toolElement.GetProperty("function");
            
            var tool = new OpenRouterTool
            {
                Type = toolElement.GetProperty("type").GetString() ?? "function",
                Function = new OpenRouterFunction
                {
                    Name = functionElement.GetProperty("name").GetString() ?? "",
                    Description = functionElement.GetProperty("description").GetString() ?? "",
                    Parameters = JsonSerializer.Deserialize<object>(functionElement.GetProperty("parameters").GetRawText()) ?? new()
                }
            };
            
            tools.Add(tool);
        }

        return tools;
    }

    private List<FixupCommandCall> ParseToolCalls(List<OpenRouterToolCall> toolCalls)
    {
        var commands = new List<FixupCommandCall>();

        foreach (var toolCall in toolCalls)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(toolCall.Function.Arguments);
                if (arguments == null) continue;

                var command = toolCall.Function.Name switch
                {
                    "setForces" => ParseSetForcesCall(arguments),
                    "grantTech" => ParseGrantTechCall(arguments),
                    "removeTech" => ParseRemoveTechCall(arguments),
                    "setTechExhausted" => ParseSetTechExhaustedCall(arguments),
                    "setPlanetExhausted" => ParseSetPlanetExhaustedCall(arguments),
                    "setPlayerTurn" => ParseSetPlayerTurnCall(arguments),
                    "setPlayerScience" => ParseSetPlayerScienceCall(arguments),
                    "setPlayerVictoryPoints" => ParseSetPlayerVictoryPointsCall(arguments),
                    _ => null
                };

                if (command != null)
                {
                    commands.Add(command);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse tool call {toolCall.Function.Name}: {ex.Message}");
                // Continue processing other tool calls
            }
        }

        return commands;
    }

    private SetForcesCall ParseSetForcesCall(Dictionary<string, JsonElement> args)
    {
        var coordinatesStr = args["coordinates"].GetString() ?? "";
        if (!HexCoordinates.TryParse(coordinatesStr, out var coordinates))
        {
            throw new ArgumentException($"Invalid coordinates: {coordinatesStr}");
        }

        return new SetForcesCall
        {
            Coordinates = coordinates,
            Amount = args.TryGetValue("amount", out var amount) ? amount.GetInt32() : -1,
            Player = args.TryGetValue("player", out var player) ? player.GetInt32() : -1
        };
    }

    private GrantTechCall ParseGrantTechCall(Dictionary<string, JsonElement> args)
    {
        return new GrantTechCall
        {
            TechId = args["techId"].GetString() ?? "",
            Player = args.TryGetValue("player", out var player) ? player.GetInt32() : -1
        };
    }

    private RemoveTechCall ParseRemoveTechCall(Dictionary<string, JsonElement> args)
    {
        return new RemoveTechCall
        {
            TechId = args["techId"].GetString() ?? "",
            Player = args.TryGetValue("player", out var player) ? player.GetInt32() : -1
        };
    }

    private SetTechExhaustedCall ParseSetTechExhaustedCall(Dictionary<string, JsonElement> args)
    {
        return new SetTechExhaustedCall
        {
            TechId = args["techId"].GetString() ?? "",
            Player = args.TryGetValue("player", out var player) ? player.GetInt32() : -1,
            Exhausted = args.TryGetValue("exhausted", out var exhausted) ? exhausted.GetBoolean() : true
        };
    }

    private SetPlanetExhaustedCall ParseSetPlanetExhaustedCall(Dictionary<string, JsonElement> args)
    {
        var coordinatesStr = args["coordinates"].GetString() ?? "";
        if (!HexCoordinates.TryParse(coordinatesStr, out var coordinates))
        {
            throw new ArgumentException($"Invalid coordinates: {coordinatesStr}");
        }

        return new SetPlanetExhaustedCall
        {
            Coordinates = coordinates,
            Exhausted = args.TryGetValue("exhausted", out var exhausted) ? exhausted.GetBoolean() : true
        };
    }

    private SetPlayerTurnCall ParseSetPlayerTurnCall(Dictionary<string, JsonElement> args)
    {
        return new SetPlayerTurnCall
        {
            Player = args.TryGetValue("player", out var player) ? player.GetInt32() : -1
        };
    }

    private SetPlayerScienceCall ParseSetPlayerScienceCall(Dictionary<string, JsonElement> args)
    {
        return new SetPlayerScienceCall
        {
            Science = args["science"].GetInt32(),
            Player = args.TryGetValue("player", out var player) ? player.GetInt32() : -1
        };
    }

    private SetPlayerVictoryPointsCall ParseSetPlayerVictoryPointsCall(Dictionary<string, JsonElement> args)
    {
        return new SetPlayerVictoryPointsCall
        {
            Vp = args["vp"].GetInt32(),
            Player = args.TryGetValue("player", out var player) ? player.GetInt32() : -1
        };
    }
} 