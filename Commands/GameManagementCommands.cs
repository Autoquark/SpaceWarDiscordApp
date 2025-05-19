using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using SixLabors.ImageSharp;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.DatabaseModels;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Commands;

public static class GameManagementCommands
{
    private static readonly IReadOnlyList<string> NameAdjectives = new List<string>(["futile", "pointless", "childish",
        "regrettable", "silly", "absurd", "peculiar", "sudden", "endless", "unexpected", "undignified", "unnecessary", "ignoble",
        "infamous", "dishonourable", "sinister", "dire", "unfortunate", "stupid", "weird", "unusual", "unpredictable", "shameful",
        "interminable", "unending", "bizarre", "asinine", "awful", "rowdy", "lurid", "gruesome", "insufferable", "pungent",
        "unsavoury", "disagreeable", "scufflesome", "kerfufflesome", "disturbing", "frightful", "frightening", "farcical", "comical", "underwhelming",
        "perplexing", "inane", "insane", "mad", "maddening", "crazy", "bonkers", "wild", "fitful", "woeful", "furious", "furtive", "ghastly",
        "startling", "troublesome", "inconvenient", "dubious", "unwelcome", "unlikely", "improbable", "unbelievable", "implausible",
        "persnickety", "perfidious", "rambunctious", "troubling", "burdensome"]);
    private static readonly IReadOnlyList<string> NameNouns = new List<string>(["war", "conflict", "battle", "disagreement",
        "fight", "confrontation", "scuffle", "kerfuffle", "brouhaha", "disturbance", "tiff", "fracas", "occurrence", "besmirchment",
        "brawl", "farce", "belligerence", "craziness", "lunacy", "fiasco", "furore", "faff", "perfidy", "quarrel"]);
    private const string GameChannelCategoryName = "Spacewar Games";

    private static readonly IReadOnlyList<PlayerColour> PlayerColours = Enum.GetValues<PlayerColour>();

    private static readonly IReadOnlyList<string> DummyPlayerNames =
        new List<string>(["Lorelentenei", "Gerg", "Goodcoe", "Neutralcoe", "Zogak", "Benjermy", "Georgery"]);
    
    private static readonly MapGenerator MapGenerator = new MapGenerator();
    
    [Command("CreateGame")]
    [RequireGuild]
    public static async Task CreateGame(CommandContext context, int dummyPlayers = 0)
    {
        await context.DeferResponseAsync();
        
        var name = $"The {NameAdjectives[Program.Random.Next(0, NameAdjectives.Count)]} {NameNouns[Program.Random.Next(0, NameNouns.Count)]}";
        
        var channelName = name.ToLowerInvariant().Replace(" ", "-");
        var category = (await context.Guild!.GetChannelsAsync()).FirstOrDefault(x => x.Name == GameChannelCategoryName)
                       ?? await context.Guild.CreateChannelCategoryAsync(GameChannelCategoryName);
        var gameChannel = await context.Guild.CreateTextChannelAsync(channelName, category);
        
        var gameRef = Program.FirestoreDb.Games().Document();
        var game = new Game
        {
            Name = name,
            Players =
            [
                new GamePlayer
                {
                    DiscordUserId = context.User.Id,
                    GamePlayerId = 1,
                    PlayerColour = PlayerColours[0]
                }
            ],
            GameChannelId = gameChannel.Id,
        };

        for (int i = 0; i < dummyPlayers; i++)
        {
            game.Players.Add(new GamePlayer
            {
                DummyPlayerName = DummyPlayerNames.Random(),
                PlayerColour = PlayerColours[game.Players.Count % PlayerColours.Count],
                GamePlayerId = game.Players.Max(x => x.GamePlayerId) + 1
            });
        }
        
        await Program.FirestoreDb.RunTransactionAsync(async transaction =>
        {
            transaction.Create(gameRef, game);
        });
        
        await context.RespondAsync($"Game created. Game channel is {gameChannel.Mention}. To add more players, use /addplayer from that channel.");
        await context.Client.SendMessageAsync(gameChannel, $"Welcome to your new game, {Program.TextInfo.ToTitleCase(name)} {context.User.Mention}. To add more players use /addplayer from this channel.");
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
                GamePlayerId = game.Players.Max(x => x.GamePlayerId) + 1,
                PlayerColour = PlayerColours[game.Players.Count % PlayerColours.Count]
            });
            
            transaction.Set(game);
        });
        
        await context.RespondAsync($"{user.Mention} added to the game");
    }

    [Command("AddDummyPlayer")]
    [Description("Adds a dummy player to the game. Dummy players can be controlled by anyone in the game.")]
    [RequireGuild]
    public static async Task AddDummyPlayerToGame(CommandContext context, string name = "")
    {
        await context.DeferResponseAsync();

        if (string.IsNullOrWhiteSpace(name))
        {
            name = DummyPlayerNames.Random();
        }
        
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
                GamePlayerId = game.Players.Max(x => x.GamePlayerId) + 1,
                PlayerColour = PlayerColours[game.Players.Count % PlayerColours.Count],
                DummyPlayerName = name
            });
            
            transaction.Set(game);
        });
        
        await context.RespondAsync($"Dummy player {name} added to the game");
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
            
            MapGenerator.GenerateMap(game);

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
        
        var builder = new DiscordMessageBuilder().EnableV2Components();
        builder.AppendContentNewline("The game has started.");
        await GameplayCommands.ShowTurnBeginMessageAsync(builder, game);
        await context.RespondAsync(builder);
    }
}