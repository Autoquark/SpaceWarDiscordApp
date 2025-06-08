using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord.Commands;

[RequireGameChannel]
public class RefreshCommands : IInteractionHandler<RefreshActionInteraction>
{
    public async Task HandleInteractionAsync(RefreshActionInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        
        await RefreshOperations.Refresh(builder, game, game.GetGamePlayerByGameId(interactionData.ForGamePlayerId));

        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Main);

        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));

        await args.Interaction.EditOriginalResponseAsync(builder);
    }
}