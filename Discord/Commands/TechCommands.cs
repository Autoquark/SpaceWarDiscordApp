using System.ComponentModel;
using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech;
using SpaceWarDiscordApp.Discord.ChoiceProvider;
using SpaceWarDiscordApp.Discord.ContextChecks;
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

        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
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

    [Command("ShowTechDeck")]
    [Description("List the techs in the tech deck (in alphabetical order)")]
    [RequireGameChannel]
    public async Task ShowTechDeck(CommandContext context, bool fullInfo = false)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;

        var deckTechs = game.TechDeck.Select(x => Tech.TechsById[x])
            .OrderBy(x => x.DisplayName)
            .ToList();

        var builder = new DiscordMessageBuilder().EnableV2Components();
        
        if (deckTechs.Count == 0)
        {
            builder.AppendContentNewline("The tech deck is empty");
        }
        else
        {
            builder.AppendContentNewline("Techs in the tech deck (in alphabetical order):");
        
            if (fullInfo)
            {
                foreach (var deckTech in deckTechs)
                {
                    TechOperations.ShowTechDetails(builder, deckTech.Id);
                }
            }
            else
            {
                builder.AppendContentNewline(string.Join(", ", deckTechs.Select(x => x.DisplayName)));
            }
        }
        
        await context.RespondAsync(builder);
    }
    
    [Command("ShowTechDiscards")]
    [Description("Show the contents of the tech discard pile")]
    [RequireGameChannel]
    public async Task ShowTechDiscards(CommandContext context, bool fullInfo = false)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;

        var deckTechs = game.TechDiscards.Select(x => Tech.TechsById[x])
            .ToList();

        var builder = new DiscordMessageBuilder().EnableV2Components();
        
        if (deckTechs.Count == 0)
        {
            builder.AppendContentNewline("The tech discards is empty");
        }
        else
        {
            builder.AppendContentNewline("Techs in the tech discards:");
        
            if (fullInfo)
            {
                foreach (var deckTech in deckTechs)
                {
                    TechOperations.ShowTechDetails(builder, deckTech.Id);
                }
            }
            else
            {
                builder.AppendContentNewline(string.Join(", ", deckTechs.Select(x => x.DisplayName)));
            }
        }
        
        await context.RespondAsync(builder);
    }
}