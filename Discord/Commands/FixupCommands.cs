using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
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
        
        var newAmount = amount > -1 ? amount : hex.Planet.ForcesPresent;
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
            await context.RespondAsync($"Set forces at {coordinates} to {newOwner.PlayerColourInfo.GetDieEmoji(hex.Planet.ForcesPresent)}");
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
        
        await context.RespondAsync($"Granted {tech.DisplayName} to {await gamePlayer.GetNameAsync(true)}");
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
        
        await context.RespondAsync($"Removed {tech.DisplayName} from {await gamePlayer.GetNameAsync(true)}");
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
    }

    [Command("setTechExhausted")]
    [Description("Exhausts a player's tech")]
    public static async Task ExhaustTech(CommandContext context,
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
        
        await context.RespondAsync($"{(exhausted ? "Exhausted" : "Unexhausted")} {tech.DisplayName} for {await gamePlayer.GetNameAsync(true)}");
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
    }
}