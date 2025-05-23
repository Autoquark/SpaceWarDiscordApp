using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord.ChoiceProvider;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;

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
    public static async Task SetForces(CommandContext context,
        [SlashAutoCompleteProvider<HexCoordsAutoCompleteProvider_WithPlanet>] HexCoordinates coordinates,
        int amount = -1,
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
}