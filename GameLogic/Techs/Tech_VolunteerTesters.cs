using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech.VolunteerTesters;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_VolunteerTesters : Tech, IInteractionHandler<SetVolunteerTestersTargetInteraction>,
    IInteractionHandler<UseVolunteerTestersInteraction>
{
    public Tech_VolunteerTesters() : base("volunteerTesters",
        "'Volunteer' Testers",
        "Free action, Exhaust: Destroy any number of your forces on a planet, gain 1 $science$ per forces destroyed.",
        "Hold the experimental device such that the aperture is pointed directly towards your forehead and press the red button on the top. Now, anybody who is still alive, please raise a hand. No? Excellent.")
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
    {
        var targets = game.Hexes.WhereOwnedBy(player).ToList();
        var name = await player.GetNameAsync(true);
        
        builder.AppendContentNewline($"{name}, choose a planet to target:");
        
        var interactions = await InteractionsHelper.SetUpInteractionsAsync(targets.Select(x => new SetVolunteerTestersTargetInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId,
            Target = x.Coordinates
        }));
        
        return builder.AppendHexButtons(game, targets, interactions);
    }

    public async Task HandleInteractionAsync(SetVolunteerTestersTargetInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        var hex = game.GetHexAt(interactionData.Target);
        var interactions = await InteractionsHelper.SetUpInteractionsAsync(Enumerable.Range(1, hex.Planet!.ForcesPresent)
            .Select(x => new UseVolunteerTestersInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Target = interactionData.Target,
                Amount = x
            }));

        var name = await player.GetNameAsync(true);
        var builder = new DiscordWebhookBuilder().EnableV2Components()
            .AppendContentNewline($"{name}, choose how many forces will 'volunteer'")
            .AppendButtonRows(Enumerable.Range(1, hex.Planet.ForcesPresent)
                .Zip(interactions)
                .Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, x.Second, x.First.ToString())));

        await args.Interaction.EditOriginalResponseAsync(builder);
    }

    public async Task HandleInteractionAsync(UseVolunteerTestersInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var hex = game.GetHexAt(interactionData.Target);

        hex.Planet!.SubtractForces(interactionData.Amount);
        var player = game.GetGamePlayerForInteraction(interactionData);
        player.Science += interactionData.Amount;
        
        var name = await player.GetNameAsync(false);

        var builder = new DiscordWebhookBuilder().EnableV2Components()
            .AppendContentNewline($"{interactionData.Amount} of {name}'s forces on {interactionData.Target} have been converted into pure Science")
            .AppendContentNewline($"{name} now has {player.Science} science (was {player.Science - interactionData.Amount})");
        
        var tech = player.GetPlayerTechById(Id);
        tech.IsExhausted = true;

        await TechOperations.ShowTechPurchaseButtonsAsync(builder, game, player);
        
        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Free);
        
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
        
        await args.Interaction.EditOriginalResponseAsync(builder);
    }
}