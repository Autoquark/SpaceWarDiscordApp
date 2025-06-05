using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData.Tech.AggressiveWasteDisposal;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_AggressiveWasteDisposal : Tech, IInteractionHandler<UseAggressiveWasteDisposalInteraction>
{
    public Tech_AggressiveWasteDisposal() : base("aggressiveWasteDisposal",
        "Aggressive Waste Disposal",
        "Free action, Exhaust: Destroy 1 forces on a planet adjacent to one you control.",
        "A primitive civilisation like theirs will probably appreciate these thousand ton containers of miscellaneous industrial refuse!")
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) => base.IsSimpleActionAvailable(game, player) && GetTargets(game, player).Any();

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
    {
        var targets = GetTargets(game, player).ToList();

        if (targets.Count == 0)
        {
            builder.AppendContentNewline("No targets available");
        }

        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(targets.Select(x =>
            new UseAggressiveWasteDisposalInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                EditOriginalMessage = true,
                Target = x.Coordinates
            }));
        
        return builder.AppendHexButtons(game, targets, interactionIds);
    }

    private static IEnumerable<BoardHex> GetTargets(Game game, GamePlayer player) => game.Hexes.WhereOwnedBy(player)
        .SelectMany(x => BoardUtils.GetNeighbouringHexes(game, x))
        .WhereForcesPresent()
        .DistinctBy(x => x.Coordinates);

    public async Task HandleInteractionAsync(UseAggressiveWasteDisposalInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var hex = game.GetHexAt(interactionData.Target);
        var player = game.GetGamePlayerForInteraction(interactionData);
        var tech = player.GetPlayerTechById(Id);

        if (!hex.AnyForcesPresent || tech.IsExhausted)
        {
            throw new Exception();
        }
        
        hex.Planet!.SubtractForces(1);
        tech.IsExhausted = true;
        
        var name = await player.GetNameAsync(false);
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        builder.AppendContentNewline($"{name} removed 1 forces from {hex.Coordinates} using Aggressive Waste Disposal");
        
        await GameFlowOperations.OnActionCompleted(builder, game, ActionType.Free);
        
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));

        await args.Interaction.EditOriginalResponseAsync(builder);
    }
}