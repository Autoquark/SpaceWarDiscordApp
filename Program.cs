// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SpaceWarDiscordApp.AI.Services;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.Converters;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp;

static class Program
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static FirestoreDb FirestoreDb { get; private set; }

    public static DiscordClient DiscordClient { get; private set; }

    private static readonly ThreadLocal<Random> _random = new(() => new Random());
    
    public static IReadOnlyDictionary<string, DiscordEmoji> AppEmojisByName { get; private set; }

    public static CommonEmoji CommonEmoji;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    
    public static Random Random => _random.Value!;
    
    public static TextInfo TextInfo { get; } = new CultureInfo("en-GB", false).TextInfo;

    public static bool IsTestEnvironment { get; private set; } = false;

    private static Task? _updateEmojiTask;
    
    static async Task Main()
    {
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Console.WriteLine(args);
            foreach (var innerException in args.Exception.InnerExceptions)
            {
                Console.WriteLine(innerException);
            }
            
            Debugger.Break();
        };
        
        var secrets = JsonConvert.DeserializeObject<Secrets>(await File.ReadAllTextAsync("Secrets.json"));
        if (secrets == null)
        {
            return;
        }

        IsTestEnvironment = secrets.IsTestEnvironment;
        
        var firestoreBuilder = new FirestoreDbBuilder
        {
            //EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly,
            ProjectId = secrets.FirestoreProjectId,
            ConverterRegistry = new ConverterRegistry { new ImageSharpColorCoordinateConverter() }
            //Credential = SslCredentials.Insecure,
        };
        FirestoreDb = await firestoreBuilder.BuildAsync();
        
        var discordBuilder = DiscordClientBuilder.CreateDefault(secrets.DiscordToken, DiscordIntents.AllUnprivileged);

        discordBuilder.ConfigureServices(x => 
        {
            x.AddScoped<SpaceWarCommandContextData>();
            x.AddScoped<SpaceWarCommandOutcome>();
            
            // List of interactions to set up
            x.AddScoped<List<InteractionData>>();

            x.AddScoped<TransientGameState>();
            
            x.AddHttpClient();
            x.AddScoped<OpenRouterService>(serviceProvider =>
            {
                var httpClient = serviceProvider.GetRequiredService<HttpClient>();
                return new OpenRouterService(httpClient, secrets.OpenRouterApiKey);
            });
            x.AddSingleton<GameCache>();
            x.AddSingleton<GameSyncManager>();
        });
        discordBuilder.UseCommands((_, extension) =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            extension.AddCommands(assembly);
            extension.AddChecks(assembly);

            var commandProcessor = new SlashCommandProcessor();
            commandProcessor.AddConverters(assembly);
            extension.AddProcessor(commandProcessor);
            
            /*TextCommandProcessor textCommandProcessor = new(new()
            {
                // The default behavior is that the bot reacts to direct
                // mentions and to the "!" prefix. If you want to change
                // it, you first set if the bot should react to mentions
                // and then you can provide as many prefixes as you want.
                PrefixResolver = new DefaultPrefixResolver(true, "?", "&").ResolvePrefixAsync,
            });

            // Add text commands with a custom prefix (?ping)
            extension.AddProcessor(textCommandProcessor);*/
        }, new CommandsConfiguration()
        {
            // The default value is true, however it's shown here for clarity
            RegisterDefaultCommandProcessors = false,
            CommandExecutor = new SpaceWarCommandExecutor()
        });
        
        RegisterEverything(new GameManagementCommands());
        RegisterEverything(new MoveActionCommands());
        RegisterEverything(new ProduceCommands());
        RegisterEverything(new RefreshCommands());
        RegisterEverything(new TechCommands());
        RegisterEverything(new GameplayCommands());
        RegisterEverything(new FixupCommands());
        
        RegisterEverything(new GameFlowOperations());
        RegisterEverything(new ProduceOperations());
        RegisterEverything(new MovementOperations());
        
        // Create tech singletons
        foreach (var techType in Assembly.GetExecutingAssembly()
                     .GetTypes()
                     .Where(x => x.IsAssignableTo(typeof(Tech)) && !x.IsAbstract))
        {
            var instance = Activator.CreateInstance(techType) as Tech ?? throw new Exception();
            RegisterEverything(instance);
            foreach (var handler in instance.AdditionalHandlers)
            {
                RegisterEverything(handler);
            }
        }

        discordBuilder.ConfigureEventHandlers(builder =>
        {
            builder.HandleInteractionCreated(InteractionDispatcher.HandleInteractionCreated);
            builder.HandleMessageCreated(MessageHandler.HandleMessageCreated);
            builder.HandleGuildDownloadCompleted(GuildDownloadCompleted);
        });
        
        DiscordClient = discordBuilder.Build();
        
        CommonEmoji = new CommonEmoji(DiscordClient);
        
        await DiscordClient.ConnectAsync(new DiscordActivity("SpaceWar", DiscordActivityType.Playing));

        // Skip updating emoji every time in test because it makes startup slow and discord might get annoyed if we
        // spam the API that much
        if (!IsTestEnvironment)
        {
            Console.WriteLine("Updating emoji...");
            _updateEmojiTask = BotManagementOperations.UpdateEmojiAsync();
            Console.WriteLine("Emoji updated");
        }
        else
        {
            _updateEmojiTask = RebuildEmojiCache(); 
        }
        
        await _updateEmojiTask;
        
        Console.WriteLine("Ready to go. Let's play some SpaceWar!");
        
        await Task.Delay(-1);
    }

    private static async Task GuildDownloadCompleted(DiscordClient client, GuildDownloadCompletedEventArgs arg)
    {
        // There's probably a better way to do this
        while (_updateEmojiTask == null)
        {
            await Task.Delay(100);
        }
        await _updateEmojiTask;
        foreach (var guild in arg.Guilds.Values)
        {
            await GuildOperations.UpdateServerTechListingAsync(guild);
        }
    }

    public static async Task RebuildEmojiCache() => AppEmojisByName = (await DiscordClient.GetApplicationEmojisAsync()).ToDictionary(x => x.Name);
    
    private static void RegisterEverything(object obj)
    {
        InteractionDispatcher.RegisterInteractionHandler(obj);
        GameEventDispatcher.RegisterHandler(obj);
    }
}