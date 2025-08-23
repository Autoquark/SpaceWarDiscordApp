using System.Text;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData.Tech;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public static class TechOperations
{
    public static async Task<DiscordMultiMessageBuilder> ShowTechPurchaseButtonsAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var availableUniversal = player.Science >= GameConstants.UniversalTechCost
            ? game.UniversalTechs.Where(x => player.TryGetPlayerTechById(x) == null).ToList() : [];
        
        var availableMarket = game.TechMarket.Select(x => (techId: x, cost: GetMarketSlotCost(game.TechMarket.IndexOf(x))))
            .Where(x => x.techId != null && player.TryGetPlayerTechById(x.techId) == null && player.Science >= x.cost)!
            // Assert that techId is not null, because it's checked above
            .ToList<(string techId, int cost)>();

        if (availableUniversal.Count == 0 && availableMarket.Count == 0)
        {
            return builder;
        }
        
        var name = await player.GetNameAsync(true);
        builder.AppendContentNewline($"{name}, you may purchase a tech:")
            .WithAllowedMentions(player);

        var (universalIds, marketIds, declineId) = await Program.FirestoreDb.RunTransactionAsync(transaction =>
        {
            var interactionGroupId = serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData
                .InteractionGroupId;
            var universalIds = InteractionsHelper.SetUpInteractions(availableUniversal.Select(x => new PurchaseTechInteraction
            {
                Game = game.DocumentId,
                TechId = x,
                ForGamePlayerId = player.GamePlayerId,
                EditOriginalMessage = false,
                Cost = GameConstants.UniversalTechCost
            }),
            transaction, interactionGroupId);
            
            var marketIds = InteractionsHelper.SetUpInteractions(availableMarket
                    .Select(x => new PurchaseTechInteraction
                {
                    Game = game.DocumentId,
                    TechId = x.techId,
                    ForGamePlayerId = player.GamePlayerId,
                    EditOriginalMessage = false,
                    Cost = x.cost
                }),
                transaction, interactionGroupId);

            var declineId = InteractionsHelper.SetUpInteraction(new DeclineTechPurchaseInteraction
            {
                Game = game.DocumentId,
                ForGamePlayerId = player.GamePlayerId,
                EditOriginalMessage = false
            },
            transaction, interactionGroupId);
            
            return (universalIds, marketIds, declineId);
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

        game.IsWaitingForTechPurchaseDecision = true;

        return builder;
    }
    
    public static async Task<DiscordMultiMessageBuilder> PurchaseTechAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        string techId, int cost, IServiceProvider serviceProvider)
    {
        var name = await player.GetNameAsync(false);
        var tech = Tech.TechsById[techId];

        if (player.Science < cost)
        {
            return builder.AppendContentNewline($"{name} does not have enough science to purchase {tech.DisplayName}!");
        }

        var originalScience = player.Science;
        player.Science -= cost;
        player.Techs.Add(tech.CreatePlayerTech(game, player));
        
        var index = game.TechMarket.IndexOf(techId);
        if (index != -1)
        {
            game.TechMarket[index] = null;
        }

        builder.AppendContentNewline($"{name} has purchased {tech.DisplayName} for {cost} Science ({originalScience} -> {player.Science})");
        
        await CycleTechMarketAsync(builder, game);

        game.IsWaitingForTechPurchaseDecision = false;
        
        await GameFlowOperations.AdvanceTurnOrPromptNextActionAsync(builder, game, serviceProvider);
        
        return builder;
    }

    public static DiscordMultiMessageBuilder ShowTechDetails(DiscordMultiMessageBuilder builder, string techId)
    {
        if (!Tech.TechsById.TryGetValue(techId, out var tech))
        {
            builder.AppendContentNewline("Unknown tech");
            return builder;
        }
        
        var text = new StringBuilder(tech.DisplayName.DiscordHeading1())
            .AppendLine()
            .AppendLine(tech.Description.ReplaceIconTokens());
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
        
        var text = new StringBuilder(tech.DisplayName.DiscordHeading1())
            .AppendLine()
            .AppendLine(tech.Description.ReplaceIconTokens());
        builder.AddContainerComponent(new DiscordContainerComponent(
        [
            new DiscordTextDisplayComponent(text.ToString()),
            new DiscordTextDisplayComponent(tech.FlavourText.DiscordItalic())
        ]));

        return builder;
    }

    public static async Task<DiscordMultiMessageBuilder> CycleTechMarketAsync(DiscordMultiMessageBuilder builder, Game game)
    {
        builder.AppendContentNewline("The tech market has been cycled.");
        var added = TryDrawTechFromDeck(builder, game);
        game.TechMarket.Insert(0, added?.Id);
        
        if (added != null)
        {
            builder.AppendContentNewline("A new tech has been added to the tech market:");
            ShowTechDetails(builder, added.Id);
        }
        
        var removed = game.TechMarket.Last();
        game.TechMarket.RemoveAt(game.TechMarket.Count - 1);
        if (removed != null)
        {
            game.TechDiscards.Add(removed);
            var tech = Tech.TechsById[removed];
            builder.AppendContentNewline($"{tech.DisplayName} has been discarded from the tech market");
        }

        await UpdatePinnedTechMessage(game);
        
        return builder;
    }

    public static async Task UpdatePinnedTechMessage(Game game)
    {
        var gameChannel = await Program.DiscordClient.GetChannelAsync(game.GameChannelId);
        var message = game.PinnedTechMessageId == 0
            ? await gameChannel.SendMessageAsync(x => x.EnableV2Components().AppendContentNewline("Watch this space!"))
            : await gameChannel.GetMessageAsync(game.PinnedTechMessageId);

        var builder = new DiscordMessageBuilder().EnableV2Components();
        builder.AppendContentNewline("This pinned message will always be updated with descriptions of all the techs currently relevant to this game");
        var allTechs = game.UniversalTechs
            .Concat(game.TechMarket.WhereNonNull())
            .Concat(game.Players.SelectMany(x => x.Techs.Select(y => y.TechId)))
            .Distinct()
            .ToList();

        foreach (var tech in allTechs)
        {
            ShowTechDetails(builder, tech);
        }
        
        await message.ModifyAsync(builder);
        await message.PinAsync();
        game.PinnedTechMessageId = message.Id;
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

    public static int GetMarketSlotCost(int slotNumber) => GameConstants.MaxMarketTechCost - slotNumber;
}