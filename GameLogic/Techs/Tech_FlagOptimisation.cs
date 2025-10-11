using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.FlagOptimisation;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_FlagOptimisation : Tech, IInteractionHandler<ApplyFlagOptimisationBonusInteraction>
{
    public Tech_FlagOptimisation() : base(
        "flagOptimisation",
        "Flag Optimisation",
        "When fighting on a planet with $victory$, +1 Combat Strength.",
        "The actuators ensure satisfactory billowing even in zero gravity, non-atmospheric environments.")
    {
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_PreMove preMove)
        {
            var destination = game.GetHexAt(preMove.Destination);

            if (destination.Planet != null && destination.Planet.Stars > 0)
            {
                bool? isAttacker = null;

                if (preMove.MovingPlayerId == player.GamePlayerId)
                {
                    isAttacker = true;
                }
                else if (destination.Planet.OwningPlayerId == player.GamePlayerId)
                {
                    isAttacker = false;
                }

                if (isAttacker != null)
                {
                    return
                    [
                        new TriggeredEffect
                        {
                            AlwaysAutoResolve = true,
                            IsMandatory = true,
                            DisplayName = DisplayName,
                            ResolveInteractionData = new ApplyFlagOptimisationBonusInteraction()
                            {
                                Game = game.DocumentId,
                                ForGamePlayerId = player.GamePlayerId,
                                Event = preMove,
                                IsAttacker = isAttacker.Value
                            }
                        }
                    ];
                }
            }
        }

        return [];
    }

    public Task<SpaceWarInteractionOutcome> HandleInteractionAsync(
        DiscordMultiMessageBuilder? builder,
        ApplyFlagOptimisationBonusInteraction interactionData,
        Game game,
        IServiceProvider serviceProvider)
    {
        if (interactionData.IsAttacker)
        {
            interactionData.Event.AttackerCombatStrengthSources.Add(new CombatStrengthSource
            {
                DisplayName = DisplayName,
                Amount = 1
            });
        }
        else
        {
            interactionData.Event.DefenderCombatStrengthSources.Add(new CombatStrengthSource
            {
                DisplayName = DisplayName,
                Amount = 1
            });
        }

        GameFlowOperations.TriggerResolved(game, interactionData.InteractionId);

        return Task.FromResult(new SpaceWarInteractionOutcome(true, builder));
    }
}