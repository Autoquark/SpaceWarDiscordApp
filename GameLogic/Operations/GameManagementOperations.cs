using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.Extensions.DependencyInjection;
using Raffinert.FuzzySharp.Extensions;
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

        var builder = new DiscordMessageBuilder().EnableV2Components()
            .AppendContentNewline("Game Setup".DiscordHeading1());
        
        // Player count
        var interactionIds = serviceProvider.AddInteractionsToSetUp(CollectionExtensions.Between(2, GameConstants.MaxPlayerCount).Select(x =>
            new SetMaxPlayerCountInteraction
            {
                Game = game.DocumentId,
                ForGamePlayerId = -1,
                MaxPlayerCount = x
            }));

        builder.AppendContentNewline("Max Players".DiscordHeading2())
            .AppendButtonRows(interactionIds.Index().Select(x =>
                new DiscordButtonComponent(
                    game.Rules.MaxPlayers == x.Index + 2 ? DiscordButtonStyle.Success : DiscordButtonStyle.Primary, x.Item, (x.Index + 2).ToString())));
        
        // Scoring rule
        
        builder.AppendContentNewline("Scoring".DiscordHeading2());
        builder.AppendContentNewline("Scoring Rule".DiscordHeading3());
        
        interactionIds = serviceProvider.AddInteractionsToSetUp(Enum.GetValues<ScoringRule>().Select(x => new SetScoringRuleInteraction
        {
            ForGamePlayerId = -1,
            Game = game.DocumentId,
            Value = x,
        }));
        
        builder.AppendButtonRows(
            Enum.GetValues<ScoringRule>()
                .Zip(interactionIds, (enumValue, interactionId) => new DiscordButtonComponent(
                    enumValue == game.Rules.ScoringRule
                        ? DiscordButtonStyle.Success
                        : DiscordButtonStyle.Secondary,
                    interactionId,
                    Enum.GetName(enumValue)!.InsertSpacesInCamelCase())));

        builder.AppendContentNewline(game.Rules.ScoringRule switch
        {
            ScoringRule.Cumulative =>
                "Each player scores points equal to the number of $star$ they control at the end of their turn. First player to reach the victory threshold wins.",
            ScoringRule.MostStars =>
                "Scoring is checked when the player with the scoring token ends their turn. If one player controls more $star$ than any other player, they score 1VP. Whether anyone scored or not, the victory token then passes against turn order. First player to reach the victory threshold wins.",
            _ => "???"
        });
        
        builder.AppendContentNewline("Victory Threshold".DiscordHeading3());

        var values = GameConstants.PossibleVictoryThresholds[game.Rules.ScoringRule];
        interactionIds = serviceProvider.AddInteractionsToSetUp(values.Select(x => new SetVictoryThresholdInteraction
            {
                VictoryThreshold = x,
                Game = game.DocumentId,
                ForGamePlayerId = -1
            }));

        builder.AppendButtonRows(values.Zip(interactionIds)
            .Select(x =>
                new DiscordButtonComponent(
                    game.Rules.VictoryThreshold == x.First ? DiscordButtonStyle.Success : DiscordButtonStyle.Secondary,
                    x.Second, x.First.ToString())));
        
        // Tech
        
        builder.AppendContentNewline("Tech".DiscordHeading2());
        
        // Starting tech rule
        
        builder.AppendContentNewline("Starting Tech".DiscordHeading3());
        
        interactionIds = serviceProvider.AddInteractionsToSetUp(Enum.GetValues<StartingTechRule>().Select(x => new SetStartingTechRuleInteraction
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

        builder.AppendContentNewline(game.Rules.StartingTechRule switch
        {
            StartingTechRule.None => "No starting techs",
            StartingTechRule.IndividualDraft =>
                "Each player is secretly dealt 3 tech cards and chooses one to keep. Each player will be offered different techs. Unchosen techs are discarded.",
            StartingTechRule.OneUniversal =>
                "Each player simultaneously and secretly chooses one universal tech to start with. Multiple players can choose the same tech.",
            _ => "???"
        });
        
        // Single use tech rules
        builder.AppendContentNewline("Tech Settings".DiscordHeading3());

        var interactionId = serviceProvider.AddInteractionToSetUp(new SetSingleUseTechCanBeUniversal
        {
            Game = game.DocumentId,
            ForGamePlayerId = -1,
            Value = !game.Rules.SingleUseTechCanBeUniversal
        });

        builder.AppendButtonRows(new DiscordButtonComponent(
            game.Rules.SingleUseTechCanBeUniversal ? DiscordButtonStyle.Success : DiscordButtonStyle.Secondary,
            interactionId,
            game.Rules.SingleUseTechCanBeUniversal
                ? "Single-use techs can be universals: ON"
                : "Single-use techs can be universals: OFF"));
        
        // Map generator
        builder.AppendContentNewline("Map Generator".DiscordHeading2());

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
        
        builder.AppendContentNewline("Current players: " + string.Join(", ", await Task.WhenAll(game.Players.Select(x => x.GetNameAsync(false)))));
        
        if (message == null)
        {
            game.SetupMessageId = (await channel.SendMessageAsync(builder)).Id;
        }
        else
        {
            await message.ModifyAsync(builder);
        }
    }

    public static async Task SaveRollbackStateAsync(Game game)
    {
        if (game.RollbackStates.Any(x =>
                x.CurrentTurnGamePlayerId == game.CurrentTurnPlayer.GamePlayerId && x.TurnNumber == game.TurnNumber))
        {
            return;
        }

        var deleteOldest = game.RollbackStates.Count >= 3;
        
        var backupRef = await Program.FirestoreDb.RunTransactionAsync(transaction =>
        {
            var backupRef = transaction.Database.GameBackups().Document();
            transaction.Set(backupRef, game);

            if (deleteOldest)
            {
                transaction.Delete(game.RollbackStates[0].GameDocument);
            }
            
            return backupRef;
        });

        if (deleteOldest)
        {
            game.RollbackStates.RemoveAt(0);
        }
        
        game.RollbackStates.Add(new RollbackState
        {
            CurrentTurnGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
            GameDocument = backupRef,
            TurnNumber = game.TurnNumber
        });
    }

    public static async Task<Game> RollBackGameAsync(Game game, RollbackState state, IServiceProvider serviceProvider)
    {
        if (!game.RollbackStates.Contains(state))
        {
            throw new ArgumentException("Given state is not associated with this game!");
        }
        
        var newGame = await Program.FirestoreDb.RunTransactionAsync(async transaction =>
        {
            var backup = (await transaction.GetSnapshotAsync(state.GameDocument))
                .ConvertTo<Game>();
            
            // The available game states in the DB remain what they were, but exclude showing the user anything that's now in the future
            backup.RollbackStates = game.RollbackStates.Where(x => x.TurnNumber <= backup.TurnNumber).ToList();

            backup.DocumentId = game.DocumentId;
            transaction.Set(game.DocumentId, backup);
            return backup;
        });
        
        // Clear old game object from the cache
        ClearGameCache(game.DocumentId!, serviceProvider);

        return newGame;
    }

    public static void ClearGameCache(DocumentReference gameRef, IServiceProvider serviceProvider)
    {
        var cache = serviceProvider.GetRequiredService<GameCache>();
        cache.Clear(gameRef);
    }
}