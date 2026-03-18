using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Movement;
using SpaceWarDiscordApp.Database.InteractionData.Tech.AdaptiveAI;
using SpaceWarDiscordApp.Database.Tech;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_AdaptiveAI : Tech, IInteractionHandler<ApplyAdaptiveAIBonusInteraction>, IInteractionHandler<GainAdaptiveAIProgressInteraction>
{
    private const int ForcesPerPlusOne = 6;
    
    public Tech_AdaptiveAI() : base("adaptive-ai", "Adaptive AI",
        $"When you lose forces in combat, put one of your tokens on this tech for each forces lost. Gain +1 Combat Strength when attacking for every {ForcesPerPlusOne} of your tokens on this tech.",
        "I understand your misgivings, Captain, but if we refuse to pilot the ship into that supernova, how is the AI supposed to learn that it's a bad idea?")
    {
        
    }

    public override PlayerTech CreatePlayerTech(Game game, GamePlayer player) => new PlayerTech_AdaptiveAI
    {
        TechId = Id
    };

    public override async Task<string> GetTechStatusLineAsync(Game game, GamePlayer player)
    {
        var tech = GetThisTech<PlayerTech_AdaptiveAI>(player);
        return await base.GetTechStatusLineAsync(game, player) + $"{(tech.Progress / ForcesPerPlusOne):+#0} ({tech.Progress % ForcesPerPlusOne}/{ForcesPerPlusOne})";
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        var tech = GetThisTech<PlayerTech_AdaptiveAI>(player);
        if (gameEvent is GameEvent_PreMove preMove && tech.Progress >= ForcesPerPlusOne &&
            preMove.MovingPlayerId == player.GamePlayerId)
        {
            return
            [
                new TriggeredEffect
                {
                    AlwaysAutoResolve = true,
                    IsMandatory = true,
                    DisplayName = DisplayName,
                    ResolveInteractionData = new ApplyAdaptiveAIBonusInteraction
                    {
                        Game = game.DocumentId,
                        ForGamePlayerId = player.GamePlayerId,
                        Event = preMove,
                        EventId = preMove.EventId,
                        IsAttacker = true
                    },
                    TriggerId = GetTriggerId(0)
                }
            ];
        }

        if (gameEvent is GameEvent_PostForcesDestroyed forcesDestroyed
            && forcesDestroyed.OwningPlayerGameId == player.GamePlayerId
            && forcesDestroyed.Reason == ForcesDestructionReason.Combat)
        {
            return
            [
                new TriggeredEffect
                {
                    AlwaysAutoResolve = true,
                    IsMandatory = true,
                    DisplayName = DisplayName,
                    ResolveInteractionData = new GainAdaptiveAIProgressInteraction
                    {
                        Game = game.DocumentId,
                        ForGamePlayerId = player.GamePlayerId,
                        Event = forcesDestroyed,
                        EventId = forcesDestroyed.EventId,
                    },
                    TriggerId = GetTriggerId(1)
                }
            ];
        }

        return [];
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyAdaptiveAIBonusInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var amount = GetThisTech<PlayerTech_AdaptiveAI>(game.GetGamePlayerForInteraction(interactionData)).Progress / ForcesPerPlusOne;
        if (interactionData.IsAttacker)
        {
            interactionData.Event.AttackerCombatStrengthSources.Add(new CombatStrengthSource
            {
                DisplayName = DisplayName,
                Amount = amount
            });
        }
        else
        {
            interactionData.Event.DefenderCombatStrengthSources.Add(new CombatStrengthSource
            {
                DisplayName = DisplayName,
                Amount = amount
            });
        }
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, GainAdaptiveAIProgressInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var owningPlayer = game.GetGamePlayerForInteraction(interactionData);
        var tech = GetThisTech<PlayerTech_AdaptiveAI>(owningPlayer);
        
        var preBonus = tech.Progress / ForcesPerPlusOne;
        tech.Progress += interactionData.Event.Amount;
        var postBonus = tech.Progress / ForcesPerPlusOne;
        
        if (preBonus != postBonus)
        {
            builder?.AppendContentNewline(
                $"{await owningPlayer.GetNameAsync(false)}'s Adaptive AI bonus has increased to {postBonus}");
        }
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true);
    }
}