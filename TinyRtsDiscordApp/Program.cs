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
using SpaceWarDiscordApp;
using TinyRtsDiscordApp.Database;
using Tumult;
using Tumult.Database;
using Tumult.Database.Interactions;
using Tumult.Discord;
using Tumult.GameLogic;

namespace TinyRtsDiscordApp;

static class Program
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static FirestoreDb FirestoreDb { get; private set; }

    public static DiscordClient DiscordClient { get; private set; }

    private static readonly ThreadLocal<Random> _random = new(() => new Random());
    
    public static IReadOnlyDictionary<string, DiscordEmoji> AppEmojisByName { get; private set; }

    //public static CommonEmoji CommonEmoji;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    
    public static Random Random => _random.Value!;
    
    public static TextInfo TextInfo { get; } = new CultureInfo("en-GB", false).TextInfo;

    public static bool IsTestEnvironment { get; private set; }

    private static Task? _updateEmojiTask;

    public static BotErrorReporter BotErrorReporter { get; private set; } = null!;

    public static bool BotReady { get; private set; }

    static async Task Main()
    {
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            BotErrorReporter?.LogExceptionAsync(null, args.Exception).Wait();
        };
        
        var secrets = JsonConvert.DeserializeObject<Secrets>(await File.ReadAllTextAsync("Secrets.json"));
        if (secrets == null)
        {
            return;
        }

        IsTestEnvironment = secrets.IsTestEnvironment;

        //var converterRegistry = new ConverterRegistry { new ImageSharpColorCoordinateConverter() };
        var converterRegistry = new ConverterRegistry {};
        var typeDiscriminator = new TinyRtsTypeDiscriminator();

        foreach (var type in typeof(TinyRtsTypeDiscriminator).GetInterfaces()
                     .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IFirestoreTypeDiscriminator<>))
                     .Select(x => x.GetGenericArguments()[0]))
        {
            var typeT = Type.MakeGenericMethodParameter(0);
            typeof(ConverterRegistry).GetMethod(nameof(ConverterRegistry.Add), 1,
                    BindingFlags.Public | BindingFlags.Instance,
                    [typeof(IFirestoreTypeDiscriminator<>).MakeGenericType(typeT)])!
                .MakeGenericMethod(type)
                .Invoke(converterRegistry, [typeDiscriminator]);
        }

        var firestoreBuilder = new FirestoreDbBuilder
        {
            //EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly,
            ProjectId = secrets.FirestoreProjectId,
            ConverterRegistry = converterRegistry
            //Credential = SslCredentials.Insecure,
        };
        FirestoreDb = await firestoreBuilder.BuildAsync();

        var gameEventDispatcher = new TinyRtsGameEventDispatcher(FirestoreDb);
        var interactionDispatcher = new InteractionDispatcher<Game>(gameEventDispatcher);

        void RegisterEverything(object obj)
        {
            interactionDispatcher.RegisterInteractionHandler(obj);
            gameEventDispatcher.RegisterHandler(obj);
        }
        
        var discordBuilder = DiscordClientBuilder.CreateDefault(secrets.DiscordToken, DiscordIntents.AllUnprivileged);

        discordBuilder.ConfigureServices(x => 
        {
            //x.AddScoped<SpaceWarCommandContextData>();
            //x.AddScoped<SpaceWarCommandOutcome>();
            
            // List of interactions to set up
            x.AddScoped<List<InteractionData>>();

            //x.AddScoped<SpaceWarPerOperationState>();
            //x.AddScoped<PerOperationState>(sp => sp.GetRequiredService<SpaceWarPerOperationState>());
            //x.AddScoped<GameMessageBuilders>();

            x.AddHttpClient();
            x.AddSingleton<GameEventDispatcher<Game>>(gameEventDispatcher);
            x.AddSingleton(interactionDispatcher);
            //x.AddSingleton<GameCache<Game, NonDbGameState>>();
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
        }, new CommandsConfiguration
        {
            RegisterDefaultCommandProcessors = false,
            //CommandExecutor = new TinyRtsCommandExecutor()
        });
        
        /*foreach (var mapGeneratorType in Assembly.GetExecutingAssembly()
                     .GetTypes()
                     .Where(x => x.IsAssignableTo(typeof(BaseMapGenerator)) && !x.IsAbstract))
        {
            // Instance will register itself from the constructor
            var instance = Activator.CreateInstance(mapGeneratorType) as BaseMapGenerator ?? throw new Exception();
        }*/

        discordBuilder.ConfigureEventHandlers(builder =>
        {
            //builder.HandleInteractionCreated(InteractionDispatcher.HandleInteractionCreated);
            //builder.HandleMessageCreated(MessageHandler.HandleMessageCreated);
            builder.HandleGuildDownloadCompleted(GuildDownloadCompleted);
        });
        
        DiscordClient = discordBuilder.Build();
        BotErrorReporter = new BotErrorReporter(IsTestEnvironment, DiscordClient, secrets.UserToMessageErrorsTo);
        await BotErrorReporter.InitializeAsync();
        
        //CommonEmoji = new CommonEmoji(DiscordClient);
        
        await DiscordClient.ConnectAsync(new DiscordActivity("SpaceWar", DiscordActivityType.Playing));

        // Skip updating emoji every time in test because it makes startup slow and discord might get annoyed if we
        // spam the API that much
        /*if (!IsTestEnvironment)
        {
            Console.WriteLine("Updating emoji...");
            _updateEmojiTask = BotManagementOperations.UpdateEmojiAsync();
            Console.WriteLine("Emoji updated");
        }
        else
        {
            _updateEmojiTask = RebuildEmojiCache(); 
        }
        
        await _updateEmojiTask;*/
        
        BotReady = true;

        // Asynchronously iterate over all games and update their prod timers. This also pulls them all into the cache
        /*await foreach(var gameDoc in new Query<Game>(FirestoreDb.Games()).WhereEqualTo(x => x.Phase, GamePhase.Play)
                          .FirestoreQuery.StreamAsync())
        {
            using var disposable = await DiscordClient.ServiceProvider.GetRequiredService<GameSyncManager>().Locker
                .LockOrNullAsync(gameDoc.Reference, TimeSpan.FromMinutes(1));

            // If we can't obtain the lock for some reason, just skip this game
            if (disposable == null)
            {
                continue;
            }

            var cache = DiscordClient.ServiceProvider.GetRequiredService<GameCache<Game, NonDbGameState>>();
            
            if (!cache.GetGame(gameDoc.Reference, out var game, out var nonDbGameState))
            {
                try
                {
                    game = gameDoc.ConvertTo<Game>();
                }
                catch (Exception e) when(e is InvalidOperationException or ArgumentException)
                {
                    continue;
                }
                
                nonDbGameState = new NonDbGameState();
            }
            
            cache.AddOrUpdateGame(game, nonDbGameState);
            
            ProdOperations.UpdateProdTimers(game, nonDbGameState);
        }*/

        Console.WriteLine("Ready to go. Let's play some TinyRTS!");
        
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

        // We keep getting rate limited by discord on editing the channel in the test server
        /*if (!IsTestEnvironment)
        {
            foreach (var guild in arg.Guilds.Values)
            {
                await GuildOperations.UpdateServerTechListingAsync(guild);
            }
        }*/
    }
    
    public static async Task RebuildEmojiCache() => AppEmojisByName = (await DiscordClient.GetApplicationEmojisAsync()).ToDictionary(x => x.Name);
}