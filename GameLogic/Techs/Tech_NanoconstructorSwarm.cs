using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Tech;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech.NanoconstructorSwarm;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_NanoconstructorSwarm : Tech, IPlayerChoiceEventHandler<GameEvent_ChooseNanoconstructorNextPlanet, SelectNanoconstructorSwarmNextPlanetInteraction>
{
    public Tech_NanoconstructorSwarm() : base("nanoconstructorSwarm", "Nanoconstructor Swarm",
        "Produce on all your ready planets. Exhaust them as normal.",
        "We've made some modifications. Hopefully they won't try to unionise this time.",
        [TechKeyword.SingleUse, TechKeyword.Action])
    {
        HasSimpleAction = true;
    }
    
    private static IEnumerable<BoardHex> GetTargetHexes(Game game, GamePlayer player) => game.Hexes.WhereOwnedBy(player).Where(x => x.Planet?.IsExhausted == false);

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player)
        => base.IsSimpleActionAvailable(game, player) && GetTargetHexes(game, player).Any();

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = GetTargetHexes(game, player).ToList();

        builder.AppendContentNewline($"{await player.GetNameAsync(false)} is deploying a nanoconstructor swarm!");
        builder.AppendContentNewline($"Producing on {targets.Count} planets:");

        GameFlowOperations.PushGameEvents(game, new GameEvent_ChooseNanoconstructorNextPlanet
        {
            PlayerGameId = player.GamePlayerId,
            RemainingPlanets = targets.Select(x => x.Coordinates).ToList()
        });

        return (await GameFlowOperations.ContinueResolvingEventStackAsync(builder, game, serviceProvider))!;
    }

    public async Task<DiscordMultiMessageBuilder?> ShowPlayerChoicesAsync(DiscordMultiMessageBuilder builder, GameEvent_ChooseNanoconstructorNextPlanet gameEvent,
        Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerByGameId(gameEvent.PlayerGameId);
        builder.AppendContentNewline(
            $"{await player.GetNameAsync(true)}, choose the next planet to produce on with nanoconstructor swarm:");

        var interactionIds = serviceProvider.AddInteractionsToSetUp(gameEvent.RemainingPlanets.Select(x =>
            new SelectNanoconstructorSwarmNextPlanetInteraction
            {
                Target = x,
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                ResolvesChoiceEventId = gameEvent.EventId
            }));

        var quickResolveId = serviceProvider.AddInteractionToSetUp(
            new SelectNanoconstructorSwarmNextPlanetInteraction
            {
                Target = null,
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                ResolvesChoiceEventId = gameEvent.EventId
            });

        var showBoardId = serviceProvider.AddInteractionToSetUp(
            new ShowBoardInteraction
            {
                ForGamePlayerId = -1,
                Game = game.DocumentId
            });
        
        builder.AppendHexButtons(game, gameEvent.RemainingPlanets.Select(game.GetHexAt), interactionIds);
        return builder.AppendButtonRows([
            new DiscordButtonComponent(DiscordButtonStyle.Secondary, quickResolveId, "Quick resolve"),
            DiscordHelpers.CreateShowBoardButton(showBoardId)
        ]);
    }

    public async Task<bool> HandlePlayerChoiceEventInteractionAsync(DiscordMultiMessageBuilder? builder,
        GameEvent_ChooseNanoconstructorNextPlanet gameEvent, SelectNanoconstructorSwarmNextPlanetInteraction choice,
        Game game, IServiceProvider serviceProvider)
    {
        if (!choice.Target.HasValue)
        {
            GameFlowOperations.PushGameEvents(game, gameEvent.RemainingPlanets.Select(x => ProduceOperations.CreateProduceEvent(game, x)));
            gameEvent.RemainingPlanets.Clear();
        }
        else
        {
            GameFlowOperations.PushGameEvents(game, ProduceOperations.CreateProduceEvent(game, choice.Target.Value));
            gameEvent.RemainingPlanets.Remove(choice.Target.Value);
        }

        var finished = gameEvent.RemainingPlanets.Count == 0;
        if (finished)
        {
            var player = game.GetGamePlayerByGameId(gameEvent.PlayerGameId);
            GameFlowOperations.PushGameEvents(game, new GameEvent_PlayerLoseTech
            {
                PlayerGameId = player.GamePlayerId,
                TechId = Id,
                Reason = LoseTechReason.SingleUse
            }, new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType
            });
        }
        else
        {
            await GameFlowOperations.ContinueResolvingEventStackAsync(builder, game, serviceProvider);
        }
        
        return finished;
    }
}