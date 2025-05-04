using System.Collections.Specialized;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SixLabors.ImageSharp;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.DatabaseModels;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.ImageGeneration;

namespace SpaceWarDiscordApp.Commands;

public class GameplayCommands : IInteractionHandler<ShowMoveOptionsInteraction>
{
    private static Regex InteractionIdRegex = new Regex(@"^([0-9a-zA-Z])\.(\d+)$");
    private static string BoardImageBucketName = "space-war-discord-app.board-images";
    
    private static string CreateInteractionId(string actionId, ulong allowedUserId = 0)
    {
        return $"{actionId}_{allowedUserId}";
    }

    private static void ParseInteractionId(string interactionId, out string actionId, out ulong? allowedUserId)
    {
        var match = InteractionIdRegex.Match(interactionId);
        actionId = match.Groups[0].Value;
        allowedUserId = ulong.Parse(match.Groups[1].Value);
        if (allowedUserId == 0)
        {
            allowedUserId = null;
        }
    }

    [Command("ShowBoard")]
    [RequireGuild]
    public static async Task ShowBoardState(CommandContext context)
    {
        await context.DeferResponseAsync();

        var game = await Program.FirestoreDb.RunTransactionAsync(async transaction => await transaction.GetGameForChannelAsync(context.Channel.Id));

        if (game == null)
        {
            await context.RespondAsync("This command must be used from a game channel");
            return;
        }

        if (game.Hexes.Count == 0)
        {
            await context.RespondAsync("No map has yet been generated for this game");
            return;
        }
        
        await context.RespondAsync(await CreateBoardStateMessageAsync(game));
    }

    public static async Task<DiscordMessageBuilder> CreateBoardStateMessageAsync(Game game)
    {
        using var image = BoardImageGenerator.GenerateBoardImage(game);
        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        var name = await game.CurrentTurnPlayer.GetNameAsync(false);
        return new DiscordMessageBuilder()
            .WithContent(
                $"Board state for {Program.TextInfo.ToTitleCase(game.Name)} at turn {game.TurnNumber} ({name}'s turn)")
            .AddFile("board.png", stream);
    }
    
    public static async Task<IList<DiscordMessageBuilder>> CreateTurnBeginMessagesAsync(Game game)
    {
        var name = await game.CurrentTurnPlayer.GetNameAsync(true);
        
        var interactionId = await InteractionsHelper.SetUpInteractionAsync(new ShowMoveOptionsInteraction()
        {
            Game = game.DocumentId,
            ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
            AllowedGamePlayerIds = game.CurrentTurnPlayer.IsDummyPlayer ? [] : [game.CurrentTurnPlayer.GamePlayerId]
        });
        
        var builder = new DiscordMessageBuilder()
            .WithContent($"{name}, it is your turn. Choose an action:")
            .AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Primary, interactionId, "Move Action"),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, CreateInteractionId("BeginProduceAction", game.CurrentTurnPlayer.DiscordUserId), "Produce Action"),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, CreateInteractionId("RefreshAction", game.CurrentTurnPlayer.DiscordUserId), "Refresh Action")
            );
        return [await CreateBoardStateMessageAsync(game), builder];
    }

    public Task HandleEventAsync(DiscordClient sender, InteractionCreatedEventArgs eventArgs)
    {
        if (eventArgs.Interaction.Type == DiscordInteractionType.Component)
        {
            ParseInteractionId(eventArgs.Interaction.Data.CustomId, out var actionId, out var allowedUserId);
        }

        return Task.CompletedTask;
    }

    public async Task HandleInteractionAsync(ShowMoveOptionsInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        ISet<BoardHex> destinations = new HashSet<BoardHex>();
        foreach (var fromHex in game.Hexes.Where(x => x.Planet?.OwningPlayerId == interactionData.ForGamePlayerId))
        {
            destinations.UnionWith(GetNeighbouringHexes(game, fromHex));
        }

        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var playerName = await player.GetNameAsync(true);
        var messageBuilder = new DiscordWebhookBuilder()
            .WithContent($"{playerName}, you may move to the following hexes: ");

        IDictionary<BoardHex, string>? interactionIds = null;
        await Program.FirestoreDb.RunTransactionAsync(async transaction =>
        {
            interactionIds = destinations.ToDictionary(
                x => x,
                x => InteractionsHelper.SetUpInteraction(new BeginMoveActionInteraction
                {
                    Game = game.DocumentId,
                    Destination = x.Coordinates,
                    AllowedGamePlayerIds = player.IsDummyPlayer ? [] : [player.GamePlayerId]
                }, transaction));
        });

        if (interactionIds == null)
        {
            throw new Exception();
        }
        
        foreach(var group in destinations.ZipWithIndices().GroupBy(x => x.Item2 / 5))
        {
            messageBuilder.AddActionRowComponent(
                new DiscordActionRowComponent(
                    group.Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, interactionIds[x.Item1], x.Item1.Coordinates.ToString()))));
        }
        
        await args.Interaction.EditOriginalResponseAsync(messageBuilder);
    }

    private static ISet<BoardHex> GetNeighbouringHexes(Game game, BoardHex hex)
    {
        var results = new HashSet<BoardHex>();
        
        // When exploring a hyperlane hex, we need to consider which neighbour we are coming from
        var toExplore = new Stack<(BoardHex hex, HexCoordinates from)>();
        foreach (var hexDirection in Enum.GetValues<HexDirection>())
        {
            var coordinates = hex.Coordinates + hexDirection;
            var neighbour = game.GetHexAt(coordinates);
            if (neighbour != null)
            {
                toExplore.Push((neighbour, coordinates));
            }
        }

        while (toExplore.Count > 0)
        {
            var (exploring, from) = toExplore.Pop();
            if (exploring.Planet != null)
            {
                results.Add(exploring);
                continue;
            }
            
            if (exploring.HyperlaneConnections.Any())
            {
                foreach (var connection in exploring.HyperlaneConnections)
                {
                    if (exploring.Coordinates + connection.First == from)
                    {
                        var neighbour = game.GetHexAt(exploring.Coordinates + connection.Second);
                        if (neighbour != null)
                        {
                            toExplore.Push((exploring, exploring.Coordinates));
                        }
                    }
                    else if (exploring.Coordinates + connection.Second == from)
                    {
                        var neighbour = game.GetHexAt(exploring.Coordinates + connection.First);
                        if (neighbour != null)
                        {
                            toExplore.Push((exploring, exploring.Coordinates));
                        }
                    }
                }
                continue;
            }
        }
        
        return results;
    }
}