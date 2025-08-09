using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.CodeOfHonour;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_CodeOfHonour : Tech, IInteractionHandler<ApplyCodeOfHonourBonusInteraction>
{
    public Tech_CodeOfHonour() : base("codeOfHonour", "Code of Honour", "If you have the same number of forces present as your opponent, +1 Combat Strength.",
        "There's no other option - we're going to have to fight fair.")
    {
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_PreMove preMove)
        {
            var destination = game.GetHexAt(preMove.Destination);
            bool? isAttacker = null;
            if (destination.ForcesPresent == preMove.Sources.Sum(x => x.Amount))
            {
                if (preMove.MovingPlayerId == player.GamePlayerId)
                {
                    isAttacker = true;
                }
                else if (destination.Planet!.OwningPlayerId == player.GamePlayerId)
                {
                    isAttacker = false;
                }
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
                        ResolveInteractionData = new ApplyCodeOfHonourBonusInteraction()
                        {
                            Game = game.DocumentId,
                            ForGamePlayerId = player.GamePlayerId,
                            Event = preMove,
                            EventDocumentId = preMove.DocumentId!,
                            IsAttacker = isAttacker.Value
                        }
                    }
                ];
            }
        }

        return [];
    }

    public Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyCodeOfHonourBonusInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
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