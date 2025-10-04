using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Discord.Commands;

public class GameplayCommands : IInteractionHandler<EndTurnInteraction>, IInteractionHandler<DeclineOptionalTriggersInteraction>, IInteractionHandler<SetPlayerStartingTechInteraction>
{
    [Command("ShowBoard")]
    [RequireGameChannel(RequireGameChannelMode.ReadOnly)]
    public static async Task ShowBoardStateCommand(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        if (game.Hexes.Count == 0)
        {
            outcome.RequiresSave = false;
            outcome.SetSimpleReply("No map has yet been generated for this game");
            return;
        }
        
        var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>();
        await GameFlowOperations.ShowBoardStateMessageAsync(builder, game);
        outcome.ReplyBuilder = builder;
    }

    [Command("Reprompt")]
    [Description("Repost whatever decision the game is currently waiting for, and ping the relevant player")]
    [RequireGameChannel(RequireGameChannelMode.ReadOnly)]
    public static async Task Reprompt(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>();
        await GameFlowOperations.ContinueResolvingEventStackAsync(builder, game, context.ServiceProvider);
        
        outcome.ReplyBuilder = builder;
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        EndTurnInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        if (game.CurrentTurnPlayer.GamePlayerId != interactionData.ForGamePlayerId)
        {
            builder?.AppendContentNewline("It looks like you are clicking an old end turn button. If you have lost the current buttons, try /reprompt");
        }
        
        // Make sure there's some record of this turn if they did nothing for some reason
        if (!game.AnyActionTakenThisTurn)
        {
            builder?.AppendContentNewline($"{await game.CurrentTurnPlayer.GetNameAsync(false)} ends their turn without taking any actions");
            builder?.NewMessage();
        }
        
        await GameFlowOperations.NextTurnAsync(builder, game, serviceProvider);
        
        // If we're ending the turn, delete the original turn action prompt message to condense the game history
        await (serviceProvider.GetRequiredService<SpaceWarCommandContextData>().InteractionMessage?.DeleteAsync() ?? Task.CompletedTask);

        return new SpaceWarInteractionOutcome(true, builder);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        DeclineOptionalTriggersInteraction interactionData,
        Game game,
        IServiceProvider serviceProvider)
    {
        await GameFlowOperations.DeclineOptionalTriggersAsync(builder, game, serviceProvider);
        return new SpaceWarInteractionOutcome(true, builder);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, SetPlayerStartingTechInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var discordUser = serviceProvider.GetRequiredService<SpaceWarCommandContextData>().User;
        var gamePlayer = game.TryGetGamePlayerByDiscordId(discordUser.Id);

        if (gamePlayer == null)
        {
            return new SpaceWarInteractionOutcome(false, builder);
        }
        
        await GameFlowOperations.SetPlayerStartingTechAsync(builder, game, gamePlayer, interactionData.TechId, serviceProvider);
        
        return new SpaceWarInteractionOutcome(true, builder);
    }
}