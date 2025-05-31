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
using SpaceWarDiscordApp.Database.InteractionData.Move;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp;

static class Program
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static FirestoreDb FirestoreDb { get; private set; }

    public static DiscordClient DiscordClient { get; private set; }

    private static readonly ThreadLocal<Random> _random = new(() => new Random());
    
    public static IReadOnlyDictionary<string, DiscordEmoji> AppEmojisByName { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    
    public static Random Random => _random.Value!;
    
    public static TextInfo TextInfo { get; } = new CultureInfo("en-GB", false).TextInfo;

    private static readonly Dictionary<Type, object> InteractionHandlers = new();

    static async Task Main()
    {
        TaskScheduler.UnobservedTaskException += (sender, args) =>
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
            x.AddHttpClient();
            x.AddScoped<OpenRouterService>(serviceProvider =>
            {
                var httpClient = serviceProvider.GetRequiredService<HttpClient>();
                return new OpenRouterService(httpClient, secrets.OpenRouterApiKey);
            });
        });
        discordBuilder.UseCommands((IServiceProvider serviceProvider, CommandsExtension extension) =>
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
            RegisterDefaultCommandProcessors = true,
            CommandExecutor = new SpaceWarCommandExecutor()
        });
        
        RegisterInteractionHandler(new MoveActionCommands());
        RegisterInteractionHandler(new ProduceActionCommands());
        RegisterInteractionHandler(new RefreshCommands());
        RegisterInteractionHandler(new TechCommands());
        RegisterInteractionHandler(new GameplayCommands());
        
        // Create tech singletons
        foreach (var techType in Assembly.GetExecutingAssembly()
                     .GetTypes()
                     .Where(x => x.IsAssignableTo(typeof(Tech)) && !x.IsAbstract))
        {
            var instance = Activator.CreateInstance(techType) as Tech ?? throw new Exception();
            RegisterInteractionHandler(instance);
            foreach (var handler in instance.AdditionalInteractionHandlers)
            {
                RegisterInteractionHandler(handler);
            }
        }

        discordBuilder.ConfigureEventHandlers(builder => builder.HandleInteractionCreated(HandleInteractionCreated));
        
        DiscordClient = discordBuilder.Build();
        await DiscordClient.ConnectAsync();
        
        AppEmojisByName = (await DiscordClient.GetApplicationEmojisAsync()).ToDictionary(x => x.Name);
        
        await Task.Delay(-1);
    }

    private static async Task HandleInteractionCreated(DiscordClient client, InteractionCreatedEventArgs args)
    {
        if (args.Interaction.Type == DiscordInteractionType.ApplicationCommand || !Guid.TryParse(args.Interaction.Data.CustomId, out _))
        {
            return;
        }

        var snapshot = (await new Query<InteractionData>(FirestoreDb.InteractionData()).WhereEqualTo(x => x.InteractionId, args.Interaction.Data.CustomId)
            .Limit(1)
            .GetSnapshotAsync()).FirstOrDefault();

        if (snapshot == null)
        {
            throw new Exception("InteractionData not found");
        }

        var interactionData = snapshot.ConvertToPolymorphic<InteractionData>();

        if (interactionData.EditOriginalMessage)
        {
            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
        }
        else
        {
            await args.Interaction.DeferAsync();
        }

        var game = await FirestoreDb.RunTransactionAsync(transaction => transaction.GetGameForChannelAsync(args.Interaction.ChannelId));

        if (game == null)
        {
            throw new Exception("Game not found");
        }

        var player = game.GetGamePlayerByDiscordId(args.Interaction.User.Id);
        if (player == null)
        {
            // Player is not part of this game, can't click any buttons
            return;
        }

        if (!interactionData.PlayerAllowedToTrigger(game, player))
        {
            await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"{args.Interaction.User.Mention} you can't click this, it not for you!"));
            return;
        }

        var interactionType = interactionData.GetType();
        if (!InteractionHandlers.TryGetValue(interactionType, out var handler))
        {
            throw new Exception("Handler not found");
        }

        typeof(IInteractionHandler<>).MakeGenericType(interactionType)
            .GetMethod(nameof(IInteractionHandler<InteractionData>.HandleInteractionAsync))!.Invoke(handler, [interactionData, game, args]);
    }

    private static void RegisterInteractionHandler(object interactionHandler)
    {
        foreach (var interactionType in interactionHandler.GetType()
                     .GetInterfaces()
                     .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IInteractionHandler<>))
                     .Select(x => x.GetGenericArguments()[0]))
        {
            if (!InteractionHandlers.TryAdd(interactionType, interactionHandler))
            {
                throw new Exception($"Handler already registered for {interactionType}");
            }
        }
        
    }
}