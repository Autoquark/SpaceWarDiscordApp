using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.IntensiveTraining;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_IntensiveTraining : Tech, IInteractionHandler<ApplyIntensiveTrainingBonusInteraction>
{
    public Tech_IntensiveTraining() : base("intensive-training",
        "Intensive Training",
        "While this tech is exhausted, gain +1 Combat Strength.",
        "I'll teach you how to fire your guns when you've mastered standing in a straight line. Which may take some time, at this rate.",
        ["Action", "Exhaust" ])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Main;
    }

    public override int GetDisplayedCombatStrengthBonus(Game game, BoardHex hex, GamePlayer player) => GetThisTech(player).IsExhausted ? 1 : 0;

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} intensively trains their troops!");
        GetThisTech(player).IsExhausted = true;
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });
        
        return builder;
    }

    public override IEnumerable<TriggeredEffect> GetTriggeredEffects(Game game, GameEvent gameEvent, GamePlayer player)
    {
        var playerTech = GetThisTech(player);
        if (playerTech.IsExhausted && gameEvent is GameEvent_PreMove preMove)
        {
            var destination = game.GetHexAt(preMove.Destination);
            bool? isAttacker = null;
            if (preMove.MovingPlayerId == player.GamePlayerId)
            {
                isAttacker = true;
            }
            else if (destination.Planet!.OwningPlayerId == player.GamePlayerId)
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
                        ResolveInteractionData = new ApplyIntensiveTrainingBonusInteraction
                        {
                            Game = game.DocumentId,
                            ForGamePlayerId = player.GamePlayerId,
                            IsAttacker = isAttacker.Value,
                            Event = preMove,
                            EventId = preMove.EventId
                        }
                    }
                ];
            }
        }
        
        return [];
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyIntensiveTrainingBonusInteraction interactionData,
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
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true);
    }
}