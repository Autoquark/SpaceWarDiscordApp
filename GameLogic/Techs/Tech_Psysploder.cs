using DSharpPlus.Entities;
using Raffinert.FuzzySharp.Extensions;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech.Psysploder;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_Psysploder : Tech, IInteractionHandler<ChoosePsysploderTargetInteraction>, IInteractionHandler<UsePsysploderInteraction>
{
    public Tech_Psysploder() : base("psysploder", "Psysploder",
        "Destroy any number of your forces on a planet, and an equal number of enemy forces on each adjacent planet.",
        "Some argue that the use of the psysploder in warefare is unethical. Personally I think those people exhibit deeply flawed thinking, which is probably why their heads keep exploding.",
        [TechKeyword.FreeAction, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }
    
    public IEnumerable<BoardHex> GetTargets(Game game, GamePlayer player) => game.Hexes.WhereOwnedBy(player);

    public override Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = GetTargets(game, player).ToList();
        
        if (targets.Count == 0)
        {
            builder.AppendContentNewline("No suitable targets");
            return Task.FromResult(builder);
        }
        
        builder.AppendContentNewline("Choose a planet to target:");
        
        var interactions = serviceProvider.AddInteractionsToSetUp(targets.Select(x => new ChoosePsysploderTargetInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId,
            Target = x.Coordinates
        }));

        var cancelInteraction = serviceProvider.AddInteractionToSetUp(new RepromptInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId
        });

        return Task.FromResult(builder.AppendHexButtons(game, targets, interactions, cancelInteraction));
    }
    
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ChoosePsysploderTargetInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Target);
        var player = game.GetGamePlayerForInteraction(interactionData);
        
        var interactions = serviceProvider.AddInteractionsToSetUp(CollectionExtensions.Between(1, hex.ForcesPresent)
            .Select(x => new UsePsysploderInteraction
            {
                Amount = x,
                Game = game.DocumentId,
                Target = hex.Coordinates,
                ForGamePlayerId = interactionData.ForGamePlayerId
            }));

        var cancelInteraction = serviceProvider.AddInteractionToSetUp(new RepromptInteraction
        {
            ForGamePlayerId = interactionData.ForGamePlayerId,
            Game = game.DocumentId
        });

        var affected = BoardUtils.GetNeighbouringHexes(game, hex)
            .WhereForcesPresent()
            .WhereNotOwnedBy(player)
            .ToList();
        
        builder?.AppendContentNewline($"{await player.GetNameAsync(true)}, choose how many forces to Psysplode on {hex.ToHexNumberWithDieEmoji(game)}");
        builder?.AppendContentNewline(
            affected.Count == 0
                ? "(will not affect any other planets!)"
                : $"(will affect {string.Join(", ", affected.Select(x => x.ToHexNumberWithDieEmoji(game)))})");

        builder?.AppendButtonRows(cancelInteraction, interactions.Index().Select(x =>
            new DiscordButtonComponent(DiscordButtonStyle.Primary, x.Item, (x.Index + 1).ToString())));
        
        return new SpaceWarInteractionOutcome(false);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, UsePsysploderInteraction interactionData, Game game,
        IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        player.GetPlayerTechById(Id).IsExhausted = true;

        var targetHex = game.GetHexAt(interactionData.Target);
        var affected = BoardUtils.GetNeighbouringHexes(game, targetHex).WhereForcesPresent()
            .WhereNotOwnedBy(player)
            .Append(targetHex);

        builder?.AppendContentNewline($"{await player.GetNameAsync(false)} is using Psysploder on {targetHex.ToHexNumberWithDieEmoji(game)}");
        foreach (var hex in affected)
        {
            var amount = GameFlowOperations.DestroyForces(game, hex, interactionData.Amount, player.GamePlayerId, ForcesDestructionReason.Tech, Id);
            builder?.AppendContentNewline($"Destroyed {amount} forces on {hex.ToHexNumberWithDieEmoji(game)}");
        }
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });

        return new SpaceWarInteractionOutcome(true);
    }
}