using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord.ChoiceProvider;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Discord.Commands;

/// <summary>
/// Commands which can be used to manually edit the game state
/// </summary>
[Command("fixup")]
[RequireGameChannel(RequireGameChannelMode.RequiresSave)]
public class FixupCommands
{
    /// <summary>
    /// Set the number and/or owner of forces on a planet
    /// </summary>
    /// <param name="context"></param>
    /// <param name="coordinates"></param>
    /// <param name="amount">New amount of forces. Omit to keep existing number</param>
    /// <param name="player">New owner of forces. Omit to keep existing owner</param>
    [Command("setForces")]
    [Description("Set the number and/or owner of forces on a planet")]
    public static async Task SetForces(CommandContext context,
        [SlashAutoCompleteProvider<HexCoordsAutoCompleteProvider_WithPlanet>] HexCoordinates coordinates,
        [Description("New amount of forces. Omit to keep existing number")] int amount = -1,
        [Description("New owner of forces. Omit to keep existing owner or you if there's no existing owner")]
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>] int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        var hex = game.GetHexAt(coordinates);
        if (hex.Planet == null)
        {
            await context.RespondAsync($"Invalid coordinates {coordinates}");
            outcome.RequiresSave = false;
            return;
        }
        
        var newAmount = amount > -1 ? amount : hex.ForcesPresent;
        GamePlayer? newOwner = null;
        
        if (newAmount > 0)
        {
            newOwner = game.TryGetGamePlayerByGameId(player) ?? game.TryGetGamePlayerByGameId(hex.Planet.OwningPlayerId) ?? game.TryGetGamePlayerByDiscordId(context.User.Id);
            if (newOwner == null)
            {
                await context.RespondAsync("Must specify a player");
                outcome.RequiresSave = false;
                return;
            }
        }

        hex.Planet.SetForces(newAmount, newOwner?.GamePlayerId ?? GamePlayer.GamePlayerIdNone);

