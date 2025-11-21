using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData.Tech;
using SpaceWarDiscordApp.Discord.ChoiceProvider;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Discord.Commands;

public class TechCommands : IInteractionHandler<UseTechActionInteraction>
{
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        UseTechActionInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var tech = Tech.TechsById[interactionData.TechId];
        var player = game.GetGamePlayerByGameId(interactionData.UsingPlayerId);
        
        await tech.UseTechActionAsync(builder, game, player, serviceProvider);
        
        return new SpaceWarInteractionOutcome(true);
    }

    [Command("ShowTech")]
    [RequireGameChannel(RequireGameChannelMode.ReadOnly)]
    public Task ShowTechDetails(CommandContext context, [SlashAutoCompleteProvider<TechIdChoiceProvider>] string techId)
    {
        var outcome = context.Outcome();
        var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>();
        
        TechOperations.ShowTechDetails(builder, techId);
        
        outcome.ReplyBuilder = builder;
        return Task.CompletedTask;
    }

    [Command("ShowTechDeck")]
    [Description("List the techs in the tech deck (in alphabetical order)")]
    [RequireGameChannel(RequireGameChannelMode.ReadOnly)]
    public Task ShowTechDeck(CommandContext context, bool fullInfo = false)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        var deckTechs = game.TechDeck.Select(x => Tech.TechsById[x])
            .OrderBy(x => x.DisplayName)
            .ToList();

        var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>();
        
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
        
        outcome.ReplyBuilder = builder;
        return Task.CompletedTask;
    }
    
    [Command("ShowTechDiscards")]
    [Description("Show the contents of the tech discard pile")]
    [RequireGameChannel(RequireGameChannelMode.ReadOnly)]
    public Task ShowTechDiscards(CommandContext context, bool fullInfo = false)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        var deckTechs = game.TechDiscards.Select(x => Tech.TechsById[x])
            .ToList();

        var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>();
        
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
        
        outcome.ReplyBuilder = builder;
        return Task.CompletedTask;
    }
}