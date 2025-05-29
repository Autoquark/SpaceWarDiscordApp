using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech;
using SpaceWarDiscordApp.Discord.ChoiceProvider;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Discord.Commands;

public class TechCommands : IInteractionHandler<UseTechActionInteraction>,
    IInteractionHandler<PurchaseTechInteraction>,
    IInteractionHandler<DeclineTechPurchaseInteraction>
{
    public async Task HandleInteractionAsync(UseTechActionInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var tech = Tech.TechsById[interactionData.TechId];
        var player = game.GetGamePlayerByGameId(interactionData.UsingPlayerId);
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        
        await tech.UseTechActionAsync(builder, game, player);

        await args.Interaction.EditOriginalResponseAsync(builder);
    }

    public async Task HandleInteractionAsync(PurchaseTechInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        
        await TechOperations.PurchaseTechAsync(builder,
            game,
            game.GetGamePlayerForInteraction(interactionData),
            interactionData.TechId,
            interactionData.Cost);
        
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
        await args.Interaction.EditOriginalResponseAsync(builder);
    }

    public async Task HandleInteractionAsync(DeclineTechPurchaseInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        var player = game.GetGamePlayerForInteraction(interactionData);
        var name = await player.GetNameAsync(false);

        builder.AppendContentNewline($"{name} declines to purchase a tech");

        game.IsWaitingForTechPurchaseDecision = false;
        await GameFlowOperations.AdvanceTurnOrPromptNextActionAsync(builder, game);
        
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
        
        await args.Interaction.EditOriginalResponseAsync(builder);
    }

    [Command("ShowTech")]
    public async Task ShowTechDetails(CommandContext context, [SlashAutoCompleteProvider<TechIdChoiceProvider>] string techId)
    {
        var builder = new DiscordMessageBuilder().EnableV2Components();
        
        TechOperations.ShowTechDetails(builder, techId);
        
        await context.RespondAsync(builder);
    }
}