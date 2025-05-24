using System.Text.Json;
using SpaceWarDiscordApp.AI.Models;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.AI.Services;

public class FixupCommandExecutor
{
    private readonly JsonSerializerOptions _jsonOptions;

    public FixupCommandExecutor()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ToolCallResult> ExecuteToolCallAsync(string toolName, string argumentsJson, Game game, GamePlayer? contextPlayer)
    {
        try
        {
            return toolName switch
            {
                "setForces" => await ExecuteSetForcesAsync(argumentsJson, game, contextPlayer),
                "grantTech" => await ExecuteGrantTechAsync(argumentsJson, game, contextPlayer),
                "removeTech" => await ExecuteRemoveTechAsync(argumentsJson, game, contextPlayer),
                "setTechExhausted" => await ExecuteSetTechExhaustedAsync(argumentsJson, game, contextPlayer),
                "setPlanetExhausted" => await ExecuteSetPlanetExhaustedAsync(argumentsJson, game, contextPlayer),
                "setPlayerTurn" => await ExecuteSetPlayerTurnAsync(argumentsJson, game, contextPlayer),
                "setPlayerScience" => await ExecuteSetPlayerScienceAsync(argumentsJson, game, contextPlayer),
                "setPlayerVictoryPoints" => await ExecuteSetPlayerVictoryPointsAsync(argumentsJson, game, contextPlayer),
                _ => new ToolCallResult { Success = false, Error = $"Unknown tool: {toolName}" }
            };
        }
        catch (Exception ex)
        {
            return new ToolCallResult { Success = false, Error = ex.Message };
        }
    }

    private async Task<ToolCallResult> ExecuteSetForcesAsync(string argumentsJson, Game game, GamePlayer? contextPlayer)
    {
        var parameters = JsonSerializer.Deserialize<SetForcesParameters>(argumentsJson, _jsonOptions);
        if (parameters == null)
            return new ToolCallResult { Success = false, Error = "Invalid parameters" };

        try
        {
            var coordinates = parameters.GetHexCoordinates();
            var hex = game.GetHexAt(coordinates);
            
            if (hex.Planet == null)
                return new ToolCallResult { Success = false, Error = $"Invalid coordinates {coordinates}" };

            var newAmount = parameters.Amount > -1 ? parameters.Amount : hex.Planet.ForcesPresent;
            GamePlayer? newOwner = null;

            if (newAmount > 0)
            {
                newOwner = game.TryGetGamePlayerByGameId(parameters.Player) ?? 
                          game.TryGetGamePlayerByGameId(hex.Planet.OwningPlayerId) ?? 
                          contextPlayer;
                
                if (newOwner == null)
                    return new ToolCallResult { Success = false, Error = "Must specify a player" };
            }

            hex.Planet.ForcesPresent = newAmount;
            hex.Planet.OwningPlayerId = newOwner?.GamePlayerId ?? 0;

            await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));

            var message = newOwner != null 
                ? $"Set forces at {coordinates} to {newOwner.PlayerColourInfo.GetDieEmoji(hex.Planet.ForcesPresent)}"
                : $"Removed all forces from {coordinates}";