        outcome.SetSimpleReply(newOwner != null
            ? $"Set forces at {coordinates} to {newOwner.PlayerColourInfo.GetDieEmoji(hex.ForcesPresent)}"
            : $"Removed all forces from {coordinates}");
    }
    
    [Command("grantTech")]
    [Description("Grant a tech to a player")]
    public static async Task GrantTech(CommandContext context,
        [SlashAutoCompleteProvider<TechIdChoiceProvider>] string techId,
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>] int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        var gamePlayer = player == -1 ? game.TryGetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Unknown player");
            return;
        }

        if (gamePlayer.Techs.Any(x => x.TechId == techId))
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Player already has that tech");
            return;
        }

        if (!Tech.TechsById.TryGetValue(techId, out var tech))
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Unknown tech");
            return;
        }
        
        gamePlayer.Techs.Add(tech.CreatePlayerTech(game, gamePlayer));

        await TechOperations.UpdatePinnedTechMessage(game);
        
        var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>()
            .AppendContentNewline($"Granted {tech.DisplayName} to {await gamePlayer.GetNameAsync(true)}")
            .WithAllowedMentions(gamePlayer);
        
        outcome.ReplyBuilder = builder;
    }

    /// <summary>
    /// Removes a tech from a player.
    /// </summary>
    [Command("removeTech")]
    public static async Task RemoveTech(CommandContext context,
        [SlashAutoCompleteProvider<TechIdChoiceProvider>] string techId,
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>] int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        
        var gamePlayer = player == -1 ? game.TryGetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Unknown player");
            return;
        }

        if (!Tech.TechsById.TryGetValue(techId, out var tech))
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Unknown tech");
            return;
        }

        var index = gamePlayer.Techs.Items.Index().FirstOrDefault(x => x.Item.TechId == techId, (-1, null!)).Index;
        if (index == -1)
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Player does not have that tech");
            return;
        }
        gamePlayer.Techs.RemoveAt(index);
        
        var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>()
            .AppendContentNewline($"Removed {tech.DisplayName} from {await gamePlayer.GetNameAsync(true)}")
            .WithAllowedMentions(gamePlayer);
        
        outcome.ReplyBuilder = builder;
    }

    [Command("DiscardUniversalTech")]
    [Description("Puts a universal tech into the tech discards.")]
    public static async Task DiscardUniversalTech(CommandContext context,
        [SlashAutoCompleteProvider<UniversalTechIdChoiceProvider>] string techId)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        var techName = Tech.TechsById[techId].DisplayName;
        var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>();
        outcome.ReplyBuilder = builder;
        
        if (game.UniversalTechs.Remove(techId))
        {
            game.TechDiscards.Add(techId);
            builder.AppendContentNewline($"Removed {techName} from universal techs");
            outcome.RequiresSave = true;
        }
        else
        {
            builder.AppendContentNewline($"{techName} is not a universal tech");
            outcome.RequiresSave = false;
        }
    }

    [Command("refreshPinnedTechMessage")]
    [Description("Updates the pinned message with the details of all techs in use")]
    public static async Task RefreshPinnedTechMessage(CommandContext context)
    {
        await TechOperations.UpdatePinnedTechMessage(context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!);
        var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>()
            .AppendContentNewline($"Updated pinned tech message");
        
        var outcome = context.Outcome();
        outcome.ReplyBuilder = builder;
    }

    [Command("discardMarketTech")]
    [Description("Discards a tech from the tech market, leaving an empty slot")]
    public static async Task DiscardMarketTech(CommandContext context,
        [SlashAutoCompleteProvider<MarketTechIdChoiceProvider>]
        string techId,
        [Description("Whether to put the card into tech discards or entirely remove it from the game")]
        bool putInDiscard = true)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        var index = game.TechMarket.IndexOf(techId);
        if (index == -1)
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("That tech is not in the market");
            return;
        }
        
        var tech = Tech.TechsById[techId];
        game.TechMarket[index] = null;

        if (putInDiscard)
        {
            game.TechDiscards.Add(techId);
        }
        
        outcome.ReplyBuilder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>()
            .AppendContentNewline(putInDiscard 
                ? $"Put {tech.DisplayName} from the tech market into the discard pile"
                : $"Removed {tech.DisplayName} from the tech market (without putting it in the discard pile)");
    }
    
    [Command("setTechExhausted")]
    [Description("Exhausts or unexhausts a player's tech")]
    public static async Task SetTechExhausted(CommandContext context,
        [SlashAutoCompleteProvider<TechIdChoiceProvider>]
        string techId,
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>]
        int player = -1,
        bool exhausted = true)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        
        var gamePlayer = player == -1 ? game.TryGetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Unknown player");
            return;
        }

        if (!Tech.TechsById.TryGetValue(techId, out var tech))
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Unknown tech");
            return;
        }

        var playerTech = gamePlayer.TryGetPlayerTechById(techId);
        if (playerTech == null)
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Player does not have that tech");
            return;
        }
        playerTech.IsExhausted = exhausted;
        
        outcome.ReplyBuilder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>()
            .AppendContentNewline($"{(exhausted ? "Exhausted" : "Unexhausted")} {tech.DisplayName} for {await gamePlayer.GetNameAsync(true)}")
            .WithAllowedMentions(gamePlayer);;
    }

    [Command("setPlanetExhausted")]
    [Description("Exhausts or unexhausts a planet")]
    public static async Task SetPlanetExhausted(CommandContext context,
        [SlashAutoCompleteProvider<HexCoordsAutoCompleteProvider_WithPlanet>] HexCoordinates coordinates,
        bool exhausted = true)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        
        var hex = game.GetHexAt(coordinates);
        if (hex.Planet == null)
        {
            outcome.RequiresSave = false;
            await context.RespondAsync($"Invalid coordinates {coordinates}");
            return;
        }
        
        hex.Planet.IsExhausted = exhausted;
        
        outcome.SetSimpleReply($"{(exhausted ? "Exhausted" : "Unexhausted")} planet at {coordinates}");
    }

    [Command("SetPlayerTurn")]
    [Description("Set which player's turn it is")]
    public static async Task SetCurrentTurn(CommandContext context,
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>]
        int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        
        var gamePlayer = player == -1 ? game.TryGetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Unknown player");
            return;
        }

        var builder = new DiscordMultiMessageBuilder(() => new DiscordMessageBuilder());
        var previousPlayer = game.CurrentTurnPlayer;
        
        game.CurrentTurnPlayerIndex = game.Players.FindIndex(x => x.GamePlayerId == gamePlayer.GamePlayerId);
        game.ActionTakenThisTurn = false;
        game.IsWaitingForTechPurchaseDecision = false;
        
        builder.AppendContentNewline($"Set current turn to {await gamePlayer.GetNameAsync(true)} (was {await previousPlayer.GetNameAsync(true)})")
            .WithAllowedMentions(gamePlayer, previousPlayer);
        await GameFlowOperations.ShowSelectActionMessageAsync(builder, game, context.ServiceProvider);
        
        outcome.ReplyBuilder = builder;
    }

    [Command("SetScoringTokenPlayer")]
    [Description("Set which player holds the scoring token")]
    public static async Task SetScoringTokenPlayer(CommandContext context,
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>]
        int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        
        var gamePlayer = player == -1 ? game.TryGetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Unknown player");
            return;
        }

        var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>();
        var previousPlayer = game.ScoringTokenPlayer;
        
        game.ScoringTokenPlayerIndex = game.Players.FindIndex(x => x.GamePlayerId == gamePlayer.GamePlayerId);
        
        builder.AppendContentNewline($"Moved scoring token to {await gamePlayer.GetNameAsync(true)} (was {await previousPlayer.GetNameAsync(true)})")
            .WithAllowedMentions(gamePlayer, previousPlayer);
        
        outcome.ReplyBuilder = builder;
    }
    
    [Command("SetPlayerScience")]
    [Description("Set a player's science total")]
    public static async Task SetPlayerScience(CommandContext context,
        int science,
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>] int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        
        var gamePlayer = player == -1 ? game.TryGetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Unknown player");
            return;
        }
        
        var previous = gamePlayer.Science;
        gamePlayer.Science = science;
        
        outcome.ReplyBuilder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>()
            .AppendContentNewline(
                $"Set {await gamePlayer.GetNameAsync(true)}'s science to {science} (was {previous})")
            .WithAllowedMentions(gamePlayer);;
        
        game.ScoringTokenPlayerIndex = game.Players.FindIndex(x => x.GamePlayerId == gamePlayer.GamePlayerId);
    }
    
    [Command("SetPlayerVp")]
    [Description("Set a player's Victory Points")]
    public static async Task SetPlayerVictoryPoints(CommandContext context,
        int vp,
        [SlashAutoCompleteProvider<GamePlayerIdChoiceProvider>]
        int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        
        var gamePlayer = player == -1 ? game.TryGetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Unknown player");
            return;
        }
        
        var previous = gamePlayer.VictoryPoints;
        gamePlayer.VictoryPoints = vp;
        
        // If nobody has now won, unfinish the game if it was finished
        if (game.Players.All(x => x.VictoryPoints < GameConstants.VpToWin) &&
            game.Players.Count(x => !x.IsEliminated) >= 2)
        {
            game.Phase = GamePhase.Play;
        }
        
        outcome.ReplyBuilder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>()
            .AppendContentNewline($"Set {await gamePlayer.GetNameAsync(true)}'s VP to {vp} (was {previous})")
            .WithAllowedMentions(gamePlayer);
    }

    [Command("ShuffleTechDeck")]
    [Description("Shuffle the tech deck. Useful if you've just had to inspect or edit it")]
    public static async Task ShuffleTechDeck(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        game.TechDeck.Shuffle();
        
        outcome.SetSimpleReply("Shuffled the tech deck");
    }

    [Command("AddTechToDeck")]
    [Description("Add a tech to the tech deck")]
    public static async Task AddTechToDeck(CommandContext context,
        [SlashAutoCompleteProvider<TechIdChoiceProvider>] string techId,
        bool allowDuplicate = false)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        var techName = Tech.TechsById[techId].DisplayName;
        
        if (!allowDuplicate && (game.TechDeck.Contains(techId) || game.TechDiscards.Contains(techId)))
        {
            outcome.RequiresSave = false;
            await context.RespondAsync($"Failed because {techName} is already in tech deck or discards. Specify allowDuplicate = true if you want to allow this");
            return;
        }
        
        game.TechDeck.Insert(0, techId);
        
        outcome.SetSimpleReply($"Put {techName} on top of the tech deck. You may now want to shuffle the deck (/shuffle_tech_deck)");
    }

    [Command("AddTechToDiscards")]
    [Description("Add a tech to the tech discards")]
    public static async Task AddTechToDiscards(CommandContext context,
        [SlashAutoCompleteProvider<TechIdChoiceProvider>]
        string techId,
        bool allowDuplicate = false)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        var techName = Tech.TechsById[techId].DisplayName;
        
        if (!allowDuplicate && (game.TechDeck.Contains(techId) || game.TechDiscards.Contains(techId)))
        {
            outcome.RequiresSave = false;
            await context.RespondAsync($"Failed because {techName} is already in tech deck or discards. Specify allowDuplicate = true if you want to allow this");
            return;
        }
        
        game.TechDiscards.Insert(0, techId);
        
        outcome.SetSimpleReply($"Put {techName} into the tech discards");
    }

    [Command("CycleTechMarket")]
    [Description("Cycle the tech market as if a tech had been purchased")]
    public static async Task CycleTechMarket(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>();
        await TechOperations.CycleTechMarketAsync(builder, game);
        
        outcome.ReplyBuilder = builder;
    }
}