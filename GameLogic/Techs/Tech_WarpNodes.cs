using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData.Tech.WarpNodes;
using SpaceWarDiscordApp.Database.Tech;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

// ReSharper disable once InconsistentNaming
public class Tech_WarpNodes : Tech,
    IInteractionHandler<WarpNodes_ChooseSourceInteraction>,
    IInteractionHandler<WarpNodes_ChooseDestinationInteraction>,
    IInteractionHandler<WarpNodes_ChooseAmountInteraction>
    
{
    public Tech_WarpNodes() : base("warpNodes", 
        "Warp Nodes", 
        "**Action**: Choose a planet you control. Move any number of forces to any number of adjacent planets.", 
        "They're nodes that are made out of warp. I really don't know how I can make this any simpler")
    {
        HasSimpleAction = true;
    }
    
    public override PlayerTech CreatePlayerTech(Game game, GamePlayer player) => new PlayerTech_WarpNodes
    {
        TechId = Id
    };

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
    {
        var playerTech = player.GetPlayerTechById<PlayerTech_WarpNodes>(Id);
        playerTech.MovedTo = [];
        
        var name = await player.GetNameAsync(true);
        builder.AppendContentNewline($"{name}, choose a hex to move from:");
        
        var sources = game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId && x.Planet.ForcesPresent > 0)
            .ToList();

        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(sources.Select(x =>
            new WarpNodes_ChooseSourceInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Source = x.Coordinates,
                Game = game.DocumentId
            }));
        
        return builder.AppendHexButtons(game, sources, interactionIds);
    }

    public async Task HandleInteractionAsync(WarpNodes_ChooseSourceInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var playerTech = player.GetPlayerTechById<PlayerTech_WarpNodes>(Id);
        playerTech.Source = interactionData.Source;

        var builder = new DiscordWebhookBuilder();

        await ShowChooseDestinationAsync(builder, game, player, game.GetHexAt(interactionData.Source));

        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));

        await args.Interaction.EditOriginalResponseAsync(builder);
    }

    public async Task HandleInteractionAsync(WarpNodes_ChooseDestinationInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var playerTech = player.GetPlayerTechById<PlayerTech_WarpNodes>(Id);
        var name = await player.GetNameAsync(true);

        // Null value indicates done making moves
        if (!interactionData.Destination.HasValue)
        {
            return;
        }

        var maxAmount = game.GetHexAt(playerTech.Source).Planet!.ForcesPresent;
        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(Enumerable.Range(0, maxAmount + 1).Select(x => new WarpNodes_ChooseAmountInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId,
            Amount = x,
            Destination = interactionData.Destination.Value
        }));
        
        var builder = new DiscordWebhookBuilder()
            .AppendContentNewline($"{name}, choose amount of forces to move:")
            .AppendButtonRows(interactionIds.ZipWithIndices().Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, x.item, x.index.ToString())));
        
        await args.Interaction.EditOriginalResponseAsync(builder);
    }

    public async Task HandleInteractionAsync(WarpNodes_ChooseAmountInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var playerTech = player.GetPlayerTechById<PlayerTech_WarpNodes>(Id);
        var source = game.GetHexAt(playerTech.Source);

        var builder = new DiscordWebhookBuilder().EnableV2Components();
        
        if (interactionData.Amount > 0)
        {
            await MovementOperations.ResolveMoveAsync(builder, game, player, new PlannedMove
            {
                Sources = [new SourceAndAmount { Source = source.Coordinates, Amount = interactionData.Amount }],
                Destination = interactionData.Destination
            });
            playerTech.MovedTo.Add(interactionData.Destination);
        }
        
        // If we could perform another Warp Nodes move, reprompt
        if (source.Planet?.ForcesPresent > 0 
            && source.Planet.OwningPlayerId == player.GamePlayerId
            && playerTech.MovedTo.Count < BoardUtils.GetNeighbouringHexes(game, source).Count)
        {
            // Go back to destination selection
            await ShowChooseDestinationAsync(builder, game, player, source); 
        }
        else
        {
            await GameFlowOperations.OnActionCompleted(builder, game, ActionType.Main);
        }
        
        await args.Interaction.EditOriginalResponseAsync(builder);
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
    }

    private async Task<TBuilder> ShowChooseDestinationAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player,
        BoardHex source) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var name = await player.GetNameAsync(true);
        var playerTech = player.GetPlayerTechById<PlayerTech_WarpNodes>(Id);
        var destinations = BoardUtils.GetNeighbouringHexes(game, source)
            // Can't move to the same planet more than once per usage of Warp Nodes
            .Where(x => !playerTech.MovedTo.Contains(x.Coordinates))
            .ToList();
        
        var interactionIds =
            await InteractionsHelper.SetUpInteractionsAsync(destinations.Select<BoardHex, HexCoordinates?>(x => x.Coordinates)
                // Add null for declining further moves
                .Append(null)
                .Select(x => new WarpNodes_ChooseDestinationInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Destination = x
            }));

        return builder.AppendContentNewline($"{name}, choose a planet to move to:")
            .AppendHexButtons(game, destinations, interactionIds)
            .AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Success, 
                    interactionIds.Last(), 
                    "Done making Warp Nodes moves"));
    }
}