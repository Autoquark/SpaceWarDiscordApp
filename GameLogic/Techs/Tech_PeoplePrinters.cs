using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.PeoplePrinters;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_PeoplePrinters : Tech, IInteractionHandler<UsePeoplePrintersInteraction>, IInteractionHandler<SpecifyPeoplePrintersAmountInteraction>
{
    public Tech_PeoplePrinters() : base("people-printers",
        "People Printers",
        "When you produce on a planet with $science$ you may convert each $science$ produced into +2 production instead.",
        "If they're coming out floppy, you need to replace the calcium cartridge.")
    {
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_BeginProduce beginProduce)
        {
            var hex = game.GetHexAt(beginProduce.Location);
            if (hex.Planet?.OwningPlayerId == player.GamePlayerId && hex.Planet.Science > 0)
            {
                return
                [
                    new TriggeredEffect
                    {
                        AlwaysAutoResolve = false,
                        DisplayName = $"{DisplayName}: Exchange produced science for forces",
                        IsMandatory = false,
                        ResolveInteractionData = new UsePeoplePrintersInteraction
                        {
                            Game = game.DocumentId,
                            ForGamePlayerId = player.GamePlayerId,
                            EventDocumentId = beginProduce.DocumentId!,
                            Event = beginProduce
                        }
                    }
                ];
            }
        }

        return [];
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, UsePeoplePrintersInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Event.Location);
        
        // No longer valid to use tech, just ignore and continue
        if (hex.Planet == null || hex.Planet.Science == 0 || interactionData.Event.EffectiveScienceProduction == 0)
        {
            await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
            return new SpaceWarInteractionOutcome(true);
        }

        // Only 1 science produced so no need to prompt for amount
        if (interactionData.Event.EffectiveScienceProduction == 1)
        {
            interactionData.Event.EffectiveScienceProduction = 0;
            interactionData.Event.EffectiveProductionValue += 2;

            builder?.AppendContentNewline(GetMessage(1));
            
            await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
            return new SpaceWarInteractionOutcome(true);
        }

        // Prompt for amount
        var interactionIds = serviceProvider.AddInteractionsToSetUp(Enumerable.Range(1, interactionData.Event.EffectiveScienceProduction)
            .Select(x => new SpecifyPeoplePrintersAmountInteraction
            {
                Event = interactionData.Event,
                EventDocumentId = interactionData.Event.DocumentId!,
                ForGamePlayerId = interactionData.ForGamePlayerId,
                Game = interactionData.Game,
                ScienceAmount = x
            }));

        builder?.AppendButtonRows(Enumerable.Range(1, interactionData.Event.EffectiveScienceProduction)
            .Zip(interactionIds).Select((x =>
                new DiscordButtonComponent(DiscordButtonStyle.Primary, x.Second,
                    $"{x.First} science -> {x.First * 2} forces"))));

        return new SpaceWarInteractionOutcome(true);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, SpecifyPeoplePrintersAmountInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        if (interactionData.Event.EffectiveScienceProduction < interactionData.ScienceAmount)
        {
            throw new Exception();
        }
        
        interactionData.Event.EffectiveScienceProduction -= interactionData.ScienceAmount;
        interactionData.Event.EffectiveProductionValue += interactionData.ScienceAmount * 2;
        
        builder?.AppendContentNewline(GetMessage(interactionData.ScienceAmount));
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        return new SpaceWarInteractionOutcome(true);
    }

    private static string GetMessage(int scienceAmount) =>
        $"Used people printers to exchange {scienceAmount} produced $science$ for {scienceAmount * 2} extra forces".ReplaceIconTokens();
}