            return new ToolCallResult { Success = true, Message = message };
        }
        catch (Exception ex)
        {
            return new ToolCallResult { Success = false, Error = ex.Message };
        }
    }

    private async Task<ToolCallResult> ExecuteGrantTechAsync(string argumentsJson, Game game, GamePlayer? contextPlayer)
    {
        var parameters = JsonSerializer.Deserialize<GrantTechParameters>(argumentsJson, _jsonOptions);
        if (parameters == null)
            return new ToolCallResult { Success = false, Error = "Invalid parameters" };

        var gamePlayer = parameters.Player == -1 ? contextPlayer : game.TryGetGamePlayerByGameId(parameters.Player);
        if (gamePlayer == null)
            return new ToolCallResult { Success = false, Error = "Unknown player" };

        if (gamePlayer.Techs.Any(x => x.TechId == parameters.TechId))
            return new ToolCallResult { Success = false, Error = "Player already has that tech" };

        if (!Tech.TechsById.TryGetValue(parameters.TechId, out var tech))
            return new ToolCallResult { Success = false, Error = "Unknown tech" };

        gamePlayer.Techs.Add(tech.CreatePlayerTech(game, gamePlayer));

        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));

        return new ToolCallResult 
        { 
            Success = true, 
            Message = $"Granted {tech.DisplayName} to {await gamePlayer.GetNameAsync(false)}" 
        };
    }

    private async Task<ToolCallResult> ExecuteRemoveTechAsync(string argumentsJson, Game game, GamePlayer? contextPlayer)
    {
        var parameters = JsonSerializer.Deserialize<RemoveTechParameters>(argumentsJson, _jsonOptions);
        if (parameters == null)
            return new ToolCallResult { Success = false, Error = "Invalid parameters" };

        var gamePlayer = parameters.Player == -1 ? contextPlayer : game.TryGetGamePlayerByGameId(parameters.Player);
        if (gamePlayer == null)
            return new ToolCallResult { Success = false, Error = "Unknown player" };

        if (!Tech.TechsById.TryGetValue(parameters.TechId, out var tech))
            return new ToolCallResult { Success = false, Error = "Unknown tech" };

        var index = gamePlayer.Techs.Items.Index().FirstOrDefault(x => x.Item.TechId == parameters.TechId, (-1, null!)).Index;
        if (index == -1)
            return new ToolCallResult { Success = false, Error = "Player does not have that tech" };

        gamePlayer.Techs.RemoveAt(index);

        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));

        return new ToolCallResult 
        { 
            Success = true, 
            Message = $"Removed {tech.DisplayName} from {await gamePlayer.GetNameAsync(false)}" 
        };
    }

    private async Task<ToolCallResult> ExecuteSetTechExhaustedAsync(string argumentsJson, Game game, GamePlayer? contextPlayer)
    {
        var parameters = JsonSerializer.Deserialize<SetTechExhaustedParameters>(argumentsJson, _jsonOptions);
        if (parameters == null)
            return new ToolCallResult { Success = false, Error = "Invalid parameters" };

        var gamePlayer = parameters.Player == -1 ? contextPlayer : game.TryGetGamePlayerByGameId(parameters.Player);
        if (gamePlayer == null)
            return new ToolCallResult { Success = false, Error = "Unknown player" };

        if (!Tech.TechsById.TryGetValue(parameters.TechId, out var tech))
            return new ToolCallResult { Success = false, Error = "Unknown tech" };

        var playerTech = gamePlayer.TryGetPlayerTechById(parameters.TechId);
        if (playerTech == null)
            return new ToolCallResult { Success = false, Error = "Player does not have that tech" };

        playerTech.IsExhausted = parameters.Exhausted;

        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));

        return new ToolCallResult 
        { 
            Success = true, 
            Message = $"{(parameters.Exhausted ? "Exhausted" : "Unexhausted")} {tech.DisplayName} for {await gamePlayer.GetNameAsync(false)}" 
        };
    }

    private async Task<ToolCallResult> ExecuteSetPlanetExhaustedAsync(string argumentsJson, Game game, GamePlayer? contextPlayer)
    {
        var parameters = JsonSerializer.Deserialize<SetPlanetExhaustedParameters>(argumentsJson, _jsonOptions);
        if (parameters == null)
            return new ToolCallResult { Success = false, Error = "Invalid parameters" };

        try
        {
            var coordinates = parameters.GetHexCoordinates();
            var hex = game.GetHexAt(coordinates);
            
            if (hex.Planet == null)
                return new ToolCallResult { Success = false, Error = $"Invalid coordinates {coordinates}" };

            hex.Planet.IsExhausted = parameters.Exhausted;

            await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));

            return new ToolCallResult 
            { 
                Success = true, 
                Message = $"{(parameters.Exhausted ? "Exhausted" : "Unexhausted")} planet at {coordinates}" 
            };
        }
        catch (Exception ex)
        {
            return new ToolCallResult { Success = false, Error = ex.Message };
        }
    }

    private async Task<ToolCallResult> ExecuteSetPlayerTurnAsync(string argumentsJson, Game game, GamePlayer? contextPlayer)
    {
        var parameters = JsonSerializer.Deserialize<SetPlayerTurnParameters>(argumentsJson, _jsonOptions);
        if (parameters == null)
            return new ToolCallResult { Success = false, Error = "Invalid parameters" };

        var gamePlayer = parameters.Player == -1 ? contextPlayer : game.TryGetGamePlayerByGameId(parameters.Player);
        if (gamePlayer == null)
            return new ToolCallResult { Success = false, Error = "Unknown player" };

        var previousPlayer = game.CurrentTurnPlayer;
        game.CurrentTurnPlayerIndex = game.Players.FindIndex(x => x.GamePlayerId == gamePlayer.GamePlayerId);

        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));

        return new ToolCallResult 
        { 
            Success = true, 
            Message = $"Set current turn to {await gamePlayer.GetNameAsync(false)} (was {await previousPlayer.GetNameAsync(false)})" 
        };
    }

    private async Task<ToolCallResult> ExecuteSetPlayerScienceAsync(string argumentsJson, Game game, GamePlayer? contextPlayer)
    {
        var parameters = JsonSerializer.Deserialize<SetPlayerScienceParameters>(argumentsJson, _jsonOptions);
        if (parameters == null)
            return new ToolCallResult { Success = false, Error = "Invalid parameters" };

        var gamePlayer = parameters.Player == -1 ? contextPlayer : game.TryGetGamePlayerByGameId(parameters.Player);
        if (gamePlayer == null)
            return new ToolCallResult { Success = false, Error = "Unknown player" };

        var previous = gamePlayer.Science;
        gamePlayer.Science = parameters.Science;

        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));

        return new ToolCallResult 
        { 
            Success = true, 
            Message = $"Set {await gamePlayer.GetNameAsync(false)}'s science to {parameters.Science} (was {previous})" 
        };
    }

    private async Task<ToolCallResult> ExecuteSetPlayerVictoryPointsAsync(string argumentsJson, Game game, GamePlayer? contextPlayer)
    {
        var parameters = JsonSerializer.Deserialize<SetPlayerVictoryPointsParameters>(argumentsJson, _jsonOptions);
        if (parameters == null)
            return new ToolCallResult { Success = false, Error = "Invalid parameters" };

        var gamePlayer = parameters.Player == -1 ? contextPlayer : game.TryGetGamePlayerByGameId(parameters.Player);
        if (gamePlayer == null)
            return new ToolCallResult { Success = false, Error = "Unknown player" };

        var previous = gamePlayer.VictoryPoints;
        gamePlayer.VictoryPoints = parameters.Vp;

        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));

        return new ToolCallResult 
        { 
            Success = true, 
            Message = $"Set {await gamePlayer.GetNameAsync(false)}'s VP to {parameters.Vp} (was {previous})" 
        };
    }
} 