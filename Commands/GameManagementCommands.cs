using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using SpaceWarDiscordApp.DatabaseModels;

namespace SpaceWarDiscordApp.Commands;

public static class GameManagementCommands
{
    private static readonly IReadOnlyList<string> NameAdjectives = new List<string>(["futile", "pointless", "childish", "regrettable", "silly", "absurd", "peculiar", "sudden", "endless"]);
    private static readonly IReadOnlyList<string> NameNouns = new List<string>(["war", "conflict", "battle", "disagreement", "fight", "confrontation", "scuffle", "kerfuffle", "brouhaha", "disturbance"]);
    private static string GameChannelCategoryName = "Spacewar Games";
    
    [Command("CreateGame")]
    [RequireGuild]
    public static async Task CreateGame(CommandContext context)
    {
        var name = $"The {NameAdjectives[Program.Random.Next(0, NameAdjectives.Count)]} {NameNouns[Program.Random.Next(0, NameNouns.Count)]}";
        
        var channelName = name.ToLowerInvariant().Replace(" ", "-");
        var category = (await context.Guild!.GetChannelsAsync()).FirstOrDefault(x => x.Name == GameChannelCategoryName)
                       ?? await context.Guild.CreateChannelCategoryAsync(GameChannelCategoryName);
        var gameChannel = await context.Guild.CreateTextChannelAsync(channelName, category);

        await context.DeferResponseAsync();
        await Program.FirestoreDb.RunTransactionAsync(async transaction =>
        {
            DocumentReference gameRef = transaction.Database.Collection("Games").Document();
            transaction.Create(gameRef, new Game
            {
                Name = name,
                Players = [new GamePlayer
                {
                    DiscordUserId = context.User.Id,
                    GamePlayerId = 1
                }],
                GameChannelId = gameChannel.Id,
                Hexes = [new BoardHex
                {
                    Planet = new Planet
                    {
                        ForcesPresent = 3,
                        OwningPlayerId = 1,
                        Production = 3,
                        Science = 1,
                        Stars = 2
                    }
                }]
            });
        });
        
        await context.RespondAsync($"Game created. Game channel is {gameChannel.Mention}. To add more players, use /addplayer from that channel.");
        await context.Client.SendMessageAsync(gameChannel, $"Welcome to your new game, {name} {context.User.Mention}. To add more players use /addplayer from this channel.");
    }

    [Command("AddPlayer")]
    [RequireGuild]
    public static async Task AddPlayerToGame(CommandContext context, DiscordMember user)
    {
        await context.DeferResponseAsync();
        
        await Program.FirestoreDb.RunTransactionAsync(async transaction =>
        {
            var game = await transaction.GetGameForChannelAsync(context.Channel.Id);
            if (game == null)
            {
                await context.RespondAsync("This command must be used from a game channel");
                return;
            }
            
            game.Players.Add(new GamePlayer
            {
                DiscordUserId = user.Id,
                GamePlayerId = game.Players.Max(x => x.GamePlayerId) + 1
            });
            
            transaction.Set(game);
        });
        
        await context.RespondAsync($"{user.Mention} added to the game");
    }

    [Command("StartGame")]
    [RequireGuild]
    public static async Task StartGame(CommandContext context)
    {
        await context.DeferResponseAsync();

        var game = await Program.FirestoreDb.RunTransactionAsync(async transaction =>
        {
            var game = await transaction.GetGameForChannelAsync(context.Channel.Id);
            if (game == null)
            {
                await context.RespondAsync("This command must be used from a game channel");
                return null;
            }
            
            if (game.Players.Count <= 1)
            {
                await context.RespondAsync("Not enough players");
            }

            // Shuffle turn order
            game.Players = game.Players.Shuffled().ToList();
            game.ScoringTokenPlayerIndex = game.Players.Count - 1;
            game.Phase = GamePhase.Play;

            transaction.Set(game);

            return game;
        });

        if (game == null)
        {
            return;
        }
        
        await context.RespondAsync($"The game has started.");
        await context.RespondAsync(GameplayCommands.CreateTurnBeginMessage(game));
    }
}