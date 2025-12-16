using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Tech;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech.UniversalPatent;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_UniversalPatent : Tech, IInteractionHandler<UseUniversalPatentInteraction>
{
    public Tech_UniversalPatent() : base("universalPatent", "Universal Patent",
        "Research any currently available tech for free.",
        "I think you'll find we have exclusive rights to all properties deriving from 'Use of technology to solve or cause problems'",
        [TechKeyword.FreeAction, TechKeyword.SingleUse])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }

    private static IEnumerable<string> GetTargetTechIds(Game game, GamePlayer player) => game.TechMarket.WhereNonNull()
        .Where(x => !player.HasTech(x)).Concat(game.UniversalTechs.Where(x => !player.HasTech(x)));

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) => base.IsSimpleActionAvailable(game, player)
        && GetTargetTechIds(game, player).Any(); 

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = GetTargetTechIds(game, player).ToTechsById().ToList();
        if (targets.Count == 0)
        {
            builder.AppendContentNewline("No techs are available to gain.");
            return builder;
        }
        
        builder.AppendContentNewline($"{await player.GetNameAsync(true)}, choose a tech to gain:");
        
        var interactionIds = serviceProvider.AddInteractionsToSetUp(targets.Select(x => new UseUniversalPatentInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId,
            TechId = x.Id
        }));
        
        var cancelId = serviceProvider.AddInteractionToSetUp(new RepromptInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId
        });

        builder.AppendButtonRows(cancelId, targets.Zip(interactionIds)
            .Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, x.Second, x.First.DisplayName)));
        
        return builder;
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, UseUniversalPatentInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        var tech = TechsById[interactionData.TechId];
        builder?.AppendContentNewline($"{await player.GetNameAsync(false)} gained {tech.DisplayName} thanks to their {DisplayName}!");
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_PlayerGainTech
            {
                TechId = interactionData.TechId,
                PlayerGameId = interactionData.ForGamePlayerId,
                CycleMarket = true
            },
            new GameEvent_PlayerLoseTech
            {
                TechId = Id,
                PlayerGameId = player.GamePlayerId,
                Reason = LoseTechReason.SingleUse
            },
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType
            });
        
        return new SpaceWarInteractionOutcome(true);
    }
}