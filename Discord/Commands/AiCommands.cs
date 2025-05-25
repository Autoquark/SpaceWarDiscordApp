using System.ComponentModel;
using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.AI.Models;
using SpaceWarDiscordApp.AI.Services;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Discord.Commands;

/// <summary>
/// Commands for AI-powered game state modifications
/// </summary>
[RequireGameChannel]
public static class AiCommands
{
    [Command("ai")]
    [Description("Use AI to interpret natural language commands and modify the game state")]
    public static async Task ProcessAiRequest(CommandContext context,
        [Description("Natural language description of what you want to do (e.g., 'move all forces from 0,1 to 0,2')")]
        string request)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var openRouterService = context.ServiceProvider.GetRequiredService<OpenRouterService>();

        try
        {
            // Create game context for the AI
            var gameContext = await CreateGameContextAsync(game);
            
            // Get AI suggestions
            var suggestedCommands = await openRouterService.GetFixupCommandsAsync(request, gameContext);
            
            if (suggestedCommands.Count == 0)
            {
                await context.RespondAsync("I couldn't understand how to modify the game state based on your request. Please try being more specific or use the fixup commands directly.");
                return;
            }

            // Execute the suggested commands
            var results = new StringBuilder();
            results.AppendLine($"AI interpretation of: \"{request}\"");
            results.AppendLine($"Executing {suggestedCommands.Count} command(s):");
            results.AppendLine();

            foreach (var command in suggestedCommands)
            {
                try
                {
                    var result = await ExecuteFixupCommandAsync(context, command);
                    results.AppendLine($"✅ {command.FunctionName}: {result}");
                }
                catch (Exception ex)
                {
                    results.AppendLine($"❌ {command.FunctionName}: {ex.Message}");
                }
            }

            await context.RespondAsync(results.ToString());
        }
        catch (Exception ex)
        {
            await context.RespondAsync($"AI request failed: {ex.Message}");
        }
    }

    private static async Task<string> CreateGameContextAsync(Game game)
    {
        var context = new StringBuilder();
        context.AppendLine($"Game: {game.Name}");
        context.AppendLine($"Turn: {game.TurnNumber}");
        context.AppendLine($"Current Player: {await game.CurrentTurnPlayer.GetNameAsync(false)}");
        context.AppendLine($"Phase: {game.Phase}");
        context.AppendLine();

        context.AppendLine("Players:");
        foreach (var player in game.Players)
        {
            var name = await player.GetNameAsync(false);
            context.AppendLine($"- Player {player.GamePlayerId}: {name} (Science: {player.Science}, VP: {player.VictoryPoints})");
        }
        context.AppendLine();

        context.AppendLine("Board:");

        foreach (var hex in game.Hexes)
        {
            var hexDescription = new StringBuilder($"- {hex.Coordinates}: ");
            if (hex.Planet != null)
            {
                var owner = game.TryGetGamePlayerByGameId(hex.Planet!.OwningPlayerId);
                //var ownerName = owner != null ? await owner.GetNameAsync(false) : "Neutral";
                hexDescription.Append($"Planet: Production: {hex.Planet.Production}, Science: {hex.Planet.Science}, Exhausted: {hex.Planet.IsExhausted}, Forces: {hex.Planet.ForcesPresent}, OwnerId: {hex.Planet.OwningPlayerId}");
            }
            else
            {
                hexDescription.Append("No planet");
            }
            
            context.AppendLine(hexDescription.ToString());
        }


        return context.ToString();
    }

    private static async Task<string> ExecuteFixupCommandAsync(CommandContext context, FixupCommandCall command)
    {
        return command switch
        {
            SetForcesCall setForces => await ExecuteSetForcesAsync(context, setForces),
            GrantTechCall grantTech => await ExecuteGrantTechAsync(context, grantTech),
            RemoveTechCall removeTech => await ExecuteRemoveTechAsync(context, removeTech),
            SetTechExhaustedCall setTechExhausted => await ExecuteSetTechExhaustedAsync(context, setTechExhausted),
            SetPlanetExhaustedCall setPlanetExhausted => await ExecuteSetPlanetExhaustedAsync(context, setPlanetExhausted),
            SetPlayerTurnCall setPlayerTurn => await ExecuteSetPlayerTurnAsync(context, setPlayerTurn),
            SetPlayerScienceCall setPlayerScience => await ExecuteSetPlayerScienceAsync(context, setPlayerScience),
            SetPlayerVictoryPointsCall setPlayerVp => await ExecuteSetPlayerVpAsync(context, setPlayerVp),
            _ => throw new ArgumentException($"Unknown command type: {command.GetType().Name}")
        };
    }

    private static async Task<string> ExecuteSetForcesAsync(CommandContext context, SetForcesCall command)
    {
        await FixupCommands.SetForces(context, command.Coordinates, command.Amount, command.Player);
        return $"Set forces at {command.Coordinates} to {command.Amount} for player {command.Player}";
    }

    private static async Task<string> ExecuteGrantTechAsync(CommandContext context, GrantTechCall command)
    {
        await FixupCommands.GrantTech(context, command.TechId, command.Player);
        return $"Granted tech {command.TechId} to player {command.Player}";
    }

    private static async Task<string> ExecuteRemoveTechAsync(CommandContext context, RemoveTechCall command)
    {
        await FixupCommands.RemoveTech(context, command.TechId, command.Player);
        return $"Removed tech {command.TechId} from player {command.Player}";
    }

    private static async Task<string> ExecuteSetTechExhaustedAsync(CommandContext context, SetTechExhaustedCall command)
    {
        await FixupCommands.SetTechExhausted(context, command.TechId, command.Player, command.Exhausted);
        return $"Set tech {command.TechId} exhausted state to {command.Exhausted} for player {command.Player}";
    }

    private static async Task<string> ExecuteSetPlanetExhaustedAsync(CommandContext context, SetPlanetExhaustedCall command)
    {
        await FixupCommands.SetPlanetExhausted(context, command.Coordinates, command.Exhausted);
        return $"Set planet at {command.Coordinates} exhausted state to {command.Exhausted}";
    }

    private static async Task<string> ExecuteSetPlayerTurnAsync(CommandContext context, SetPlayerTurnCall command)
    {
        await FixupCommands.SetCurrentTurn(context, command.Player);
        return $"Set current turn to player {command.Player}";
    }

    private static async Task<string> ExecuteSetPlayerScienceAsync(CommandContext context, SetPlayerScienceCall command)
    {
        await FixupCommands.SetPlayerScience(context, command.Science, command.Player);
        return $"Set science to {command.Science} for player {command.Player}";
    }

    private static async Task<string> ExecuteSetPlayerVpAsync(CommandContext context, SetPlayerVictoryPointsCall command)
    {
        await FixupCommands.SetPlayerVictoryPoints(context, command.Vp, command.Player);
        return $"Set Victory Points to {command.Vp} for player {command.Player}";
    }
} 