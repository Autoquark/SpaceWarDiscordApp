using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech;
using SpaceWarDiscordApp.Database.InteractionData.Tech.CreativeAI;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;
using System.Linq;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_CreativeAI : Tech, IInteractionHandler<CreativeAITechInteraction>
{
    public Tech_CreativeAI() : base(
        "creative_ai",
        "Creative AI",
        "You may discard a tech from the tech market. Cycle the tech market.",
        "So far it's come up with 'edible spaceships' and 'exploding helmets'. Actually, that second one might have some potential...",
        [TechKeyword.FreeAction, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var availableMarket = game.TechMarket.Select(x => x).WhereNonNull().ToList();

        if (availableMarket.Count == 0)
        {
            builder.AppendContentNewline("No suitable targets");
            return builder;
        }
        
        builder.AppendContentNewline($"{await player.GetNameAsync(true)}, choose a tech to discard:");

        var marketIds = serviceProvider.AddInteractionsToSetUp(availableMarket
            .Select(x => new CreativeAITechInteraction
            {
                Game = game.DocumentId,
                TechId = x,
                ForGamePlayerId = player.GamePlayerId,
                EditOriginalMessage = true,
            }));

        builder.AppendButtonRows(availableMarket
            .Zip(marketIds, (x, y) => (techId: x, interactionId: y))
            .Select(x => new DiscordButtonComponent(
                DiscordButtonStyle.Primary,
                x.interactionId,
                $"{Tech.TechsById[x.techId].DisplayName}")));

        var declineId = serviceProvider.AddInteractionToSetUp(new CreativeAITechInteraction
        {
            Game = game.DocumentId,
            TechId = null,
            ForGamePlayerId = player.GamePlayerId,
            EditOriginalMessage = false,
        });

        builder.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Danger, declineId, "Decline"));

        return builder;
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        CreativeAITechInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        if (interactionData.TechId != null)
        {
            var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
            var name = await player.GetNameAsync(false);
            var tech = Tech.TechsById[interactionData.TechId];

            builder?.AppendContentNewline($"{name} has discarded {tech.DisplayName} from the tech market");

            player.GetPlayerTechById(Id).IsExhausted = true;

            var index = game.TechMarket.IndexOf(tech.Id);
            if (index != -1)
            {
                game.TechMarket[index] = null;
            }
        } // Or decline

        await TechOperations.CycleTechMarketAsync(builder, game);

        await TechOperations.UpdatePinnedTechMessage(game);

        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });
        
        return new SpaceWarInteractionOutcome(true);
    }
}