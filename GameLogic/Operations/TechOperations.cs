using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public class TechOperations : IPlayerChoiceEventHandler<GameEvent_TechPurchaseDecision, PurchaseTechInteraction>,
    IEventResolvedHandler<GameEvent_PlayerGainScience>,
    IEventResolvedHandler<GameEvent_PlayerGainTech>,
    IEventResolvedHandler<GameEvent_PlayerLoseTech>
{
    public static DiscordMultiMessageBuilder ShowTechDetails(DiscordMultiMessageBuilder builder, string techId)
    {
        if (!Tech.TechsById.TryGetValue(techId, out var tech))
        {
            builder.AppendContentNewline("Unknown tech");
            return builder;
        }

        var keywordsText = tech.DescriptionKeywords.Any()
            ? (string.Join(", ", tech.DescriptionKeywords.Select(x => x.ToString().InsertSpacesInCamelCase())) + ": ")
            .DiscordBold()
            : "";
        var text = new StringBuilder(tech.DisplayName.DiscordHeading1())
            .AppendLine()
            .AppendLine(keywordsText + tech.Description.ReplaceIconTokens());
        builder.AddContainerComponent(new DiscordContainerComponent(
            [
                new DiscordTextDisplayComponent(text.ToString()),
                new DiscordTextDisplayComponent(tech.FlavourText.DiscordItalic())
            ]));

        return builder;
    }
    
    public static IDiscordMessageBuilder ShowTechDetails(IDiscordMessageBuilder builder, string techId)
    {
        if (!Tech.TechsById.TryGetValue(techId, out var tech))
        {
            builder.AppendContentNewline("Unknown tech");
            return builder;
        }
        
        var keywordsText = tech.DescriptionKeywords.Any() ? (string.Join(", ", tech.DescriptionKeywords) + ": ").DiscordBold() : "";
        var text = new StringBuilder(tech.DisplayName.DiscordHeading1())
            .AppendLine()
            .AppendLine(keywordsText + tech.Description.ReplaceIconTokens());
        builder.AddContainerComponent(new DiscordContainerComponent(
        [
            new DiscordTextDisplayComponent(text.ToString()),
            new DiscordTextDisplayComponent(tech.FlavourText.DiscordItalic())
        ]));

        return builder;
    }

    public static async Task<DiscordMultiMessageBuilder?> CycleTechMarketAsync(DiscordMultiMessageBuilder? builder, Game game)
    {
        builder?.AppendContentNewline("The tech market has been cycled.");
        var added = TryDrawTechFromDeck(builder, game);
        game.TechMarket.Insert(0, added?.Id);
        
        if (added != null)
        {
            builder?.AppendContentNewline("A new tech has been added to the tech market:");
            builder.OrDefault(x => ShowTechDetails(x, added.Id));
        }
        
        var removed = game.TechMarket.Last();
        game.TechMarket.RemoveAt(game.TechMarket.Count - 1);
        if (removed != null)
        {
            AddTechToDiscards(game, removed);
            var tech = Tech.TechsById[removed];
            builder?.AppendContentNewline($"{tech.DisplayName} has been discarded from the tech market");
        }

        await UpdatePinnedTechMessage(game);
        
        return builder;
    }

    public static async Task UpdatePinnedTechMessage(Game game)
    {
        var gameChannel = await Program.DiscordClient.GetChannelAsync(game.GameChannelId);
        var rootMessage = await gameChannel.TryGetMessageAsync(game.PinnedTechMessageId)
            ?? await gameChannel.SendMessageAsync(x =>
                x.EnableV2Components().AppendContentNewline("This pinned thread will always be updated with descriptions of all the techs currently relevant to this game"));
        
        // A thread started from a message has the same ID as that message
        var thread = gameChannel.Threads.FirstOrDefault(x => x.Id == rootMessage.Id);
        if (thread! == null!)
        {
            thread = await rootMessage.CreateThreadAsync("Game Techs", DiscordAutoArchiveDuration.Week);
        }

        var rootBuilder = new DiscordMessageBuilder().EnableV2Components()
            .AppendContentNewline(
            "This pinned thread will always be updated with descriptions of all the techs currently relevant to this game");
        await rootMessage.ModifyAsync(rootBuilder);

        var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>();
        var allTechs = game.UniversalTechs
            .Concat(game.TechMarket.WhereNonNull())
            .Concat(game.Players.SelectMany(x => x.Techs.Select(y => y.TechId)))
            .Distinct()
            .OrderBy(x => Tech.TechsById[x].DisplayName)
            .ToList();

        foreach (var tech in allTechs)
        {
            ShowTechDetails(builder, tech);
        }

        // Skip first thread message as it has weird behaviour and we can't edit components into it
        // Reverse order because discord returns most recent first
        var threadMessages = await thread.GetMessagesAsync().Reverse().Skip(1).ToListAsync();
        foreach (var (discordMessageBuilder, message) in builder.Builders.Cast<DiscordMessageBuilder>()
                     .ZipLongest(threadMessages))
        {
            // We need fewer messages now, delete this one
            if (discordMessageBuilder == null)
            {
                await message!.DeleteAsync();
                continue;
            }
            
            // There's a corresponding old message we can edit
            if (message! != null!)
            {
                await message.ModifyAsync(discordMessageBuilder);
                continue;
            }
            
            var newMessage = await thread.SendMessageAsync(discordMessageBuilder);
        }
        
        await rootMessage.PinAsync();
        game.PinnedTechMessageId = rootMessage.Id;
    }

    public static Tech? DrawTechFromDeckSilent(Game game) => TryDrawTechFromDeck(null, game);

    public static Tech? TryDrawTechFromDeck(DiscordMultiMessageBuilder? builder, Game game)
    {
        if (game.TechDeck.Count == 0)
        {
            game.TechDeck.AddRange(game.TechDiscards.Shuffled());
            game.TechDiscards.Clear();
            if (game.TechDeck.Count == 0)
            {
                builder?.AppendContentNewline("Can't draw a new tech, all tech cards are already in play");
                return null; // All techs are already in play
            }
            builder?.AppendContentNewline("The tech discards have been shuffled to form a new tech deck");
        }
        
        var tech = game.TechDeck[0];
        game.TechDeck.RemoveAt(0);
        return Tech.TechsById[tech];
    }
    
    public static void AddTechToDiscards(Game game, string techId) => game.TechDiscards.Insert(0, techId);

    public static int GetMarketSlotCost(int slotNumber) => slotNumber == 0 ? 3 : 2;

    public async Task<DiscordMultiMessageBuilder?> ShowPlayerChoicesAsync(DiscordMultiMessageBuilder builder, GameEvent_TechPurchaseDecision gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerByGameId(gameEvent.PlayerGameId);
        var availableUniversal = GetPurchaseableUniversalTechsForPlayer(game, player).ToList();
        
        var availableMarket = game.TechMarket.Select(x => (techId: x, cost: GetMarketSlotCost(game.TechMarket.IndexOf(x))))
            .Where(x => x.techId != null && player.TryGetPlayerTechById(x.techId) == null && player.Science >= x.cost)!
            // Assert that techId is not null, because it's checked above
            .ToList<(string techId, int cost)>();
        
        var name = await player.GetNameAsync(true);
        builder.AppendContentNewline($"{name}, you may purchase a tech:")
            .WithAllowedMentions(player);
        
        var universalIds = serviceProvider.AddInteractionsToSetUp(availableUniversal.Select(x =>
            new PurchaseTechInteraction
            {
                Game = game.DocumentId,
                TechId = x,
                ForGamePlayerId = player.GamePlayerId,
                EditOriginalMessage = true,
                Cost = GameConstants.UniversalTechCost,
                ResolvesChoiceEventId = gameEvent.EventId,
            }));
            
        var marketIds = serviceProvider.AddInteractionsToSetUp(availableMarket 
            .Select(x => new PurchaseTechInteraction
                {
                    Game = game.DocumentId,
                    TechId = x.techId,
                    ForGamePlayerId = player.GamePlayerId,
                    EditOriginalMessage = true,
                    Cost = x.cost,
                    ResolvesChoiceEventId = gameEvent.EventId,
                }));

        var declineId = serviceProvider.AddInteractionToSetUp(new PurchaseTechInteraction
            {
                Game = game.DocumentId,
                TechId = null,
                Cost = 0,
                ForGamePlayerId = player.GamePlayerId,
                EditOriginalMessage = false,
                ResolvesChoiceEventId = gameEvent.EventId
            });

        if (availableUniversal.Count > 0)
        {
            builder.AppendContentNewline("Universal Techs:".DiscordHeading3());
            builder.AppendButtonRows(availableUniversal.Zip(universalIds)
                .Select(x => new DiscordButtonComponent(
                    DiscordButtonStyle.Primary,
                    x.Second,
                    $"{Tech.TechsById[x.First].DisplayName} ({GameConstants.UniversalTechCost})")));
        }

        if (availableMarket.Count > 0)
        {
            builder.AppendContentNewline("Market Techs:".DiscordHeading3());
            builder.AppendButtonRows(availableMarket
                .Zip(marketIds, (x, y) => (x.techId, x.cost, interactionId: y))
                .Select(x => new DiscordButtonComponent(
                    DiscordButtonStyle.Primary,
                    x.interactionId,
                    $"{Tech.TechsById[x.techId].DisplayName} ({x.cost})")));
        }
        
        builder.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Danger, declineId, "Decline"));

        return builder;
    }

    public async Task<bool> HandlePlayerChoiceEventInteractionAsync(DiscordMultiMessageBuilder? builder, GameEvent_TechPurchaseDecision gameEvent,
        PurchaseTechInteraction choice, Game game, IServiceProvider serviceProvider)
    {
        if (choice.TechId != null)
        {
            var player = game.GetGamePlayerByGameId(gameEvent.PlayerGameId);
            var name = await player.GetNameAsync(false);
            var tech = Tech.TechsById[choice.TechId];
            
            var originalScience = player.Science;
            player.Science -= choice.Cost;
            
            builder?.AppendContentNewline($"{name} has purchased {tech.DisplayName} for {choice.Cost} Science ({originalScience} -> {player.Science})");
            
            GameFlowOperations.PushGameEvents(game,
                new GameEvent_PlayerGainTech
                {
                    TechId = choice.TechId,
                    PlayerGameId = gameEvent.PlayerGameId,
                    CycleMarket = true
                });
        }
        
        return true;
    }

    public async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_PlayerGainScience gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        if (gameEvent.Amount > 0)
        {
            var player = game.GetGamePlayerByGameId(gameEvent.PlayerGameId);
            player.Science += gameEvent.Amount;
            builder?.AppendContentNewline($"{await player.GetNameAsync(false)} now has {player.Science} science (was {player.Science - gameEvent.Amount})");
            
            if (GetPurchaseableUniversalTechsForPlayer(game, player).Any() ||
                GetPurchaseableMarketTechsForPlayer(game, player).Any())
            {
                GameFlowOperations.PushGameEvents(game, new GameEvent_TechPurchaseDecision
                {
                    PlayerGameId = gameEvent.PlayerGameId
                });
            }
        }
        
        return builder;
    }
    
    private static IEnumerable<string> GetPurchaseableMarketTechsForPlayer(Game game, GamePlayer player) =>
        game.TechMarket.WhereNonNull()
            .Where(x => player.TryGetPlayerTechById(x) == null)
            .Where(x => player.Science >= GetMarketSlotCost(game.TechMarket.IndexOf(x)))
            .ToList();
    
    private static IEnumerable<string> GetPurchaseableUniversalTechsForPlayer(Game game, GamePlayer player) =>
        player.Science >= GameConstants.UniversalTechCost
            ? game.UniversalTechs.Where(x => player.TryGetPlayerTechById(x) == null).ToList() 
            : [];

    public async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_PlayerGainTech gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerByGameId(gameEvent.PlayerGameId);
        var tech = Tech.TechsById[gameEvent.TechId];
        
        player.Techs.Add(gameEvent.PlayerTech ?? tech.CreatePlayerTech(game, player));
        
        var index = game.TechMarket.IndexOf(tech.Id);
        if (index != -1)
        {
            game.TechMarket[index] = null;
        }

        builder.OrDefault(x => ShowTechDetails(x, tech.Id));

        if (gameEvent.CycleMarket)
        {
            await CycleTechMarketAsync(builder, game);
        }
        
        await UpdatePinnedTechMessage(game);
        
        await GameFlowOperations.AdvanceTurnOrPromptNextActionAsync(builder, game, serviceProvider);
        
        return builder;
    }

    public Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_PlayerLoseTech gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerByGameId(gameEvent.PlayerGameId);
        player.Techs.Remove(player.GetPlayerTechById(gameEvent.TechId));
        return Task.FromResult(builder);
    }
}