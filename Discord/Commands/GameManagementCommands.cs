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

[RequireGameChannel(RequireGameChannelMode.RequiresSave)]
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
    [RequireGameChannel(RequireGameChannelMode.DoNotRequire)]
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
        
        await context.RespondAsync($"Game created. Game channel is {gameChannel.Mention}. To add more players, use /addplayer from that channel.");
        await gameChannel.SendMessageAsync($"Welcome to your new game, {Program.TextInfo.ToTitleCase(name)} {context.User.Mention}. To add more players use /addplayer from this channel.");
        game.PinnedTechMessageId = (await gameChannel.SendMessageAsync(x => x.EnableV2Components().AppendContentNewline("(This message reserved for future use)"))).Id;
        
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Create(gameRef, game));
    }

    [Command("AddPlayer")]
    public static async Task AddPlayerToGameCommand(CommandContext context, DiscordMember user)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        
        game.Players.Add(new GamePlayer
        {
            DiscordUserId = user.Id,
            GamePlayerId = game.Players.Max(x => x.GamePlayerId) + 1,
            PlayerColour = PlayerColours[game.Players.Count % PlayerColours.Count]
        });
        
        await context.RespondAsync($"{user.Mention} added to the game");
    }

    [Command("AddDummyPlayer")]
    [Description("Adds a dummy player to the game. Dummy players can be controlled by anyone in the game.")]
    public static async Task AddDummyPlayerToGameCommand(CommandContext context, string name = "")
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        
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
        
        outcome.SetSimpleReply($"Dummy player {name} added to the game");
    }

    [Command("StartGame")]
    public static async Task StartGameCommand(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        
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
            game.UniversalTechs.Add(TechOperations.DrawTechFromDeckSilent(game).Id);
        }

        for (var i = 0; i < GameConstants.MarketTechCount - 1; i++)
        {
            game.TechMarket.Add(TechOperations.DrawTechFromDeckSilent(game).Id);
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

        await TechOperations.UpdatePinnedTechMessage(game);
        
        await GameFlowOperations.ShowSelectActionMessageAsync(builder, game);
        
        outcome.ReplyBuilder = builder;
    }

    [Command("Credits")]
    [Description("Who is to blame for this?")]
    [RequireGameChannel(RequireGameChannelMode.DoNotRequire)]
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