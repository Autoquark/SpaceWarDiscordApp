using System.Text;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData.Tech;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public static class TechOperations
{
    public static async Task<TBuilder> ShowTechPurchaseButtonsAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var availableUniversal = player.Science >= GameConstants.UniversalTechCost
            ? game.UniversalTechs.Where(x => player.TryGetPlayerTechById(x) == null).ToList() : [];
        var availableMarket = game.TechMarket.Where((x, i) => x != null && player.TryGetPlayerTechById(x) == null && player.Science >= GetMarketSlotCost(i))
            .ToList();

        if (availableUniversal.Count == 0 && availableMarket.Count == 0)
        {
            return builder;
        }
        
        var name = await player.GetNameAsync(true);
        builder.AppendContentNewline($"{name}, you may purchase a tech:");

        var (universalIds, marketIds, declineId) = await Program.FirestoreDb.RunTransactionAsync(transaction =>
        {
            var universalIds = InteractionsHelper.SetUpInteractions(availableUniversal.Select(x => new PurchaseTechInteraction
            {
                Game = game.DocumentId,
                TechId = x,
                ForGamePlayerId = player.GamePlayerId,
                EditOriginalMessage = true,
                Cost = GameConstants.UniversalTechCost
            }),
            transaction);
            
            var marketIds = InteractionsHelper.SetUpInteractions(availableMarket.Select((techId, i) => (techId, GetMarketSlotCost(i)))
                    .Where(x => x.techId != null)
                    .Select(x => new PurchaseTechInteraction
                {
                    Game = game.DocumentId,
                    TechId = x.techId!,
                    ForGamePlayerId = player.GamePlayerId,
                    EditOriginalMessage = true,
                    Cost = x.Item2
                }),
                transaction);

            var declineId = InteractionsHelper.SetUpInteraction(new DeclineTechPurchaseInteraction
            {
                Game = game.DocumentId,
                ForGamePlayerId = player.GamePlayerId,
                EditOriginalMessage = true
            },
            transaction);
            
            return (universalIds, marketIds, declineId);
        });

        if (availableUniversal.Count > 0)
        {
            builder.AppendContentNewline("Universal Techs:".DiscordHeading3());
            builder.AppendButtonRows(availableUniversal.Zip(universalIds).Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, x.Second, Tech.TechsById[x.First].DisplayName)));
        }

        if (availableMarket.Count > 0)
        {
            builder.AppendContentNewline("Market Techs:".DiscordHeading3());
            builder.AppendButtonRows(availableMarket.Where(x => x != null)
                .Zip(marketIds)
                .Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, x.Second, Tech.TechsById[x.First!].DisplayName)));
        }
        
        builder.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Danger, declineId, "Decline"));

        return builder;
    }
    
    public static async Task<TBuilder> PurchaseTechAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player,
        string techId, int cost)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var name = await player.GetNameAsync(false);
        var tech = Tech.TechsById[techId];

        if (player.Science < cost)
        {
            throw new Exception();
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
        
        CycleTechMarket(builder, game);
        
        return builder;
    }

    public static TBuilder ShowTechDetails<TBuilder>(TBuilder builder, string techId) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        if (!Tech.TechsById.TryGetValue(techId, out var tech))
        {
            builder.AppendContentNewline("Unknown tech");
            return builder;
        }
        
        var text = new StringBuilder(tech.DisplayName.DiscordHeading1())
            .AppendLine()
            .AppendLine(tech.Description);
        builder.AddContainerComponent(new DiscordContainerComponent(
            [
                new DiscordTextDisplayComponent(text.ToString()),
                new DiscordTextDisplayComponent(tech.FlavourText.DiscordItalic())
            ]));

        return builder;
    }

    public static TBuilder CycleTechMarket<TBuilder>(TBuilder builder, Game game)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var addedId = game.DrawTechFromDeck();
        game.TechMarket.Insert(0, addedId);
        var added = Tech.TechsById[addedId];

        builder.AppendContentNewline("A new tech has been added to the tech market:");
        ShowTechDetails(builder, addedId);
        
        var removed = game.TechMarket.Last();
        game.TechMarket.RemoveAt(game.TechMarket.Count - 1);
        if (removed != null)
        {
            var tech = Tech.TechsById[removed];
            builder.AppendContentNewline($"{tech.DisplayName} has been discarded from the tech market");
        }
        
        return builder;
    }

    public static int GetMarketSlotCost(int slotNumber) => GameConstants.MaxMarketTechCost - slotNumber;
}