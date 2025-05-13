// See https://aka.ms/new-console-template for more information

using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using SpaceWarDiscordApp.Commands;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.Converters;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.DatabaseModels;

namespace SpaceWarDiscordApp;

static class Program
{
    public static FirestoreDb FirestoreDb { get; private set; }

    public static DiscordClient DiscordClient { get; private set; }

    private static readonly ThreadLocal<Random> _random = new(() => new Random());
    
    public static Random Random => _random.Value!;
    
    public static TextInfo TextInfo { get; } = new CultureInfo("en-GB", false).TextInfo;

    private static readonly Dictionary<Type, object> InteractionHandlers = new();

    static async Task Main()
    {
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Console.WriteLine(args);
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

        discordBuilder.UseCommands((IServiceProvider serviceProvider, CommandsExtension extension) =>
        {
            extension.AddCommands([typeof(GameManagementCommands), typeof(GameplayCommands)]);
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
            DebugGuildId = secrets.TestGuildId,
        });
        
        // We are actually registering this as a handler for multiple interaction types,
        // but to make the call compile we have to specify one
        RegisterInteractionHandler<ShowMoveOptionsInteraction>(new MoveActionCommands());

        discordBuilder.ConfigureEventHandlers(builder => builder.HandleInteractionCreated(HandleInteractionCreated));
        
        DiscordClient = discordBuilder.Build();
        await DiscordClient.ConnectAsync();
        
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

        var typeName = snapshot.GetValue<string>(nameof(InteractionData.SubtypeName));
        var type = Type.GetType(typeName);

        if (type == null)
        {
            throw new Exception($"InteractionData subtype {typeName} not found");
        }

        var interactionData = (InteractionData)typeof(DocumentSnapshot).GetMethod(nameof(DocumentSnapshot.ConvertTo))!.MakeGenericMethod(type)
            .Invoke(snapshot, [])!;

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

        if (!interactionData.PlayerAllowedToTrigger(player))
        {
            await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"{args.Interaction.User.Mention} you can't click this, it not for you!"));
            return;
        }

        if (!InteractionHandlers.TryGetValue(type, out var handler))
        {
            throw new Exception("Handler not found");
        }

        typeof(IInteractionHandler<>).MakeGenericType(type)
            .GetMethod(nameof(IInteractionHandler<InteractionData>.HandleInteractionAsync))!.Invoke(handler, [interactionData, game, args]);
    }

    private static void RegisterInteractionHandler<T>(IInteractionHandler<T> interactionHandler) where T : InteractionData
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