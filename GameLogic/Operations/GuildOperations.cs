using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public static class GuildOperations
{
    private const string TechListingChannelName = "spacewar-techs";
    
    public async static Task UpdateServerTechListingAsync(DiscordGuild guild)
    {
        var guildData = await Program.FirestoreDb.RunTransactionAsync(async transaction =>
            await transaction.GetOrCreateGuildDataAsync(guild.Id));

        DiscordChannel? channel = null;
        if (guildData.TechListingChannelId != 0)
        {
            try
            {
                channel = await guild.GetChannelAsync(guildData.TechListingChannelId);
            }
            catch (NotFoundException)
            {
                guildData.TechListingMessageIds = [];
            }
        }
        
        if(channel! == null!)
        {
            channel = await guild.CreateChannelAsync(TechListingChannelName,
                DiscordChannelType.Text,
                overwrites:
                [
                    new DiscordOverwriteBuilder(guild.EveryoneRole).Deny(DiscordPermission.SendMessages),
                    new DiscordOverwriteBuilder(guild.CurrentMember).Allow(DiscordPermission.SendMessages)
                ]);
        }

        var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>();
        builder.AppendContentNewline("This channel will be kept updated with details of all techs in SpaceWar:");
        foreach (var techId in Tech.TechsById.Keys.Order())
        {
            TechOperations.ShowTechDetails(builder, techId);
        }

        var messages = await guildData.TechListingMessageIds.ToAsyncEnumerable()
            .SelectAwait(async x => await channel.TryGetMessageAsync(x))
            .WhereNonNull()
            .ToListAsync();
        
        foreach (var (discordMessageBuilder, message) in builder.Builders.Cast<DiscordMessageBuilder>()
                     .ZipLongest(messages))
        {
            // We need fewer messages now, delete this one
            if (discordMessageBuilder == null)
            {
                await message!.DeleteAsync();
                guildData.TechListingMessageIds.Remove(message.Id);
                continue;
            }
            
            // There's a corresponding old message we can edit
            if (message! != null!)
            {
                await message.ModifyAsync(discordMessageBuilder);
                continue;
            }
            
            var newMessage = await channel.SendMessageAsync(discordMessageBuilder);
            guildData.TechListingMessageIds.Add(newMessage.Id);
        }
        
        guildData.TechListingChannelId = channel.Id;
        
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(guildData));
    }
}