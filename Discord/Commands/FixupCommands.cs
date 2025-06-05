using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord.ChoiceProvider;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Discord.Commands;

/// <summary>
/// Commands which can be used to manually edit the game state
/// </summary>
[Command("fixup")]
[RequireGameChannel]
public class FixupCommands
{
    /// <summary>
    /// Set the number and/or owner of forces on a planet
    /// </summary>
    /// <param name="context"></param>
    /// <param name="coordinates"></param>
    /// <param name="amount">New amount of forces. Omit to keep existing number</param>
    /// <param name="player">New owner of forces. Omit to keep existing owner</param>
    [Command("setForces")]
    [Description("Set the number and/or owner of forces on a planet")]
    public static async Task SetForces(CommandContext context,
        [SlashAutoCompleteProvider<HexCoordsAutoCompleteProvider_WithPlanet>] HexCoordinates coordinates,
        [Description("New amount of forces. Omit to keep existing number")] int amount = -1,
        [Description("New owner of forces. Omit to keep existing owner or you if there's no existing owner")]
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>] int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;

        var hex = game.GetHexAt(coordinates);
        if (hex.Planet == null)
        {
            await context.RespondAsync($"Invalid coordinates {coordinates}");
            return;
        }
        
        var newAmount = amount > -1 ? amount : hex.ForcesPresent;
        GamePlayer? newOwner = null;
        
        if (newAmount > 0)
        {
            newOwner = game.TryGetGamePlayerByGameId(player) ?? game.TryGetGamePlayerByGameId(hex.Planet.OwningPlayerId) ?? game.GetGamePlayerByDiscordId(context.User.Id);
            if (newOwner == null)
            {
                await context.RespondAsync($"Must specify a player");
                return;
            }
        }

        hex.Planet.ForcesPresent = newAmount;
        hex.Planet.OwningPlayerId = newOwner?.GamePlayerId ?? 0;

        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
        
