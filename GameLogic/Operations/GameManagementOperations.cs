using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData.GameRules;
using SpaceWarDiscordApp.Discord;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public class GameManagementOperations
{
    public static async Task CreateOrUpdateGameSettingsMessageAsync(Game game, IServiceProvider serviceProvider)
    {
        var channel = await Program.DiscordClient.GetChannelAsync(game.GameChannelId);
        DiscordMessage? message = null;
        if (game.SetupMessageId != 0)
        {
            message = await channel.TryGetMessageAsync(game.SetupMessageId);
        }

        var builder = new DiscordMessageBuilder().EnableV2Components()
            .AppendContentNewline("Game Setup".DiscordHeading1())
            .AppendContentNewline("Starting Tech:".DiscordHeading2());
        
        var interactionIds = serviceProvider.AddInteractionsToSetUp(Enum.GetValues<StartingTechRule>().Select(x => new SetStartingTechRuleInteraction
        {
            ForGamePlayerId = -1,
            Game = game.DocumentId,
            Value = x,
        }));

        builder.AppendButtonRows(
            Enum.GetValues<StartingTechRule>()
                .Zip(interactionIds, (enumValue, interactionId) => new DiscordButtonComponent(
                    enumValue == game.Rules.StartingTechRule
                        ? DiscordButtonStyle.Success
                        : DiscordButtonStyle.Secondary,
                    interactionId,
                    Enum.GetName(enumValue)!.InsertSpacesInCamelCase())));
        
        if (message == null)
        {
            game.SetupMessageId = (await channel.SendMessageAsync(builder)).Id;
        }
        else
        {
            await message.ModifyAsync(builder);
        }
    }
}