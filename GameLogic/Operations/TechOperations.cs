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
        var availableUniversal = game.UniversalTechs.Where(x => player.TryGetPlayerTechById(x) == null).ToList();
        var availableMarket = game.MarketTechs.Where(x => player.TryGetPlayerTechById(x) == null).ToList();

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
            
            var marketIds = InteractionsHelper.SetUpInteractions(availableMarket.Select(x => new PurchaseTechInteraction
                {
                    Game = game.DocumentId,
                    TechId = x,
                    ForGamePlayerId = player.GamePlayerId,
                    EditOriginalMessage = true,
                    Cost = 2 //TODO
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
            builder.AppendButtonRows(availableMarket.Zip(marketIds).Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, x.Second, Tech.TechsById[x.First].DisplayName)));
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
        
        builder.AppendContentNewline($"{name} purchases {tech.DisplayName} for {cost} Science ({originalScience} -> {player.Science})");
        
        return builder;
    }
    
}