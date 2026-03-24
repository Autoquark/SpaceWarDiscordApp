using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.Interactions.Tech.CrustalCrutoniser;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_CrustalCrutoniser : Tech, ISpaceWarInteractionHandler<UseCrustalCrutoniserInteraction>
{
    public Tech_CrustalCrutoniser() : base("crustalCrutoniser", "Crustal Crutoniser",
        "Produce on a ready planet with at least one production (exhaust it as usual). Reduce that planet's production score by 1.",
        "We have great respect for the natural environment. Why else would we be going to so much effort to compress it into 1 metre cubes and pack it neatly into our trucks?",
        [TechKeyword.Exhaust, TechKeyword.FreeAction])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }

    protected IEnumerable<BoardHex> GetTargets(Game game, GamePlayer player) =>
        game.Hexes.WhereOwnedBy(player).Where(x => x.Planet is { IsExhausted: false, Production: > 0 });

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) => base.IsSimpleActionAvailable(game, player) && GetTargets(game, player).Any();

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = GetTargets(game, player).ToList();

        var interactionIds = serviceProvider.AddInteractionsToSetUp(targets.Select(x => new UseCrustalCrutoniserInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Target = x.Coordinates,
            Game = game.DocumentId
        }));

        builder.AppendContentNewline($"{await player.GetNameAsync(true)}, choose a planet to crustally crutonise:");
        builder.AppendHexButtons(game, targets, interactionIds);
        
        return builder;
    }

    public async Task<InteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, UseCrustalCrutoniserInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Target);
        
        var thisTech = GetThisTech(game.GetGamePlayerForInteraction(interactionData));
        thisTech.IsExhausted = true;
        
        var playerName = await game.GetGamePlayerByGameId(interactionData.ForGamePlayerId).GetNameAsync(true);
        builder?.AppendContentNewline($"{playerName} is accelerating production by crustally crutonising {hex.Coordinates}!");
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            ProduceOperations.CreateProduceEvent(game, interactionData.Target, true),
            new GameEvent_AlterPlanet
            {
                Coordinates = hex.Coordinates,
                ResponsibleTechId = Id,
                ProductionChange = -1
            },
            new GameEvent_ActionComplete
            {
                ActionType = ActionType.Free
            });

        hex.Planet!.Production--;
        
        return new InteractionOutcome(true);
    }
}