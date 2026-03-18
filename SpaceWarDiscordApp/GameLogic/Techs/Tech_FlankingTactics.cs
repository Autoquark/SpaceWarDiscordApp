using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Movement;
using SpaceWarDiscordApp.Database.InteractionData.Tech.FlankingTactics;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_FlankingTactics : Tech, IInteractionHandler<ApplyFlankingTacticsBonusInteraction>
{
    public Tech_FlankingTactics() : base("flanking-tactics",
        "Flanking Tactics",
        "When you attack by moving from 2 or more planets simultaneously, +1 Combat Strength per planet beyond the first.",
        "We've had to issue some additional training materials clarifying the distinction between 'left' and 'right'")
    {
        
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_PreMove preMove
            && preMove.MovingPlayerId == player.GamePlayerId
            && preMove.Sources.Count > 1)
        {
            return
            [
                new TriggeredEffect
                {
                    AlwaysAutoResolve = true,
                    IsMandatory = true,
                    DisplayName = DisplayName,
                    ResolveInteractionData = new ApplyFlankingTacticsBonusInteraction
                    {
                        Game = game.DocumentId,
                        ForGamePlayerId = player.GamePlayerId,
                        Event = preMove,
                        EventId = preMove.EventId,
                    },
                    TriggerId = GetTriggerId(0)
                }
            ];
        }

        return [];
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyFlankingTacticsBonusInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        interactionData.Event.AttackerCombatStrengthSources.Add(new CombatStrengthSource
        {
            DisplayName = DisplayName,
            Amount = interactionData.Event.Sources.Count - 1
        });
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true);
    }
}