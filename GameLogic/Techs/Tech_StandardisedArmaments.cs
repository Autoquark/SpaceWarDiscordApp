using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech.StandardisedArmaments;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_StandardisedArmaments : Tech, IInteractionHandler<UseStandardisedArmamentsInteraction>
{
    public Tech_StandardisedArmaments() : base("standardised-armaments", "Standardised Armaments",
        "Free Action, Once per turn: Choose a ready planet you control. Exhaust it and add 1 forces.",
        "Regulation 4.7.9: The barrel of the gun is to have no more than 10 degrees of curvature.",
        [TechKeyword.FreeAction, TechKeyword.OncePerTurn])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        SimpleActionIsOncePerTurn = true;
    }
    
    private IEnumerable<BoardHex> GetTargets(Game game, GamePlayer player)
        => game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId && !x.Planet.IsExhausted);

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) =>
        base.IsSimpleActionAvailable(game, player) && GetTargets(game, player).Any();

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = GetTargets(game, player).ToList();

        if (targets.Count == 0)
        {
            builder.AppendContentNewline("No targets available");
        }

        builder.AppendContentNewline($"{await player.GetNameAsync(true)}, choose where add forces:");
        
        var interactionIds = serviceProvider.AddInteractionsToSetUp(targets.Select(x =>
            new UseStandardisedArmamentsInteraction()
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                EditOriginalMessage = true,
                Target = x.Coordinates
            }));
        
        var cancelId = serviceProvider.AddInteractionToSetUp(new RepromptInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId
        });
        
        return builder.AppendHexButtons(game, targets, interactionIds)
            .AppendCancelButton(cancelId);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, UseStandardisedArmamentsInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        GetThisTech(player).UsedThisTurn = true;
        
        var hex = game.GetHexAt(interactionData.Target);
        hex.Planet!.AddForces(1);
        hex.Planet.IsExhausted = true;

        builder?.AppendContentNewline(
            $"{await player.GetNameAsync(false)} added 1 forces to {hex.ToHexNumberWithDieEmoji(game)} using {DisplayName}");
        
        ProduceOperations.CheckPlanetCapacity(game, hex);
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });
        
        return new SpaceWarInteractionOutcome(true);
    }
}