using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.Interactions.Tech.HistoricalRevisionism;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_HistoricalRevisionism : Tech, ISpaceWarInteractionHandler<SelectHistoricalRevisionismFirstTargetInteraction>,
    ISpaceWarInteractionHandler<UseHistoricalRevisionismInteraction>
{
    public Tech_HistoricalRevisionism() : base("historical-revisionism", "Historical Revisionism",
        "Choose two adjacent planets with forces. Swap ownership of those planets and forces.",
        "We've issued some corrections to your personal record. Here's a photograph of your new next of kin and a quick primer on the language they probably speak.",
        [TechKeyword.Action, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
        SimpleActionIsOncePerTurn = true;
    }

    private IEnumerable<BoardHex> GetTargets(Game game, GamePlayer player) => game.Hexes
        .WhereForcesPresent()
        .Where(x => GetSecondTargets(game, x).Any());

    private IEnumerable<BoardHex> GetSecondTargets(Game game, BoardHex firstTarget) => BoardUtils
        .GetNeighbouringHexes(game, firstTarget)
        .WhereForcesPresent()
        .Where(x => x.Planet!.OwningPlayerId != firstTarget.Planet!.OwningPlayerId);

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) =>
        base.IsSimpleActionAvailable(game, player) && GetTargets(game, player).Any();

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = GetTargets(game, player).ToList();
        
        var interactionIds = serviceProvider.AddInteractionsToSetUp(targets.Select(x => new SelectHistoricalRevisionismFirstTargetInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Target = x.Coordinates,
            Game = game.DocumentId
        }));

        builder.AppendContentNewline($"{await player.GetNameAsync(true)}, choose the first planet to swap ownership of:");
        builder.AppendHexButtons(game, targets, interactionIds);
        
        return builder;
    }

    public async Task<InteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        SelectHistoricalRevisionismFirstTargetInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Target);
        var player = game.GetGamePlayerForInteraction(interactionData);
        var secondTargets = GetSecondTargets(game, hex).ToList();
        
        var interactionIds = serviceProvider.AddInteractionsToSetUp(secondTargets.Select(x => new UseHistoricalRevisionismInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            FirstTarget = hex.Coordinates,
            SecondTarget = x.Coordinates,
            Game = game.DocumentId
        }));

        builder?.AppendContentNewline($"{await player.GetNameAsync(true)}, choose the second planet to swap ownership of {hex.ToHexNumberWithDieEmoji(game)} with:");
        builder?.AppendHexButtons(game, secondTargets, interactionIds);
        
        return new InteractionOutcome(false);
    }

    public async Task<InteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, UseHistoricalRevisionismInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var hex1 = game.GetHexAt(interactionData.FirstTarget);
        var hex2 = game.GetHexAt(interactionData.SecondTarget);
        var player = game.GetGamePlayerForInteraction(interactionData);
        
        (hex1.Planet!.OwningPlayerId, hex2.Planet!.OwningPlayerId) = (hex2.Planet.OwningPlayerId, hex1.Planet.OwningPlayerId);
        
        var hex1NewOwner = game.GetGamePlayerByGameId(hex1.Planet.OwningPlayerId);
        var hex2NewOwner = game.GetGamePlayerByGameId(hex2.Planet.OwningPlayerId);

        builder?.AppendContentNewline(
            $"{await player.GetNameAsync(false)} has revealed that {hex1.ToHexNumberWithDieEmoji(game)} always belonged to {await hex1NewOwner.GetNameAsync(true)} and {hex2.ToHexNumberWithDieEmoji(game)} always belonged to {await hex2NewOwner.GetNameAsync(true)}.");
        
        GetThisTech(player).IsExhausted = true;

        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_CapturePlanet
            {
                Location = hex1.Coordinates, 
                FormerOwnerGameId = hex2NewOwner.GamePlayerId
            },
            new GameEvent_CapturePlanet
            {
                Location = hex2.Coordinates, 
                FormerOwnerGameId = hex1NewOwner.GamePlayerId
            },
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });
        
        return new InteractionOutcome(true);
    }
}