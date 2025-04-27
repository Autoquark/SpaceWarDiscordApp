// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.Commands;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using SpaceWarDiscordApp.Commands;

namespace SpaceWarDiscordApp;

static class Program
{
    public static FirestoreDb FirestoreDb { get; set; }

    public static DiscordClient DiscordClient { get; set; }

    private static readonly ThreadLocal<Random> _random = new(() => new Random());
    
    public static Random Random => _random.Value;

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
            //Credential = SslCredentials.Insecure,
        };
        FirestoreDb = await firestoreBuilder.BuildAsync();
        
        var discordBuilder = DiscordClientBuilder.CreateDefault(secrets.DiscordToken, DiscordIntents.AllUnprivileged);

        discordBuilder.UseCommands((IServiceProvider serviceProvider, CommandsExtension extension) =>
        {
            extension.AddCommands([typeof(GameManagementCommands)]);
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
        
        DiscordClient = discordBuilder.Build();
        await DiscordClient.ConnectAsync();
        await Task.Delay(-1);
    }
}