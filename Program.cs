// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using Newtonsoft.Json;

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
        DiscordClient = discordBuilder.Build();
        await DiscordClient.ConnectAsync();
        await Task.Delay(-1);
    }
}