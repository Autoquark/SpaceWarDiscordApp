using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.FungibleAnatomy;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_FungibleAnatomy : Tech, IInteractionHandler<UseFungibleAnatomyInteraction>
{
    public Tech_FungibleAnatomy() : base("fungibleAnatomy", "Fungible Anatomy",
        "Whenever you would lose forces due to exceeding a planet's capacity, you may instead move the excess to an adjacent planet you control.",
        "We find it's most efficient to pack similar body parts together and reassemble personnel at the far end.")
    {
        _movementFlowHandler = new FungibleAnatomyMovementFlowHandler(this);
        AdditionalHandlers = [_movementFlowHandler];
    }
    
    private FungibleAnatomyMovementFlowHandler _movementFlowHandler;

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_ExceedingPlanetCapacity exceedingPlanetCapacity &&
            _movementFlowHandler.GetAllowedDestinationsForSource(game, game.GetHexAt(exceedingPlanetCapacity.Location)).Any())
        {
            var hex = game.GetHexAt(exceedingPlanetCapacity.Location);
            if (hex.Planet!.OwningPlayerId == player.GamePlayerId)
            {
                return
                [
                    new TriggeredEffect
                    {
                        AlwaysAutoResolve = false,
                        IsMandatory = false,
                        DisplayName = $"{DisplayName}: Move excess forces",
                        ResolveInteractionData = new UseFungibleAnatomyInteraction
                        {
                            Game = game.DocumentId,
                            ForGamePlayerId = player.GamePlayerId,
                            Event = exceedingPlanetCapacity,
                            EventId = exceedingPlanetCapacity.EventId
                        },
                        TriggerId = GetTriggerId(0)
                    }
                ];
            }
        }
        
        return [];
    }


    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, UseFungibleAnatomyInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Event.Location);
        await _movementFlowHandler.BeginPlanningMoveAsync(builder!, game,
            game.GetGamePlayerForInteraction(interactionData), serviceProvider,
            fixedSource: interactionData.Event.Location, dynamicMaxAmountPerSource: hex.ForcesPresent - interactionData.Event.Capacity,
            triggerToMarkResolved: interactionData.InteractionId);
        return new SpaceWarInteractionOutcome(false);
    }
}

class FungibleAnatomyMovementFlowHandler : MovementFlowHandler<Tech_FungibleAnatomy>
{
    public FungibleAnatomyMovementFlowHandler(Tech_FungibleAnatomy tech) : base(tech)
    {
        ActionType = null;
        AllowManyToOne = false;
        ContinueResolvingStackAfterMove = true;
        DestinationRestriction = MoveDestinationRestriction.MustAlreadyControl;
    }
    
    
}