        if (newOwner != null)
        {
            await context.RespondAsync($"Set forces at {coordinates} to {newOwner.PlayerColourInfo.GetDieEmoji(hex.ForcesPresent)}");
        }
        else
        {
            await context.RespondAsync($"Removed all forces from {coordinates}");
        }
    }
    
    [Command("grantTech")]
    [Description("Grant a tech to a player")]
    public static async Task GrantTech(CommandContext context,
        [SlashAutoCompleteProvider<TechIdChoiceProvider>] string techId,
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>] int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var gamePlayer = player == -1 ? game.GetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            await context.RespondAsync("Unknown player");
            return;
        }

        if (gamePlayer.Techs.Any(x => x.TechId == techId))
        {
            await context.RespondAsync("Player already has that tech");
            return;
        }

        if (!Tech.TechsById.TryGetValue(techId, out var tech))
        {
            await context.RespondAsync("Unknown tech");
            return;
        }
        
        gamePlayer.Techs.Add(tech.CreatePlayerTech(game, gamePlayer));
        
        var builder = new DiscordMessageBuilder().EnableV2Components();
        builder.AppendContentNewline($"Granted {tech.DisplayName} to {await gamePlayer.GetNameAsync(true)}")
            .AllowMentions(gamePlayer);
        
        await context.RespondAsync(builder);
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
    }

    /// <summary>
    /// Removes a tech from a player.
    /// </summary>
    [Command("removeTech")]
    public static async Task RemoveTech(CommandContext context,
        [SlashAutoCompleteProvider<TechIdChoiceProvider>] string techId,
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>] int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var gamePlayer = player == -1 ? game.GetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            await context.RespondAsync("Unknown player");
            return;
        }

        if (!Tech.TechsById.TryGetValue(techId, out var tech))
        {
            await context.RespondAsync("Unknown tech");
            return;
        }

        var index = gamePlayer.Techs.Items.Index().FirstOrDefault(x => x.Item.TechId == techId, (-1, null!)).Index;
        if (index == -1)
        {
            await context.RespondAsync("Player does not have that tech");
            return;
        }
        gamePlayer.Techs.RemoveAt(index);
        
        var builder = new DiscordMessageBuilder().EnableV2Components();
        builder.AppendContentNewline($"Removed {tech.DisplayName} from {await gamePlayer.GetNameAsync(true)}")
            .AllowMentions(gamePlayer);
        
        await context.RespondAsync(builder);
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
    }

    [Command("setTechExhausted")]
    [Description("Exhausts or unexhausts a player's tech")]
    public static async Task SetTechExhausted(CommandContext context,
        [SlashAutoCompleteProvider<TechIdChoiceProvider>]
        string techId,
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>]
        int player = -1,
        bool exhausted = true)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var gamePlayer = player == -1 ? game.GetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            await context.RespondAsync("Unknown player");
            return;
        }

        if (!Tech.TechsById.TryGetValue(techId, out var tech))
        {
            await context.RespondAsync("Unknown tech");
            return;
        }

        var playerTech = gamePlayer.TryGetPlayerTechById(techId);
        if (playerTech == null)
        {
            await context.RespondAsync("Player does not have that tech");
            return;
        }
        playerTech.IsExhausted = exhausted;
        
        var builder = new DiscordMessageBuilder().EnableV2Components();
        builder.AppendContentNewline($"{(exhausted ? "Exhausted" : "Unexhausted")} {tech.DisplayName} for {await gamePlayer.GetNameAsync(true)}")
            .AllowMentions(gamePlayer);
        
        await context.RespondAsync(builder);
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
    }

    [Command("setPlanetExhausted")]
    [Description("Exhausts or unexhausts a planet")]
    public static async Task SetPlanetExhausted(CommandContext context,
        [SlashAutoCompleteProvider<HexCoordsAutoCompleteProvider_WithPlanet>] HexCoordinates coordinates,
        bool exhausted = true)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var hex = game.GetHexAt(coordinates);
        if (hex.Planet == null)
        {
            await context.RespondAsync($"Invalid coordinates {coordinates}");
            return;
        }
        
        hex.Planet.IsExhausted = exhausted;
        
        await context.RespondAsync($"{(exhausted ? "Exhausted" : "Unexhausted")} planet at {coordinates}");
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
    }

    [Command("SetPlayerTurn")]
    [Description("Set which player's turn it is")]
    public static async Task SetCurrentTurn(CommandContext context,
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>]
        int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var gamePlayer = player == -1 ? game.GetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            await context.RespondAsync("Unknown player");
            return;
        }

        var builder = new DiscordMessageBuilder().EnableV2Components();
        var previousPlayer = game.CurrentTurnPlayer;
        
        game.CurrentTurnPlayerIndex = game.Players.FindIndex(x => x.GamePlayerId == gamePlayer.GamePlayerId);
        game.ActionTakenThisTurn = false;
        game.IsWaitingForTechPurchaseDecision = false;
        
        builder.AppendContentNewline($"Set current turn to {await gamePlayer.GetNameAsync(true)} (was {await previousPlayer.GetNameAsync(true)})")
            .AllowMentions(gamePlayer, previousPlayer);
        await GameFlowOperations.ShowSelectActionMessageAsync(builder, game);
        await context.RespondAsync(builder);
        
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
    }

    [Command("SetPlayerScience")]
    [Description("Set a player's science total")]
    public static async Task SetPlayerScience(CommandContext context,
        int science,
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>] int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var gamePlayer = player == -1 ? game.GetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            await context.RespondAsync("Unknown player");
            return;
        }
        
        var previous = gamePlayer.Science;
        gamePlayer.Science = science;
        
        var builder = new DiscordMessageBuilder().EnableV2Components();
        builder.AppendContentNewline(
                $"Set {await gamePlayer.GetNameAsync(true)}'s science to {science} (was {previous})")
            .AllowMentions(gamePlayer);
        
        await context.RespondAsync(builder);
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
    }
    
    [Command("SetPlayerVp")]
    [Description("Set a player's Victory Points")]
    public static async Task SetPlayerVictoryPoints(CommandContext context,
        int vp,
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>]
        int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var gamePlayer = player == -1 ? game.GetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            await context.RespondAsync("Unknown player");
            return;
        }
        
        var previous = gamePlayer.VictoryPoints;
        gamePlayer.VictoryPoints = vp;
        
        var builder = new DiscordMessageBuilder().EnableV2Components();
        builder.AppendContentNewline($"Set {await gamePlayer.GetNameAsync(true)}'s VP to {vp} (was {previous})")
            .AllowMentions(gamePlayer);
        
        await context.RespondAsync(builder);
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
    }
}