using System.Collections.ObjectModel;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.Interactions;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.GameLogic;

public class SpaceWarGameEventDispatcher : GameEventDispatcher<Game>
{
    public SpaceWarGameEventDispatcher(FirestoreDb firestoreDb) : base(firestoreDb)
    {
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsForPlayer(Game game, GameEvent gameEvent, BaseGamePlayer player)
    {
        var player1 = (GamePlayer)player;
        var triggers = player1.Techs.SelectMany(x => Tech.TechsById[x.TechId].GetTriggeredEffects(game, gameEvent, player1))
            .Where(x => !gameEvent.TriggerIdsResolved.Contains(x.TriggerId))
            .ToList();

        foreach (var triggeredEffect in triggers)
        {
            triggeredEffect.ResolveInteractionId = triggeredEffect.ResolveInteractionData!.InteractionId;
        }
        
        return triggers;
    }

    protected override IEnumerable<int> GetPlayerIdsToResolveTriggersFor(Game game)
        => game.PlayersInTurnOrderFrom(game.CurrentTurnPlayer).Select(x => x.GamePlayerId);

    protected override Task<DiscordMultiMessageBuilder?> OnEventStackEmptyAsync(DiscordMultiMessageBuilder? builder, Game game, IServiceProvider serviceProvider)
        => GameFlowOperations.AdvanceTurnOrPromptNextActionAsync(builder, game, serviceProvider);

    protected override async Task ShowTriggeredEffectsChoiceAsync(DiscordMultiMessageBuilder? builder, GameEvent resolvingEvent, Game game, IServiceProvider serviceProvider)
    {
        var player = (GamePlayer)game.GamePlayers.First(x => x.GamePlayerId == resolvingEvent.ResolvingTriggersForPlayerId);
        var name = await player.GetNameAsync(true);
        // TODO: Better messaging, different for if there are any mandatory or not
        var mandatoryCount = resolvingEvent.RemainingTriggersToResolve.Count(x => x.IsMandatory);
        var optionalCount = resolvingEvent.RemainingTriggersToResolve.Count - mandatoryCount;

        if (mandatoryCount == 0)
        {
            builder?.AppendContentNewline(
                $"{name}, you have optional tech effects which you may trigger. Please select one to resolve next or click 'Decline'.");
        }
        else if (mandatoryCount > 1 && optionalCount == 0)
        {
            builder?.AppendContentNewline(
                $"{name}, you may choose the order in which these mandatory tech effects resolve. Please select one to resolve next.");
        }
        else
        {
            builder?.AppendContentNewline(
                $"{name}, you have optional tech effects which you may trigger. There is also at least one mandatory effect which must be resolved before continuing. Please select an effect to resolve next.");
        }

        var interactionGroupId = serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData
            .InteractionGroupId;

        // Store or update interaction data for buttons in DB
        await Program.FirestoreDb.RunTransactionAsync(async transaction =>
        {
            // For triggers whose InteractionData has already been saved to the DB, we need to update the
            // interaction group ID so AI players know they are still among the current available choices
            var idsToUpdate = resolvingEvent.RemainingTriggersToResolve
                .Select(x => x.ResolveInteractionId)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            IReadOnlyList<DocumentSnapshot> toUpdate = new ReadOnlyCollection<DocumentSnapshot>([]);
            if (idsToUpdate.Count > 0)
            {
                toUpdate = (await transaction.GetSnapshotAsync(
                    new Query<InteractionData>(transaction.Database.InteractionData())
                        .WhereIn(x => x.InteractionId, idsToUpdate))).Documents;
            }

            foreach (var trigger in resolvingEvent.RemainingTriggersToResolve
                .Where(x => x.ResolveInteractionData != null && string.IsNullOrEmpty(x.ResolveInteractionId)))
            {
                trigger.ResolveInteractionId =
                    InteractionStatics.SetUpInteraction(trigger.ResolveInteractionData!, transaction,
                        interactionGroupId);
            }

            foreach (var document in toUpdate)
            {
                transaction.Update(document.Reference, nameof(InteractionData.InteractionGroupId), interactionGroupId);
            }
        });

        builder?.AppendButtonRows(resolvingEvent.RemainingTriggersToResolve.Select(x =>
            new DiscordButtonComponent(
                x.IsMandatory ? DiscordButtonStyle.Primary : DiscordButtonStyle.Secondary,
                x.ResolveInteractionId, x.DisplayName)));

        var secondRowButtons = new List<DiscordButtonComponent>();
        // If there are no mandatory triggers left, the player can decline remaining optional triggers
        if (resolvingEvent.RemainingTriggersToResolve.All(x => !x.IsMandatory))
        {
            var interactionId = serviceProvider.AddInteractionToSetUp(new DeclineOptionalTriggersInteraction
            {
                Game = game.DocumentId,
                ForGamePlayerId = resolvingEvent.ResolvingTriggersForPlayerId
            });
            secondRowButtons.Add(new DiscordButtonComponent(DiscordButtonStyle.Danger, interactionId, "Decline Optional Trigger(s)"));
        }

        var showBoardId = serviceProvider.AddInteractionToSetUp(new ShowBoardInteraction
        {
            ForGamePlayerId = -1,
            Game = game.DocumentId
        });

        secondRowButtons.Add(DiscordHelpers.CreateShowBoardButton(showBoardId));
        builder?.AddActionRowComponent(secondRowButtons);
    }
}
