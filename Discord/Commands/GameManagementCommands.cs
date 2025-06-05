using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Discord.Commands;

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
    public static async Task CreateGameCommand(CommandContext context, int dummyPlayers = 0)
    {
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
                DummyPlayerName = DummyPlayerNames[game.Players.Count % DummyPlayerNames.Count],
                PlayerColour = PlayerColours[game.Players.Count % PlayerColours.Count],
                GamePlayerId = game.Players.Max(x => x.GamePlayerId) + 1
            });
        }
        
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Create(gameRef, game));
        
        await context.RespondAsync($"Game created. Game channel is {gameChannel.Mention}. To add more players, use /addplayer from that channel.");
        await context.Client.SendMessageAsync(gameChannel, $"Welcome to your new game, {Program.TextInfo.ToTitleCase(name)} {context.User.Mention}. To add more players use /addplayer from this channel.");
    }

    [Command("AddPlayer")]
    [RequireGameChannel]
    public static async Task AddPlayerToGameCommand(CommandContext context, DiscordMember user)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        
        await Program.FirestoreDb.RunTransactionAsync(transaction =>
        {
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
    [RequireGameChannel]
    public static async Task AddDummyPlayerToGameCommand(CommandContext context, string name = "")
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        
        await Program.FirestoreDb.RunTransactionAsync(transaction =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = DummyPlayerNames[game.Players.Count % DummyPlayerNames.Count];
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
    [RequireGameChannel]
    public static async Task StartGameCommand(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        
        if (game.Players.Count <= 1)
        {
            await context.RespondAsync("Not enough players");
        }

        if (game.Phase != GamePhase.Setup)
        {
            await context.RespondAsync("Game has already started");
        }
        
        var builder = new DiscordMessageBuilder().EnableV2Components();
        
        MapGenerator.GenerateMap(game);

        game.TechDeck = Tech.TechsById.Values.Select(x => x.Id).ToList();
        game.TechDeck.Shuffle();

        // Select universal techs at random
        for (var i = 0; i < GameConstants.UniversalTechCount; i++)
        {
            game.UniversalTechs.Add(game.DrawTechFromDeck());
        }

        for (var i = 0; i < GameConstants.MarketTechCount - 1; i++)
        {
            game.TechMarket.Add(game.DrawTechFromDeck());
        }
        
        game.TechMarket.Add(null);
        
        // Shuffle turn order
        game.Players = game.Players.Shuffled().ToList();
        game.ScoringTokenPlayerIndex = game.Players.Count - 1;
        game.Phase = GamePhase.Play;
        
        builder.AppendContentNewline("The game has started.");
        builder.AppendContentNewline("Universal Techs:".DiscordHeading2());
        foreach (var tech in game.UniversalTechs)
        {
            TechOperations.ShowTechDetails(builder, tech);
        }
        
        builder.AppendContentNewline("Tech Market:".DiscordHeading2());
        foreach (var tech in game.TechMarket.Where(x => x != null))
        {
            TechOperations.ShowTechDetails(builder, tech!);
        }
        
        await GameFlowOperations.ShowSelectActionMessageAsync(builder, game);
        
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
        
        await context.RespondAsync(builder);
    }

    [Command("Credits")]
    [Description("Who is to blame for this?")]
    public static async Task CreditsCommand(CommandContext context)
    {
        var builder = new DiscordMessageBuilder().EnableV2Components();
        builder.AppendContentNewline("Bot & game design by @Autoquark.");
        builder.AppendContentNewline("Image Credits".DiscordHeading3());
        builder.AppendContentNewline("Die icons by [Delapouite](https://delapouite.com/)");
        builder.AppendContentNewline("Science & star icons by [Lorc](https://lorcblog.blogspot.com/)");
        builder.AppendContentNewline("Other".DiscordHeading3());
        builder.AppendContentNewline("Thanks to @Xeddar for hosting and AI shenanigans, to everyone at PlaytestUK Sheffield for playtesting and to you for playing!");
        await context.RespondAsync(builder);
    }
}