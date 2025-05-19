using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.DatabaseModels;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Commands;

public class RefreshCommands : IInteractionHandler<RefreshActionInteraction>
{
    public async Task HandleInteractionAsync(RefreshActionInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        var name = await game.GetGamePlayerByGameId(interactionData.ForPlayerId).GetNameAsync(false);
        builder.AppendContentNewline($"{name} is refreshing");

        var refreshed = new HashSet<BoardHex>();
        foreach (var hex in game.Hexes.Where(x => x.Planet?.OwningPlayerId == interactionData.ForPlayerId))
        {
            if (hex.Planet?.IsExhausted == true)
            {
                hex.Planet.IsExhausted = false;
                refreshed.Add(hex);
            }
        }

        if (refreshed.Count > 0)
        {
            builder.AppendContentNewline($"Refreshed: " + string.Join(", ", refreshed.Select(x => x.Coordinates)));
        }
        else
        {
            builder.AppendContentNewline("Nothing to refresh!");
        }

        await GameplayCommands.NextTurnAsync(builder, game);

        await Program.FirestoreDb.RunTransactionAsync(transaction =>
        {
            transaction.Set(game);
            return Task.CompletedTask;
        });

        await args.Interaction.EditOriginalResponseAsync(builder);
    }
}