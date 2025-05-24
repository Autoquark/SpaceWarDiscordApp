using System.Text;
using System.Text.Json;
using SpaceWarDiscordApp.AI.Models;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.AI.Services;

public class SpaceWarAIService
{
    private readonly OpenRouterService _openRouterService;
    private readonly FixupCommandExecutor _commandExecutor;

    public SpaceWarAIService(OpenRouterService openRouterService, FixupCommandExecutor commandExecutor)
    {
        _openRouterService = openRouterService;
        _commandExecutor = commandExecutor;
    }

    public async Task<AIAnalysisResult> AnalyzeGameStateAndExecuteActionsAsync(
        Game game, 
        GamePlayer? contextPlayer, 
        string? prompt = null)
    {
        var result = new AIAnalysisResult();
        
        try
        {
            // Build the game state description
            var gameStateDescription = await BuildGameStateDescriptionAsync(game);
            
            // Create the system prompt
            var systemPrompt = BuildSystemPrompt();
            
            // Create the user prompt
            var userPrompt = prompt ?? "Analyze the current game state and suggest strategic adjustments using the available tools.";
            var fullUserPrompt = $"{userPrompt}\n\nCurrent game state:\n{gameStateDescription}";

            // Load tool definitions
            var tools = await _openRouterService.LoadToolDefinitionsAsync();

            // Create the request
            var request = new OpenRouterRequest
            {
                Model = "openai/gpt-4o-mini",
                Messages = 
                [
                    new OpenRouterMessage { Role = "system", Content = systemPrompt },
                    new OpenRouterMessage { Role = "user", Content = fullUserPrompt }
                ],
                Tools = tools,
                ToolChoice = "auto",
                MaxTokens = 2000,
                Temperature = 0.7
            };

            // Send the request to OpenRouter
            var response = await _openRouterService.SendRequestAsync(request);
            if (response?.Choices == null || response.Choices.Count == 0)
            {
                result.Success = false;
                result.Error = "No response from AI service";
                return result;
            }

            var choice = response.Choices[0];
            result.AIResponse = choice.Message.Content;

            // Execute tool calls if any
            if (choice.Message.ToolCalls != null && choice.Message.ToolCalls.Count > 0)
            {
                foreach (var toolCall in choice.Message.ToolCalls)
                {
                    var toolResult = await _commandExecutor.ExecuteToolCallAsync(
                        toolCall.Function.Name,
                        toolCall.Function.Arguments,
                        game,
                        contextPlayer);

                    result.ToolResults.Add(toolResult);
                    
                    if (!toolResult.Success)
                    {
                        result.Success = false;
                        result.Error += $"Tool execution failed: {toolResult.Error}; ";
                    }
                }
            }

            result.Success = result.ToolResults.All(x => x.Success);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    private async Task<string> BuildGameStateDescriptionAsync(Game game)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"Game: {game.Name}");
        sb.AppendLine($"Phase: {game.Phase}");
        sb.AppendLine($"Turn: {game.TurnNumber}");
        sb.AppendLine($"Current Player: {await game.CurrentTurnPlayer.GetNameAsync(false)}");
        sb.AppendLine($"Scoring Token Player: {await game.ScoringTokenPlayer.GetNameAsync(false)}");
        sb.AppendLine();

        // Player information
        sb.AppendLine("Players:");
        foreach (var player in game.Players)
        {
            sb.AppendLine($"- {await player.GetNameAsync(false)} (ID: {player.GamePlayerId}):");
            sb.AppendLine($"  Science: {player.Science}, VP: {player.VictoryPoints}, Stars: {GameStateOperations.GetPlayerStars(game, player)}");
            sb.AppendLine($"  Eliminated: {player.IsEliminated}");
            
            if (player.Techs.Any())
            {
                sb.AppendLine($"  Techs: {string.Join(", ", player.Techs.Select(x => $"{Tech.TechsById[x.TechId].DisplayName}{(x.IsExhausted ? " (exhausted)" : "")}"))}");
            }
        }
        sb.AppendLine();

        // Board state
        sb.AppendLine("Board State:");
        var planetsWithForces = game.Hexes.Where(x => x.Planet != null && x.Planet.ForcesPresent > 0).ToList();
        foreach (var hex in planetsWithForces)
        {
            var planet = hex.Planet!;
            var owner = game.TryGetGamePlayerByGameId(planet.OwningPlayerId);
            var ownerName = owner != null ? await owner.GetNameAsync(false) : "Neutral";
            
            sb.AppendLine($"- {hex.Coordinates}: {planet.ForcesPresent} forces ({ownerName}), {planet.Stars} stars, {planet.Science} science{(planet.IsExhausted ? ", exhausted" : "")}");
        }
        sb.AppendLine();

        // Available techs
        if (game.MarketTechs.Any())
        {
            sb.AppendLine("Market Techs:");
            for (int i = 0; i < game.MarketTechs.Count; i++)
            {
                var tech = Tech.TechsById[game.MarketTechs[i]];
                var cost = 6 - i; // Assuming standard cost structure
                sb.AppendLine($"- {tech.DisplayName} (Cost: {cost}): {tech.Description}");
            }
            sb.AppendLine();
        }

        if (game.UniversalTechs.Any())
        {
            sb.AppendLine($"Universal Techs: {string.Join(", ", game.UniversalTechs.Select(x => Tech.TechsById[x].DisplayName))}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildSystemPrompt()
    {
        return @"You are an AI assistant for the SpaceWar strategy game. Your role is to analyze game states and suggest strategic adjustments using the available fixup commands.

The game is a turn-based space strategy game where players:
- Control forces on planets
- Collect science and victory points
- Purchase and use technologies
- Compete for territorial control and victory conditions

Available tools allow you to:
- Modify force counts and ownership on planets
- Grant or remove technologies from players
- Adjust player resources (science, victory points)
- Change exhaustion states of planets and techs
- Modify turn order

When analyzing, consider:
- Current player positions and relative strength
- Resource distribution and economy
- Technology advantages and synergies
- Victory conditions and paths to win
- Game balance and competitive fairness

Provide thoughtful analysis and use tools judiciously to improve game balance or test strategic scenarios. Explain your reasoning for any modifications you suggest.";
    }
}

public class AIAnalysisResult
{
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
    public string AIResponse { get; set; } = "";
    public List<ToolCallResult> ToolResults { get; set; } = [];
    
    public string GetSummary()
    {
        var sb = new StringBuilder();
        
        if (!string.IsNullOrEmpty(AIResponse))
        {
            sb.AppendLine("AI Analysis:");
            sb.AppendLine(AIResponse);
            sb.AppendLine();
        }

        if (ToolResults.Any())
        {
            sb.AppendLine("Actions Taken:");
            foreach (var result in ToolResults)
            {
                if (result.Success)
                {
                    sb.AppendLine($"✅ {result.Message}");
                }
                else
                {
                    sb.AppendLine($"❌ {result.Error}");
                }
            }
        }

        if (!Success && !string.IsNullOrEmpty(Error))
        {
            sb.AppendLine($"Error: {Error}");
        }

        return sb.ToString();
    }
} 