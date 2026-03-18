using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech.Annexation;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_Annexation : Tech, IInteractionHandler<UseAnnexationInteraction>
{
    public Tech_Annexation() : base("annexation", "Annexation",
        "Produce on a ready neutral planet that is next to one you control (exhaust it as usual. If it has neutral forces on it, combat will occur.)",
        "Congratulations! You have been freed from the shackles of your inefficient, corrupt former government. Please fill out your new citizenship forms in triplicate, being sure to attach the appropriate processing fee.",
        [TechKeyword.Action, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Main;
    }

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) =>
        base.IsSimpleActionAvailable(game, player)
        && GetTargets(game, player).Any();

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = GetTargets(game, player).ToList();

        if (targets.Count == 0)
        {
            builder.AppendContentNewline("No targets available");
            return builder;
        }

        var interactionIds = serviceProvider.AddInteractionsToSetUp(targets.Select(x => new UseAnnexationInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId,
            Target = x.Coordinates
        }));

        var cancelId = serviceProvider.AddInteractionToSetUp(new RepromptInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId
        });

        builder.AppendContentNewline($"{await player.GetNameAsync(true)}, choose a planet to annex: {targets.Count}");
        builder.AppendHexButtons(game, targets, interactionIds);
        builder.AppendCancelButton(cancelId);
        
        return builder;
    }

    private IEnumerable<BoardHex> GetTargets(Game game, GamePlayer player) => game.Hexes.WhereOwnedBy(player)
        .SelectMany(x => BoardUtils.GetNeighbouringHexes(game, x).Where(y => y is { IsNeutral: true, Planet.IsExhausted: false }))
        .Distinct();

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, UseAnnexationInteraction interactionData, Game game,
        IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        GetThisTech(player).IsExhausted = true;
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            ProduceOperations.CreateProduceEvent(game, interactionData.Target,
                overrideProducingPlayer: player),
                new GameEvent_ActionComplete
                {
                    ActionType = SimpleActionType
                });
        return new SpaceWarInteractionOutcome(true);
    }
}