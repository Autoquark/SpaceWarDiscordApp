using System.ComponentModel;
using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.GameRules;
using SpaceWarDiscordApp.Database.InteractionData.Tech.DimensionalOrigami;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Discord.Commands;

[RequireGameChannel(RequireGameChannelMode.RequiresSave, GamePhase.Setup)]
public class GameManagementCommands : IInteractionHandler<JoinGameInteraction>, IInteractionHandler<SetStartingTechRuleInteraction>,
    IInteractionHandler<SetMapGeneratorInteraction>,
    IInteractionHandler<ShowRollBackConfirmInteraction>,
    IInteractionHandler<RollBackGameInteraction>,
    IInteractionHandler<SetMaxPlayerCountInteraction>
{
    private class NounProjectImageCredit
    {
        public required string ImageName { get; set; }
        
        public required string ArtistName { get; set; }
    }
    
    private static readonly IReadOnlyList<string> NameAdjectives = new List<string>(["futile", "pointless", "childish",
        "regrettable", "silly", "absurd", "peculiar", "sudden", "endless", "unexpected", "undignified", "unnecessary", "ignoble",
        "infamous", "dishonourable", "sinister", "dire", "unfortunate", "stupid", "weird", "unusual", "unpredictable", "shameful",
        "interminable", "unending", "bizarre", "asinine", "awful", "rowdy", "lurid", "gruesome", "insufferable", "pungent",
        "unsavoury", "disagreeable", "scufflesome", "kerfufflesome", "disturbing", "frightful", "frightening", "farcical", "comical", "underwhelming",
        "perplexing", "inane", "insane", "mad", "maddening", "crazy", "bonkers", "wild", "fitful", "woeful", "furious", "furtive", "ghastly",
        "startling", "troublesome", "inconvenient", "dubious", "unwelcome", "unlikely", "improbable", "unbelievable", "implausible",
        "persnickety", "perfidious", "rambunctious", "troubling", "burdensome", "perturbing", "inaugural", "surprising", "tumultuous",
        "rambunctious", "off-putting", "bellicose", "undesirable"]);
    private static readonly IReadOnlyList<string> NameNouns = new List<string>(["war", "conflict", "battle", "disagreement",
        "fight", "confrontation", "scuffle", "kerfuffle", "brouhaha", "disturbance", "tiff", "fracas", "occurrence", "besmirchment",
        "brawl", "farce", "belligerence", "craziness", "lunacy", "fiasco", "furore", "faff", "perfidy", "quarrel", "tumult", "altercation",
        "rumpus", "happening", "thing", "violence", "event", "incident"]);
    private const string GameChannelCategoryName = "Spacewar Games";

    private static readonly IReadOnlyList<PlayerColour> PlayerColours = Enum.GetValues<PlayerColour>();

    private static readonly IReadOnlyList<string> DummyPlayerNames =
        new List<string>(["Lorelentenei", "Gerg", "Goodcoe", "Neutralcoe", "Zogak", "Benjermy", "Georgery"]);

    private static readonly IReadOnlyList<NounProjectImageCredit> NounProjectImageCredits =
    [
        new()
        {
            ImageName = "Arrow",
            ArtistName = "Rainbow Designs"
        },
        new()
        {
            ImageName = "Flying Flag",
            ArtistName = "AFY Studio"
        },
        new()
        {
            ImageName = "Chevron Double Up",
            ArtistName = "tezar tantular"
        },
        new()
        {
            ImageName = "Chevron Double Up Line",
            ArtistName = "tezar tantular"
        },
        new()
        {
            ImageName = "Trophy Cup",
            ArtistName = "tezar tantular"
        }
    ];
    
    [Command("CreateGame")]
    [RequireGuild]
    [RequireGameChannel(RequireGameChannelMode.DoNotRequire)]
    public static async Task CreateGameCommand(CommandContext context, int maxPlayers = 6, int dummyPlayers = 0)
    {
        var name = $"The {NameAdjectives[Program.Random.Next(0, NameAdjectives.Count)]} {NameNouns[Program.Random.Next(0, NameNouns.Count)]}";
        
        var channelName = name.ToLowerInvariant().Replace(" ", "-");
        var category = (await context.Guild!.GetChannelsAsync()).FirstOrDefault(x => x.Name == GameChannelCategoryName)
                       ?? await context.Guild.CreateChannelCategoryAsync(GameChannelCategoryName);
        var gameChannel = await context.Guild.CreateTextChannelAsync(channelName, category);
        
        var game = new Game
        {
            DocumentId = Program.FirestoreDb.Games().Document(),
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
            GameChannelId = gameChannel.Id
        };
        
        game.Rules.MaxPlayers = maxPlayers;

        for (var i = 0; i < dummyPlayers; i++)
        {
            game.Players.Add(new GamePlayer
            {
                DummyPlayerName = DummyPlayerNames[game.Players.Count % DummyPlayerNames.Count],
                PlayerColour = PlayerColours[game.Players.Count % PlayerColours.Count],
                GamePlayerId = game.Players.Max(x => x.GamePlayerId) + 1
            });
        }

        var joinGameInteraction = (new JoinGameInteraction
        {
            Game = game.DocumentId,
            ForGamePlayerId = -1,
            EphemeralResponse = true
        });

        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder;
        builder.AppendContentNewline($"Game created. Game channel is {gameChannel.Mention}.")
            .AppendContentNewline("Anyone can join the game by clicking 'join game'. When all players have joined, click 'start game' to begin: ")
            .AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Success, joinGameInteraction.InteractionId, "Join Game"));

        var startGameInteraction = new StartGameInteraction
        {
            ForGamePlayerId = -1,
            Game = game.DocumentId
        };
        
        await gameChannel.SendMessageAsync($"Welcome to your new game, {Program.TextInfo.ToTitleCase(name)} {context.User.Mention}. To invite specific players use /invite from this channel.");
        await gameChannel.SendMessageAsync(x => x.AppendContentNewline("Anyone can join by clicking this button:")
            .AppendButtonRows(new DiscordButtonComponent(DiscordButtonStyle.Success, startGameInteraction.InteractionId, "Start Game")
            , new DiscordButtonComponent(DiscordButtonStyle.Primary, joinGameInteraction.InteractionId, "Join Game")));
        
        game.PinnedTechMessageId = (await gameChannel.SendMessageAsync(x => x.EnableV2Components().AppendContentNewline("(This message reserved for future use)"))).Id;
        await GameManagementOperations.CreateOrUpdateGameSettingsMessageAsync(game, context.ServiceProvider);
        
        await Program.FirestoreDb.RunTransactionAsync(transaction =>
        {
            transaction.Create(game.DocumentId, game);
            InteractionsHelper.SetUpInteractions([startGameInteraction, joinGameInteraction],
                transaction,
                context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData.InteractionGroupId);
        });
    }

    [Command("Invite")]
    [RequireGameChannel(RequireGameChannelMode.ReadOnly)]
    public static async Task InvitePlayerToGameCommand(CommandContext context, DiscordMember user)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;

        if (game.Phase != GamePhase.Setup)
        {
            await context.RespondAsync("You can't invite players once the game has started");
            return;
        }

        if (game.TryGetGamePlayerByDiscordId(user.Id) != null)
        {
            await context.RespondAsync("That player is already in the game");
            return;
        }

        var interactionId = context.ServiceProvider.AddInteractionToSetUp(new JoinGameInteraction
        {
            Game = game.DocumentId,
            ForDiscordUserId = user.Id,
            ForGamePlayerId = -1
        });
        
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline($"{user.Mention}, you have been invited to join this game. To accept, click this button:")
            .WithAllowedMention(new UserMention(user.Id))
            .AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Success, interactionId, "Join Game"));
    }

    [Command("LeaveGame")]
    [Description("Leave a game during setup")]
    [RequireGamePlayer]
    public static async Task LeaveGameCommand(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        
        var player = game.GetGamePlayerByDiscordId(context.User.Id);
        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!;
        builder.AppendContentNewline($"{await player.GetNameAsync(false)} left the game");
        
        game.Players.RemoveAll(x => x.DiscordUserId == context.User.Id);
        
        await GameManagementOperations.CreateOrUpdateGameSettingsMessageAsync(game, context.ServiceProvider);
    }

    [Command("AddDummyPlayer")]
    [Description("Adds a dummy player to the game. Dummy players can be controlled by anyone in the game.")]
    public static async Task AddDummyPlayerToGameCommand(CommandContext context, string name = "")
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        
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
        
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline($"Dummy player {name} added to the game");
        
        await GameManagementOperations.CreateOrUpdateGameSettingsMessageAsync(game, context.ServiceProvider);
    }

    [Command("StartGame")]
    public static async Task StartGameCommand(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder;
        
        await GameFlowOperations.StartGameAsync(builder, game, context.ServiceProvider);
    }

    [Command("Credits")]
    [Description("Who is to blame for this?")]
    [RequireGameChannel(RequireGameChannelMode.DoNotRequire)]
    public static async Task CreditsCommand(CommandContext context)
    {
        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder;
        builder.AppendContentNewline("Bot & game design by @Autoquark.");
        builder.AppendContentNewline("Image Credits".DiscordHeading3());
        builder.AppendContentNewline("Die icons by [Delapouite](https://delapouite.com/)");
        builder.AppendContentNewline("Science & star icons by [Lorc](https://lorcblog.blogspot.com/)");
        var text = new StringBuilder();
        text.AppendJoin("\n", NounProjectImageCredits.Select(x => $"\"{x.ImageName}\" image by {x.ArtistName} from thenounproject.com"));
        builder.AppendContentNewline(text.ToString());
        builder.AppendContentNewline("Other".DiscordHeading3());
        builder.AppendContentNewline("Thanks to @Xeddar for hosting and AI shenanigans, @Jamespellis for additional coding, everyone at PlaytestUK Sheffield for playtesting and to you for playing!");
    }

    [Command("Lore")]
    [Description("Posts some lore about the SpaceWar setting")]
    [RequireGameChannel(RequireGameChannelMode.DoNotRequire)]
    public static async Task Lore(CommandContext context)
    {
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder
            .AppendContentNewline(context.ServiceProvider.GetRequiredService<BackstoryGenerator>()
            .GenerateBackstory(null));
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, JoinGameInteraction interactionData, Game game,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        var builders = serviceProvider.GetRequiredService<GameMessageBuilders>();
        var userId = serviceProvider.GetRequiredService<SpaceWarCommandContextData>().User.Id;
        if (game.TryGetGamePlayerByDiscordId(userId) != null)
        {
            builders.SourceChannelBuilder.AppendContentNewline("You are already in this game!");
            return new SpaceWarInteractionOutcome(false);
        }

        if (game.Players.Count >= game.Rules.MaxPlayers)
        {
            builders.SourceChannelBuilder.AppendContentNewline("This game is full!");
            return new SpaceWarInteractionOutcome(false);
        }

        if (game.Phase != GamePhase.Setup)
        {
            builders.SourceChannelBuilder.AppendContentNewline("You can't join this game because it has already started");
            return new SpaceWarInteractionOutcome(false);
        }
        
        game.Players.Add(new GamePlayer
        {
            DiscordUserId = userId,
            GamePlayerId = game.Players.Max(x => x.GamePlayerId) + 1,
            PlayerColour = PlayerColours.First(x => game.Players.All(y => y.PlayerColour != x))
        });
        
        var user = await Program.DiscordClient.GetUserAsync(userId);

        // Hack: If discord id is 0, we are replying outside the game channel. Do an ephemeral response here and proper response
        // in game channel
        if (interactionData.ForDiscordUserId == 0)
        {
            builders.SourceChannelBuilder.AppendContentNewline("Game joined!");
        }
        
        builders.GameChannelBuilder!.NewMessage()
            .AppendContentNewline($"{user.Mention} joined the game!")
            .WithAllowedMention(new UserMention(userId));
        
        await GameManagementOperations.CreateOrUpdateGameSettingsMessageAsync(game, serviceProvider);
        
        return new SpaceWarInteractionOutcome(true);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, SetStartingTechRuleInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        if (game.Phase != GamePhase.Setup)
        {
            builder?.AppendContentNewline("You can't change the starting tech rule after the game has started");
            return new SpaceWarInteractionOutcome(false);       
        }
        
        game.Rules.StartingTechRule = interactionData.Value;
        await GameManagementOperations.CreateOrUpdateGameSettingsMessageAsync(game, serviceProvider);

        return new SpaceWarInteractionOutcome(true)
        {
            DeleteOriginalMessage = true
        };
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, SetMapGeneratorInteraction interactionData, Game game,
        IServiceProvider serviceProvider)
    {
        if (game.Phase != GamePhase.Setup)
        {
            builder?.AppendContentNewline("You can't change the map generator after the game has started");
            return new SpaceWarInteractionOutcome(false);       
        }
        
        game.Rules.MapGeneratorId = interactionData.GeneratorId;
        await GameManagementOperations.CreateOrUpdateGameSettingsMessageAsync(game, serviceProvider);

        return new SpaceWarInteractionOutcome(true)
        {
            DeleteOriginalMessage = true
        };
    }
    
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, SetMaxPlayerCountInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        if (game.Phase != GamePhase.Setup)
        {
            builder?.AppendContentNewline("You can't change the max player count after the game has started");
            return new SpaceWarInteractionOutcome(false);       
        }

        game.Rules.MaxPlayers = interactionData.MaxPlayerCount;
        await GameManagementOperations.CreateOrUpdateGameSettingsMessageAsync(game, serviceProvider);

        return new SpaceWarInteractionOutcome(true)
        {
            DeleteOriginalMessage = true
        };
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ShowRollBackConfirmInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var rollbackInteractionId = serviceProvider.AddInteractionToSetUp(new RollBackGameInteraction
        {
            BackupGameDocument = interactionData.BackupGameDocument,
            ForGamePlayerId = interactionData.ForGamePlayerId,
            Game = game.DocumentId
        });

        var sourceBuilder = serviceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder;
        var state = game.RollbackStates.FirstOrDefault(x => x.GameDocument.Equals(interactionData.BackupGameDocument));

        if (state == null)
        {
            sourceBuilder.AppendContentNewline("That backup state seems to no longer exist");
            return new SpaceWarInteractionOutcome(false);
        }

        var turnPlayerName = await game.GetGamePlayerByGameId(state.CurrentTurnGamePlayerId).GetNameAsync(false, false);

        sourceBuilder.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Danger, rollbackInteractionId,
            $"Confirm roll back to start of turn {state.TurnNumber} ({turnPlayerName}'s turn)"));

        return new SpaceWarInteractionOutcome(false);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, RollBackGameInteraction interactionData, Game game,
        IServiceProvider serviceProvider)
    {
        var newGame = await GameManagementOperations.RollBackGameAsync(game,
            game.RollbackStates.First(x => x.GameDocument.Equals(interactionData.BackupGameDocument)),
            serviceProvider);
        
        var turnPlayerName = await newGame.CurrentTurnPlayer.GetNameAsync(false, false);
        builder!.AppendContentNewline($"Rolled back game to start of turn {newGame.TurnNumber} ({turnPlayerName}'s turn)".DiscordHeading2());

        var hasEvents = newGame.EventStack.Count > 0;
        await GameFlowOperations.ContinueResolvingEventStackAsync(builder, newGame, serviceProvider);

        return new SpaceWarInteractionOutcome(hasEvents);
    }
}