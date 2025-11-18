using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp.Formats.Jpeg;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData.GameRules;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.MapGeneration;

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

        // Starting tech rule
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
        
        // Map generator
        builder.AppendContentNewline("Map Generator:".DiscordHeading2());

        interactionIds = serviceProvider.AddInteractionsToSetUp(BaseMapGenerator.GetAllGenerators().Select(x =>
            new SetMapGeneratorInteraction
            {
                Game = game.DocumentId,
                ForGamePlayerId = -1,
                GeneratorId = x.Id
            }));
        
        var currentGenerator = BaseMapGenerator.GetGenerator(game.Rules.MapGeneratorId);
        builder.AppendButtonRows(BaseMapGenerator.GetAllGenerators().Zip(interactionIds, (generator, interactionId) =>
            new DiscordButtonComponent(
                generator.Id == currentGenerator.Id
                    ? DiscordButtonStyle.Success
                    : DiscordButtonStyle.Secondary, interactionId, $"{generator.DisplayName} [{string.Join(",", generator.SupportedPlayerCounts)}]")
        ));

        if (!string.IsNullOrWhiteSpace(currentGenerator.Description))
        {
            builder.AppendContentNewline(currentGenerator.Description);
        }

        if (!BaseMapGenerator.GetGenerator(game.Rules.MapGeneratorId).SupportedPlayerCounts.Contains(game.Players.Count))
        {
            builder.AppendContentNewline(
                "Warning: The selected map generator does not support the current player count");
        }
        
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