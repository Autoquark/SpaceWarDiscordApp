using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.MilitaryGraduationCannon;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_MilitaryGraduationCannon : Tech, IInteractionHandler<TriggerMilitaryGraduationCannonInteractionData>
{
    public Tech_MilitaryGraduationCannon() : base("militaryGraduationCannon",
        "Military Graduation Cannon",
        "Exhaust: When you produce Forces, you may immediately move any number of the forces produced to one adjacent planet. (This occurs before forces on the producing planet are capped to 6)",
        "When your identification number is called, please collect your medal and climb into the barrel")
    {
        _movementFlowHandler = new MilitaryGraduationCannon_MovementFlowHandler(this);
        AdditionalHandlers = [_movementFlowHandler];
    }
    
    private readonly MilitaryGraduationCannon_MovementFlowHandler _movementFlowHandler;

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (GetThisTech(player).IsExhausted)
        {
            return [];
        }
        
        if (gameEvent is GameEvent_PostProduce postProduce && postProduce.PlayerGameId == player.GamePlayerId)
        {
            return [new TriggeredEffect
            {
                DisplayName = $"{DisplayName}: Move produced forces",
                IsMandatory = false,
                ResolveInteractionData = new TriggerMilitaryGraduationCannonInteractionData
                {
                    ForGamePlayerId = player.GamePlayerId,
                    Game = game.DocumentId,
                    AmountProduced = postProduce.ForcesProduced,
                    Source = postProduce.Location
                }
            }];
        }

        return [];
    }


    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync<TBuilder>(TBuilder builder, TriggerMilitaryGraduationCannonInteractionData interactionData,
        Game game, IServiceProvider serviceProvider) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        await _movementFlowHandler.BeginPlanningMoveAsync(builder,
            game,
            game.GetGamePlayerByGameId(interactionData.ForGamePlayerId),
            serviceProvider,
            fixedSource: interactionData.Source,
            dynamicMaxAmountPerSource: interactionData.AmountProduced,
            triggerToMarkResolved: interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(false, builder);
    }
}

class MilitaryGraduationCannon_MovementFlowHandler : MovementFlowHandler<Tech_MilitaryGraduationCannon>
{
    public MilitaryGraduationCannon_MovementFlowHandler(Tech_MilitaryGraduationCannon tech) : base(tech.DisplayName)
    {
        AllowManyToOne = false;
        ExhaustTechId = tech.Id;
        ActionType = null;
        ContinueResolvingStackAfterMove = true;
    }
}