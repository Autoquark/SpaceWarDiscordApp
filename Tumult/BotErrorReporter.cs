using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.Entities;
using Tumult.Database;
using Tumult.Discord;

namespace Tumult;

public class BotErrorReporter
{
    private readonly bool _isTestEnvironment;
    private readonly DiscordClient _client;
    private readonly ulong _userToMessageErrorsToId;
    private DiscordUser? _userToMessageErrorsTo;

    public BotErrorReporter(bool isTestEnvironment, DiscordClient client, ulong userToMessageErrorsTo)
    {
        _isTestEnvironment = isTestEnvironment;
        _client = client;
        _userToMessageErrorsToId = userToMessageErrorsTo;
    }

    public async Task InitializeAsync()
    {
        _userToMessageErrorsTo = _client.TryGetUserAsync(_userToMessageErrorsToId).GetAwaiter().GetResult();
    }
    
    public async Task LogExceptionAsync(BaseGame? game, Exception exception)
    {
        Console.WriteLine(exception);
        var aggregateException = exception as AggregateException;
        if (aggregateException != null)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                Console.WriteLine(innerException);
            }
        }

        Debugger.Break();

        if (!_isTestEnvironment && _userToMessageErrorsTo != null)
        {
            var builder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>();
            if (game != null)
            {
                var gameChannel = await _client.TryGetChannelAsync(game.GameChannelId);
                builder.AppendContentNewline($"ERROR relating to game {gameChannel?.Mention ?? game.Name}:");
            }
            else
            {
                builder.AppendContentNewline("ERROR without a related game:");
            }
            
            foreach (var section in exception.ToString().Chunk(1900).Select(x => new string(x)))
            {
                builder.AppendContentNewline($"```\n{section}\n```");
            }
            
            if (aggregateException != null)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    foreach (var section in innerException.ToString().Chunk(1900).Select(x => new string(x)))
                    {
                        builder.AppendContentNewline($"```\n{section}\n```");
                    }
                }
            }

            foreach (var discordMessageBuilder in builder.Builders.Cast<DiscordMessageBuilder>())
            {
                await _userToMessageErrorsTo.SendMessageAsync(discordMessageBuilder);
            }
            
        }
    }
}