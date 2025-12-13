using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech.AstralLandscaping;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_AstralLandscaping : Tech, IInteractionHandler<SelectAstralLandscapingFirstTargetInteraction>, IInteractionHandler<SelectAstralLandscapingSecondTargetInteraction>
{
    public Tech_AstralLandscaping() : base("astral-landscaping", "Astral Landscaping",
        "Swap the position of a system you control with an adjacent system.",
        "Trust me, this is really going to improve the galactic Feng Shui",
        [TechKeyword.FreeAction, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var controlledHexes = game.Hexes.WhereOwnedBy(player).ToList();

        var ids = serviceProvider.AddInteractionsToSetUp(controlledHexes.Select(x =>
            new SelectAstralLandscapingFirstTargetInteraction
            {
                Target = x.Coordinates,
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId
            }));

        var cancelId = serviceProvider.AddInteractionToSetUp(new RepromptInteraction()
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId
        });

        builder.AppendContentNewline($"{await player.GetNameAsync(true)}, choose a system you control to swap with an adjacent system:");
        builder.AppendHexButtons(game, controlledHexes, ids, cancelId);
        
        return builder;
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        SelectAstralLandscapingFirstTargetInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        var firstTarget = game.GetHexAt(interactionData.Target);
        if (firstTarget.Planet?.OwningPlayerId != interactionData.ForGamePlayerId)
        {
            throw new Exception();
        }

        var adjacentTargets = BoardUtils.GetNeighbouringHexes(game, firstTarget);

        var ids = serviceProvider.AddInteractionsToSetUp(adjacentTargets.Select(x =>
            new SelectAstralLandscapingSecondTargetInteraction()
            {
                ControlledTarget = firstTarget.Coordinates,
                OtherTarget = x.Coordinates,
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId
            }));

        var cancelId = serviceProvider.AddInteractionToSetUp(new RepromptInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId
        });
        
        builder!.AppendContentNewline($"{await player.GetNameAsync(true)}, choose an adjacent system to swap with {firstTarget.ToHexNumberWithDieEmoji(game)}:");
        builder.AppendHexButtons(game, adjacentTargets, ids, cancelId);
        
        return new SpaceWarInteractionOutcome(false);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        SelectAstralLandscapingSecondTargetInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        var firstTarget = game.GetHexAt(interactionData.ControlledTarget);
        if (firstTarget.Planet?.OwningPlayerId != interactionData.ForGamePlayerId)
        {
            throw new Exception();
        }

        var secondTarget = game.GetHexAt(interactionData.OtherTarget);

        builder!.AppendContentNewline(
            $"{await player.GetNameAsync(false)} is doing a spot of {DisplayName}, swapping the positions of {firstTarget.ToHexNumberWithDieEmoji(game)} and {secondTarget.ToHexNumberWithDieEmoji(game)}");
        
        (firstTarget.Coordinates, secondTarget.Coordinates) = (secondTarget.Coordinates, firstTarget.Coordinates);

        GetThisTech(player).IsExhausted = true;
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });
        
        return new SpaceWarInteractionOutcome(true);
    }
}