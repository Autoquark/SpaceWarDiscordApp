using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech.Plagiarism;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_Plagiarism : Tech, IInteractionHandler<PlagiariseTechInteraction>
{
    public Tech_Plagiarism() : base("plagiarism", "Plagiarism",
        "Gain a tech that at least one other player owns.",
        "I think you'll find that the addition of an air freshener renders our giant space laser legally distinct under intergalactic patent conventions.",
        ["Free Action", "Single Use"])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) => GetTargetTechIds(game, player).Any();

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = GetTargetTechIds(game, player).ToTechsById().ToList();
        if (targets.Count == 0)
        {
            builder.AppendContentNewline("No techs are available to plagiarise.");
            return builder;
        }
        
        builder.AppendContentNewline($"{await player.GetNameAsync(true)}, choose a tech to plagiarise:");

        var interactionIds = serviceProvider.AddInteractionsToSetUp(targets.Select(x => new PlagiariseTechInteraction
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

        builder.AppendButtonRows(targets.Zip(interactionIds)
            .Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, x.Second, x.First.DisplayName))
            .Append(new DiscordButtonComponent(DiscordButtonStyle.Danger, cancelId, "Cancel")));
        
        return builder;
    }

    private IEnumerable<string> GetTargetTechIds(Game game, GamePlayer player)
        => game.Players.Except(player).SelectMany(x => x.Techs).Select(x => x.TechId).Distinct();

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, PlagiariseTechInteraction interactionData, Game game,
        IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        builder?.AppendContentNewline($"{await player.GetNameAsync(false)} plagiarised {interactionData.TechId}!");
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_PlayerGainTech
            {
                TechId = interactionData.TechId,
                PlayerGameId = interactionData.ForGamePlayerId
            },
            new GameEvent_PlayerLoseTech
            {
                TechId = Id,
                PlayerGameId = player.GamePlayerId
            },
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType
            });
        
        return new SpaceWarInteractionOutcome(true);
    }
}