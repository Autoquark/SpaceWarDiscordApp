using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SixLabors.ImageSharp;
using SpaceWarDiscordApp.DatabaseModels;
using SpaceWarDiscordApp.ImageGeneration;

namespace SpaceWarDiscordApp.Commands;

public class GameplayCommands : IEventHandler<InteractionCreatedEventArgs>
{
    private static Regex InteractionIdRegex = new Regex(@"^([0-9a-zA-Z])\.(\d+)$");
    private static string BoardImageBucketName = "space-war-discord-app.board-images";
    
    private static string CreateInteractionId(string actionId, ulong allowedUserId = 0)
    {
        return $"{actionId}_{allowedUserId}";
    }

    private static void ParseInteractionId(string interactionId, out string actionId, out ulong? allowedUserId)
    {
        var match = InteractionIdRegex.Match(interactionId);
        actionId = match.Groups[0].Value;
        allowedUserId = ulong.Parse(match.Groups[1].Value);
        if (allowedUserId == 0)
        {
            allowedUserId = null;
        }
    }

    [Command("ShowBoard")]
    [RequireGuild]
    public static async Task ShowBoardState(CommandContext context)
    {
        await context.DeferResponseAsync();

        var game = await Program.FirestoreDb.RunTransactionAsync(async transaction => await transaction.GetGameForChannelAsync(context.Channel.Id));

        if (game == null)
        {
            await context.RespondAsync("This command must be used from a game channel");
            return;
        }

        using (var image = BoardImageGenerator.GenerateBoardImage(game))
        {
            await image.SaveAsBmpAsync("./test.bmp");
            var stream = new MemoryStream();
            await image.SaveAsPngAsync(stream);
            stream.Position = 0;

            var user = await context.Client.GetUserAsync(game.CurrentTurnPlayer.DiscordUserId);
            
            var messagebuilder = new DiscordMessageBuilder()
                .WithContent($"Board state for {game.Name} at turn {game.TurnNumber} ({user.GlobalName}'s turn)")
                .AddFile("board.png", stream);
            await context.RespondAsync(messagebuilder);
        }
    }
    
    public static DiscordMessageBuilder CreateTurnBeginMessage(Game game)
    {
        var builder = new DiscordMessageBuilder()
            .WithContent($"{game.CurrentTurnPlayer}, it is your turn. Choose an action:")
            .AddComponents(
                new DiscordButtonComponent(DiscordButtonStyle.Primary, CreateInteractionId("BeginMoveAction", game.CurrentTurnPlayer.DiscordUserId), "Move Action"),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, CreateInteractionId("BeginProduceAction", game.CurrentTurnPlayer.DiscordUserId), "Produce Action"),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, CreateInteractionId("RefreshAction", game.CurrentTurnPlayer.DiscordUserId), "Refresh Action")
            );
        return builder;
    }

    public Task HandleEventAsync(DiscordClient sender, InteractionCreatedEventArgs eventArgs)
    {
        if (eventArgs.Interaction.Type == DiscordInteractionType.Component)
        {
            ParseInteractionId(eventArgs.Interaction.Data.CustomId, out var actionId, out var allowedUserId);
        }

        return Task.CompletedTask;
    }
}