using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Setup;
using SpaceWarDiscordApp.Database.GameEvents.Tech;
using SpaceWarDiscordApp.Database.InteractionData.Tech;
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
public class FixupCommands : MovementFlowHandler<FixupCommands>
{
    public FixupCommands() : base(null)
    {
        ActionType = null;
        RequireAdjacency = false;
    }
    
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
        [SlashAutoCompleteProvider<GamePlayerIdAutoCompleteProvider>] int player = -1)
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

        var previous = hex.Planet.ForcesPresent;
        var previousOwner = game.TryGetGamePlayerByGameId(hex.Planet.OwningPlayerId);
        hex.Planet.SetForces(newAmount, newOwner?.GamePlayerId ?? GamePlayer.GamePlayerIdNone);
        
        var previousString = previous == 0 ? "(was empty)" : $"(was {previousOwner!.PlayerColourInfo.GetDieEmojiOrNumber(previous)})";

        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline(newOwner != null
                ? $"Set forces at {coordinates} to {newOwner.PlayerColourInfo.GetDieEmojiOrNumber(hex.ForcesPresent)} {previousString}"
                : $"Removed all forces from {coordinates} {previousString}");
    }
    
    [Command("grantTech")]
    [Description("Grant a tech to a player")]
    public static async Task GrantTech(CommandContext context,
        [SlashAutoCompleteProvider<TechIdAutoCompleteProvider>] string techId,
        [SlashAutoCompleteProvider<GamePlayerIdAutoCompleteProvider>] int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        var gamePlayer = player == -1 ? game.TryGetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        var builders = context.ServiceProvider.GetRequiredService<GameMessageBuilders>();
        
        if (gamePlayer == null)
        {
            outcome.RequiresSave = false;
            builders.SourceChannelBuilder.AppendContentNewline("Unknown player");
            return;
        }

        if (gamePlayer.Techs.Any(x => x.TechId == techId))
        {
            outcome.RequiresSave = false;
            builders.SourceChannelBuilder.AppendContentNewline("Player already has that tech");
            return;
        }

        if (!Tech.TechsById.TryGetValue(techId, out var tech))
        {
            outcome.RequiresSave = false;
            builders.SourceChannelBuilder.AppendContentNewline("Unknown tech");
            return;
        }
        
        gamePlayer.Techs.Add(tech.CreatePlayerTech(game, gamePlayer));

        await TechOperations.UpdatePinnedTechMessage(game);
        
        builders.GameChannelBuilder!
            .AppendContentNewline($"Granted {tech.DisplayName} to {await gamePlayer.GetNameAsync(true)}")
            .WithAllowedMentions(gamePlayer);
    }

    /// <summary>
    /// Removes a tech from a player.
    /// </summary>
    [Command("removeTech")]
    public static async Task RemoveTech(CommandContext context,
        [SlashAutoCompleteProvider<TechIdAutoCompleteProvider>] string techId,
        [SlashAutoCompleteProvider<GamePlayerIdAutoCompleteProvider>] int player = -1)
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

        var index = gamePlayer.Techs.Index().FirstOrDefault(x => x.Item.TechId == techId, (-1, null!)).Index;
        if (index == -1)
        {
            outcome.RequiresSave = false;
            await context.RespondAsync("Player does not have that tech");
            return;
        }
        gamePlayer.Techs.RemoveAt(index);
        
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline($"Removed {tech.DisplayName} from {await gamePlayer.GetNameAsync(true)}")
            .WithAllowedMentions(gamePlayer);
    }

    [Command("AddUniversalTech")]
    [Description("Adds a universal tech.")]
    public static async Task AddUniversalTech(CommandContext context,
        [SlashAutoCompleteProvider<TechIdAutoCompleteProvider>]
        string techId,
        bool removeFromOtherPlaces = true)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        var techName = Tech.TechsById[techId].DisplayName;
        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!;
        
        if(game.UniversalTechs.Count >= GameConstants.UniversalTechCount)
        {
            builder.AppendContentNewline("This game already has the maximum number of universal techs");
            outcome.RequiresSave = false;
            return;
        }

        if (game.UniversalTechs.Contains(techId))
        {
            builder.AppendContentNewline($"{techName} is already a universal tech");
            outcome.RequiresSave = false;
            return;
        }
        
        game.UniversalTechs.Add(techId);
        
        builder.AppendContentNewline($"Added {techName} to universal techs");
        
        if (removeFromOtherPlaces)
        {
            if (game.TechMarket.RemoveAll(x => x == techId) > 0)
            {
                builder.AppendContentNewline($"Removed {techName} from the tech market");
            }
            
            if(game.TechDeck.RemoveAll(x => x == techId) > 0)
            {
                builder.AppendContentNewline($"Removed {techName} from the tech deck");
            }
            
            if(game.TechDiscards.RemoveAll(x => x == techId) > 0)
            {
                builder.AppendContentNewline($"Removed {techName} from the tech discards");
            }
        }
    }

    [Command("ShowTechPurchase")]
    [Description("Prompts for the given player to purchase a tech")]
    public static async Task ShowTechPurchase(CommandContext context,
        [SlashAutoCompleteProvider<GamePlayerIdAutoCompleteProvider>] int player = -1)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        var builders = context.ServiceProvider.GetRequiredService<GameMessageBuilders>();
        
        var gamePlayer = player == -1 ? game.TryGetGamePlayerByDiscordId(context.User.Id) : game.TryGetGamePlayerByGameId(player);
        if (gamePlayer == null)
        {
            builders.SourceChannelBuilder.AppendContentNewline("Unknown player");
            outcome.RequiresSave = false;
            return;
        }

        if (gamePlayer.Science < GameConstants.UniversalTechCost)
        {
            builders.SourceChannelBuilder.AppendContentNewline($"{await gamePlayer.GetNameAsync(false)} cannot afford any techs right now");
            outcome.RequiresSave = false;
            return;
        }

        await GameFlowOperations.PushGameEventsAndResolveAsync(builders.GameChannelBuilder, game,
            context.ServiceProvider, new GameEvent_TechPurchaseDecision
            {
                PlayerGameId = gamePlayer.GamePlayerId,
            });
    }

    [Command("DiscardUniversalTech")]
    [Description("Puts a universal tech into the tech discards.")]
    public static async Task DiscardUniversalTech(CommandContext context,
        [SlashAutoCompleteProvider<UniversalTechAutoCompleteProvider>] string techId)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        var techName = Tech.TechsById[techId].DisplayName;
        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!;
        
        if (game.UniversalTechs.Remove(techId))
        {
            TechOperations.AddTechToDiscards(game, techId);
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
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline($"Updated pinned tech message");
    }

    [Command("discardMarketTech")]
    [Description("Discards a tech from the tech market, leaving an empty slot")]
    public static async Task DiscardMarketTech(CommandContext context,
        [SlashAutoCompleteProvider<MarketTechIdAutoCompleteProvider>]
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
            context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder
                .AppendContentNewline("That tech is not in the market");
            return;
        }
        
        var tech = Tech.TechsById[techId];
        game.TechMarket[index] = null;

        if (putInDiscard)
        {
            TechOperations.AddTechToDiscards(game, techId);
        }
        
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline(putInDiscard 
                ? $"Put {tech.DisplayName} from the tech market into the discard pile"
                : $"Removed {tech.DisplayName} from the tech market (without putting it in the discard pile)");
    }
    
    [Command("setTechExhausted")]
    [Description("Exhausts or unexhausts a player's tech")]
    public static async Task SetTechExhausted(CommandContext context,
        [SlashAutoCompleteProvider<TechIdAutoCompleteProvider>]
        string techId,
        [SlashAutoCompleteProvider<GamePlayerIdAutoCompleteProvider>]
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
        
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline($"{(exhausted ? "Exhausted" : "Unexhausted")} {tech.DisplayName} for {await gamePlayer.GetNameAsync(true)}")
            .WithAllowedMentions(gamePlayer);;
    }

    [Command("regressTechMarket")]
    [Description("Undo cycling the tech market")]
    public static async Task RegressTechMarket(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        if (game.TechMarket.Count == 0)
        {
            context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder
                .AppendContentNewline("This game appears to have no tech market (0 slots)");
            outcome.RequiresSave = false;
            return;
        }

        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!;

        // Put first card in market back on the tech deck
        var first = game.TechMarket[0];
        if (first != null)
        {
            game.TechDeck.Insert(0, first);
            builder.AppendContentNewline($"Returned {Tech.TechsById[first].DisplayName} to the top of the tech deck");
        }
        
        for(var i = 0; i < game.TechMarket.Count - 1; i++)
        {
            game.TechMarket[i] = game.TechMarket[i + 1];
        }
        
        // Put top card in tech discards back into the last market slot
        var topDiscard = game.TechDiscards.DefaultIfEmpty().First();
        if (topDiscard != null)
        {
            game.TechDiscards.RemoveAt(0);
            game.TechMarket[^1] = topDiscard;
            builder.AppendContentNewline($"Returned {Tech.TechsById[topDiscard].DisplayName} from the tech discards to the market");
        }
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
        
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline($"{(exhausted ? "Exhausted" : "Unexhausted")} planet at {coordinates}");
    }

    [Command("SetPlayerTurn")]
    [Description("Set which player's turn it is")]
    public static async Task SetCurrentTurn(CommandContext context,
        [SlashAutoCompleteProvider<GamePlayerIdAutoCompleteProvider>]
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

        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!;
        var previousPlayer = game.CurrentTurnPlayer;
        
        game.CurrentTurnPlayerIndex = game.Players.FindIndex(x => x.GamePlayerId == gamePlayer.GamePlayerId);
        game.ActionTakenThisTurn = false;
        game.AnyActionTakenThisTurn = false;
        
        builder.AppendContentNewline($"Set current turn to {await gamePlayer.GetNameAsync(true)} (was {await previousPlayer.GetNameAsync(true)})")
            .WithAllowedMentions(gamePlayer, previousPlayer);
        await GameFlowOperations.ShowSelectActionMessageAsync(builder, game, context.ServiceProvider);
    }

    [Command("SetScoringTokenPlayer")]
    [Description("Set which player holds the scoring token")]
    public static async Task SetScoringTokenPlayer(CommandContext context,
        [SlashAutoCompleteProvider<GamePlayerIdAutoCompleteProvider>]
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

        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!;
        var previousPlayer = game.ScoringTokenPlayer;
        
        game.ScoringTokenPlayerIndex = game.Players.FindIndex(x => x.GamePlayerId == gamePlayer.GamePlayerId);
        
        builder.AppendContentNewline($"Moved scoring token to {await gamePlayer.GetNameAsync(true)} (was {await previousPlayer.GetNameAsync(true)})")
            .WithAllowedMentions(gamePlayer, previousPlayer);
    }
    
    [Command("SetPlayerScience")]
    [Description("Set a player's science total")]
    public static async Task SetPlayerScience(CommandContext context,
        int science,
        [SlashAutoCompleteProvider<GamePlayerIdAutoCompleteProvider>] int player = -1)
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
        
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline(
                $"Set {await gamePlayer.GetNameAsync(true)}'s science to {science} (was {previous})")
            .WithAllowedMentions(gamePlayer);
        
        game.ScoringTokenPlayerIndex = game.Players.FindIndex(x => x.GamePlayerId == gamePlayer.GamePlayerId);
    }
    
    [Command("SetPlayerVp")]
    [Description("Set a player's Victory Points")]
    public static async Task SetPlayerVictoryPoints(CommandContext context,
        int vp,
        [SlashAutoCompleteProvider<GamePlayerIdAutoCompleteProvider>]
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
        if (game.Players.All(x => x.VictoryPoints < game.Rules.VictoryThreshold) &&
            game.Players.Count(x => !x.IsEliminated) >= 2)
        {
            game.Phase = GamePhase.Play;
        }
        
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline($"Set {await gamePlayer.GetNameAsync(true)}'s VP to {vp} (was {previous})")
            .WithAllowedMentions(gamePlayer);
    }

    [Command("ShuffleTechDeck")]
    [Description("Shuffle the tech deck. Useful if you've just had to inspect or edit it")]
    public static async Task ShuffleTechDeck(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        game.TechDeck.Shuffle();
        
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline("Shuffled the tech deck");
    }

    [Command("AddTechToDeck")]
    [Description("Add a tech to the tech deck")]
    public static async Task AddTechToDeck(CommandContext context,
        [SlashAutoCompleteProvider<TechIdAutoCompleteProvider>] string techId,
        bool allowDuplicate = false)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        var techName = Tech.TechsById[techId].DisplayName;
        
        if (!allowDuplicate && (game.TechDeck.Contains(techId) || game.TechDiscards.Contains(techId)))
        {
            outcome.RequiresSave = false;
            context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder
                .AppendContentNewline($"Failed because {techName} is already in tech deck or discards. Specify allowDuplicate = true if you want to allow this");
            return;
        }
        
        game.TechDeck.Insert(0, techId);
        
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline($"Put {techName} on top of the tech deck. You may now want to shuffle the deck (/shuffle_tech_deck)");
    }

    [Command("AddTechToDiscards")]
    [Description("Add a tech to the tech discards")]
    public static async Task AddTechToDiscards(CommandContext context,
        [SlashAutoCompleteProvider<TechIdAutoCompleteProvider>]
        string techId,
        bool allowDuplicate = false)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        var techName = Tech.TechsById[techId].DisplayName;
        
        if (!allowDuplicate && (game.TechDeck.Contains(techId) || game.TechDiscards.Contains(techId)))
        {
            outcome.RequiresSave = false;
            context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder
                .AppendContentNewline($"Failed because {techName} is already in tech deck or discards. Specify allowDuplicate = true if you want to allow this");
            return;
        }
        
        TechOperations.AddTechToDiscards(game, techId);
        
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline($"Put {techName} into the tech discards");
    }

    [Command("CycleTechMarket")]
    [Description("Cycle the tech market as if a tech had been purchased")]
    public static async Task CycleTechMarket(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;

        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!;
        await TechOperations.CycleTechMarketAsync(builder, game);
    }

    [Command("MoveForces")]
    [Description("Move forces between planets")]
    public async Task MoveForces(CommandContext context,
        [SlashAutoCompleteProvider<GamePlayerIdAutoCompleteProvider>] int player = -1,
        [SlashAutoCompleteProvider<HexCoordsAutoCompleteProvider_WithPlanet>] HexCoordinates? from = null,
        [SlashAutoCompleteProvider<HexCoordsAutoCompleteProvider_WithPlanet>] HexCoordinates? to = null,
        int? amount = null)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!;

        var gamePlayer = game.TryGetGamePlayerByGameId(player) ?? game.TryGetGamePlayerByDiscordId(context.User.Id);
        if (from.HasValue)
        {
            var fromHex = game.TryGetHexAt(from.Value);
            if (fromHex == null)
            {
                builder.AppendContentNewline($"Invalid 'from' coordinates {from.Value}");
                outcome.RequiresSave = false;
                return;
            }

            if (fromHex.Planet == null)
            {
                builder.AppendContentNewline($"Can't move from {from.Value}, no planet");
                outcome.RequiresSave = false;
                return;
            }

            if (fromHex.ForcesPresent == 0)
            {
                builder.AppendContentNewline($"Can't move from {from.Value}, no forces present");
                outcome.RequiresSave = false;
                return;
            }

            gamePlayer = game.GetGamePlayerByGameId(fromHex.Planet.OwningPlayerId);
        }

        if (gamePlayer == null)
        {
            builder.AppendContentNewline("Must specify either the movement source or player to move forces for");
            outcome.RequiresSave = false;
            return;
        }
        
        await BeginPlanningMoveAsync(builder, game, gamePlayer, context.ServiceProvider, from, to, amount, amount);
        
        outcome.RequiresSave = true;
    }

    [Command("Produce")]
    [Description("Produce on a planet")]
    public async Task Produce(CommandContext context,
        [SlashAutoCompleteProvider<HexCoordsAutoCompleteProvider_WithPlanet>] HexCoordinates location)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!;
        
        var hex = game.TryGetHexAt(location);
        if (hex == null)
        {
            builder.AppendContentNewline($"Invalid coordinates {location}");
            outcome.RequiresSave = false;
            return;
        }

        if (hex.Planet == null)
        {
            builder.AppendContentNewline($"Can't produce at {location}, no planet");
            outcome.RequiresSave = false;
            return;
        }

        if (hex.IsNeutral)
        {
            builder.AppendContentNewline($"Can't produce at {location}, planet is unowned");
            outcome.RequiresSave = false;
            return;
        }

        if (hex.Planet.IsExhausted)
        {
            builder.AppendContentNewline("Note: Planet is already exhausted");
        }

        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, context.ServiceProvider,
            ProduceOperations.CreateProduceEvent(game, hex.Coordinates, true));
        
        outcome.RequiresSave = true;
    }

    [Command("RefreshSettingsMessage")]
    public async Task RefreshSettingsMessage(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        
        await GameManagementOperations.CreateOrUpdateGameSettingsMessageAsync(game, context.ServiceProvider);
        
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!
            .AppendContentNewline($"Updated pinned tech message");
        
        outcome.RequiresSave = false;
    }

    [Command("SetPlayerStartingTech")]
    public async Task SetStartingTech(CommandContext context,
        [SlashAutoCompleteProvider<GamePlayerIdAutoCompleteProvider>]
        int player,
        [SlashAutoCompleteProvider<TechIdAutoCompleteProvider>]
        string techId)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!;

        var topEvent = game.EventStack.LastOrDefault();

        if (topEvent is GameEvent_PlayersChooseStartingTech)
        {
            await InteractionDispatcher.HandleInteractionAsync(builder, new ChoosePlayerStartingTechInteraction
            {
                TechId = techId,
                ForGamePlayerId = player,
                Game = game.DocumentId,
                ResolvesChoiceEventId = topEvent.EventId
            }, game, context.ServiceProvider);
        }
        else
        {
            builder.AppendContentNewline("There is not a tech selection event on top of the stack");
        }
        
        outcome.RequiresSave = true;
    }

    [Command("ShowEventStack")]
    public async Task ShowEventStack(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder;
        
        builder.AppendContentNewline("Event stack (top to bottom):".DiscordHeading3());
        foreach (var gameEvent in Enumerable.Reverse(game.EventStack))
        {
            builder.AppendContentNewline(gameEvent.ToString());
        }
    }
}