using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents.Setup;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Discord.Commands;

public class GameplayCommands : IInteractionHandler<EndTurnInteraction>, IInteractionHandler<DeclineOptionalTriggersInteraction>,
    IPlayerChoiceEventHandler<GameEvent_PlayersChooseStartingTech, ChoosePlayerStartingTechInteraction>,
    IInteractionHandler<RepromptInteraction>
{
    [Command("ShowBoard")]
    [RequireGameChannel(RequireGameChannelMode.ReadOnly)]
    public static async Task ShowBoardStateCommand(CommandContext context, bool oldCoords = false)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;

        if (game.Hexes.Count == 0)
        {
            context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder
                .AppendContentNewline("No map has yet been generated for this game");
            return;
        }
        
        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder;
        await GameFlowOperations.ShowBoardStateMessageAsync(builder, game, oldCoords);
    }

    [Command("Reprompt")]
    [Description("Repost whatever decision the game is currently waiting for, and ping the relevant player")]
    [RequireGameChannel(RequireGameChannelMode.ReadOnly)]
    public static async Task Reprompt(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;

        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder;
        await GameFlowOperations.ContinueResolvingEventStackAsync(builder, game, context.ServiceProvider);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        EndTurnInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        if (game.CurrentTurnPlayer.GamePlayerId != interactionData.ForGamePlayerId)
        {
            builder?.AppendContentNewline("It looks like you are clicking an old end turn button. If you have lost the current buttons, try /reprompt");
        }
        
        // Make sure there's some record of this turn if they did nothing for some reason
        if (!game.AnyActionTakenThisTurn)
        {
            builder?.AppendContentNewline($"{await game.CurrentTurnPlayer.GetNameAsync(false)} ends their turn without taking any actions");
            builder?.NewMessage();
        }
        
        await GameFlowOperations.NextTurnAsync(builder, game, serviceProvider);
        
        // If we're ending the turn, delete the original turn action prompt message to condense the game history
        await (serviceProvider.GetRequiredService<SpaceWarCommandContextData>().InteractionMessage?.DeleteAsync() ?? Task.CompletedTask);

        return new SpaceWarInteractionOutcome(true);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        DeclineOptionalTriggersInteraction interactionData,
        Game game,
        IServiceProvider serviceProvider)
    {
        await GameFlowOperations.DeclineOptionalTriggersAsync(builder, game, serviceProvider);
        return new SpaceWarInteractionOutcome(true);
    }
    
    public async Task<DiscordMultiMessageBuilder?> ShowPlayerChoicesAsync(DiscordMultiMessageBuilder builder, GameEvent_PlayersChooseStartingTech gameEvent,
        Game game, IServiceProvider serviceProvider)
    {
        await GameFlowOperations.ShowBoardStateMessageAsync(builder, game);
        
        switch (game.Rules.StartingTechRule)
        {
            case StartingTechRule.OneUniversal:
            {
                var notChosen = game.Players.Where(x => x.StartingTechs.Count == 0)
                    .ToList();

                builder.AppendContentNewline(
                    string.Join(", ", await Task.WhenAll(notChosen.Select(x => x.GetNameAsync(true)))) +
                    ", please choose a starting tech.");

                var interactions = serviceProvider.AddInteractionsToSetUp(game.UniversalTechs.Select(x =>
                    new ChoosePlayerStartingTechInteraction
                    {
                        ForGamePlayerId = -1,
                        Game = game.DocumentId,
                        TechId = x,
                        ResolvesChoiceEventId = gameEvent.EventId
                    }));

                builder.AppendButtonRows(game.UniversalTechs.Zip(interactions,
                    (techId, interactionId) => new DiscordButtonComponent(DiscordButtonStyle.Primary, interactionId,
                        Tech.TechsById[techId].DisplayName)));
                break;
            }
            
            case StartingTechRule.IndividualDraft:
                var builders = serviceProvider.GetRequiredService<GameMessageBuilders>();
                foreach (var player in game.Players)
                {
                    var playerBuilder = builders.PlayerPrivateThreadBuilders[player.GamePlayerId];
                    var hand = game.StartingTechHands[player.CurrentStartingTechHandIndex].Techs.ToTechsById().ToList();
                    var interactionIds = serviceProvider.AddInteractionsToSetUp(hand.Select(x => new ChoosePlayerStartingTechInteraction
                    {
                        TechId = x.Id,
                        Game = game.DocumentId,
                        ForGamePlayerId = player.GamePlayerId,
                        ResolvesChoiceEventId = gameEvent.EventId
                    }));
                    
                    playerBuilder.NewMessage();
                    playerBuilder.AppendContentNewline($"{await player.GetNameAsync(true)}, please choose a starting tech:");

                    foreach (var tech in hand)
                    {
                        TechOperations.ShowTechDetails(playerBuilder, tech.Id);
                    }
                    
                    playerBuilder.AppendButtonRows(hand.Zip(interactionIds).Select(
                        x => new DiscordButtonComponent(DiscordButtonStyle.Primary, x.Second, x.First.DisplayName)));
                }
                break;
        }

        return builder;
    }

    public async Task<bool> HandlePlayerChoiceEventInteractionAsync(DiscordMultiMessageBuilder? builder,
        GameEvent_PlayersChooseStartingTech gameEvent, ChoosePlayerStartingTechInteraction choice, Game game,
        IServiceProvider serviceProvider)
    {
        var discordUser = serviceProvider.GetRequiredService<SpaceWarCommandContextData>().User;
        
        // If this is being triggered artificially from a fixup command, we need to use the interaction game player ID
        // to know the intended player. Otherwise, this button might be for any player so we need to use the Discord user ID.
        var gamePlayer = game.TryGetGamePlayerByGameId(choice.ForGamePlayerId) ?? game.TryGetGamePlayerByDiscordId(discordUser.Id);

        if (gamePlayer == null)
        {
            return false;
        }
        
        await GameFlowOperations.PlayerChooseStartingTechAsync(builder, game, gamePlayer, choice.TechId, serviceProvider);
        
        return game.Players.All(x => x.StartingTechs.Count > 0);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, RepromptInteraction interactionData, Game game,
        IServiceProvider serviceProvider)
    {
        await GameFlowOperations.ContinueResolvingEventStackAsync(builder, game, serviceProvider);
        return new SpaceWarInteractionOutcome(false);
    }
}