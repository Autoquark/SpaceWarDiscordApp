using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Movement;
using SpaceWarDiscordApp.Database.InteractionData.Tech.EnPassant;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_EnPassant : Tech, IInteractionHandler<ResolveEnPassantInteraction>
{
    public Tech_EnPassant() : base("en_passant", "En Passant",
        "When you complete a movement, if the destination and at least one source were both adjacent to a planet controlled by an opponent, destroy 1 forces from that planet.",
        "This is the 'offside gambit' all over again, isn't it?")
    {
    }

    private static IEnumerable<BoardHex> GetPlanetsToAffect(Game game, GameEvent_MovementFlowComplete gameEvent)
    {
        var adjacentToDestination = BoardUtils.GetNeighbouringHexes(game, gameEvent.Destination)
            .WhereForcesPresent()
            .WhereNotOwnedBy(gameEvent.PlayerGameId)
            .ToHashSet();

        return gameEvent.Sources.SelectMany(x => BoardUtils.GetNeighbouringHexes(game, x.Source))
            .Intersect(adjacentToDestination)
            .Distinct();
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_MovementFlowComplete movementFlowComplete
            && movementFlowComplete.PlayerGameId == player.GamePlayerId
            && GetPlanetsToAffect(game, movementFlowComplete).Any())
        {
            return
            [
                new TriggeredEffect
                {
                    AlwaysAutoResolve = true,
                    DisplayName = DisplayName,
                    IsMandatory = true,
                    ResolveInteractionData = new ResolveEnPassantInteraction
                    {
                        Game = game.DocumentId,
                        ForGamePlayerId = player.GamePlayerId,
                        EventId = movementFlowComplete.EventId,
                        Event = movementFlowComplete
                    },
                    TriggerId = GetTriggerId(0)
                }
            ];
        }

        return [];
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ResolveEnPassantInteraction interactionData, Game game,
        IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerByGameId(interactionData.Event.PlayerGameId);
        
        foreach (var boardHex in GetPlanetsToAffect(game, interactionData.Event))
        {
            GameFlowOperations.DestroyForces(game, boardHex, 1, interactionData.Event.PlayerGameId,
                ForcesDestructionReason.Tech, Id);
            builder?.AppendContentNewline(
                $"{await player.GetNameAsync(false)} destroyed 1 forces on {boardHex.ToHexNumberWithDieEmoji(game)} *en passant*");
        }
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        return new SpaceWarInteractionOutcome(true);
    }
}