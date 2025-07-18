using DSharpPlus.Entities;

namespace SpaceWarDiscordApp.Discord;

public class DiscordMultiMessageBuilder : IDisposable, IAsyncDisposable
{
    public DiscordMultiMessageBuilder(IDiscordMessageBuilder initial,
        Func<IDiscordMessageBuilder> followupBuilderFactory)
    {
        initial.EnableV2Components();
        _followupBuilderFactory = () => followupBuilderFactory().EnableV2Components();
        _builders.Add(initial);
    }
    
    public IReadOnlyList<IDiscordMessageBuilder> Builders => _builders;

    private readonly List<IDiscordMessageBuilder> _builders = [];
    
    private readonly Func<IDiscordMessageBuilder> _followupBuilderFactory;
    
    private IDiscordMessageBuilder CurrentBuilder => _builders.Last();
    
    public void Dispose()
    {
        foreach (var builder in Builders)
        {
            builder.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var builder in Builders)
        {
            await builder.DisposeAsync();
        }
    }

    /// <summary>
    /// Causes any following content to appear in a new message, separate to any preceding content.
    /// </summary>
    /// <returns></returns>
    public DiscordMultiMessageBuilder NewMessage()
    {
        if (_builders.Last().Components.Count != 0)
        {
            var builder = _followupBuilderFactory();
            _builders.Add(builder);
        }

        return this;
    }

    public DiscordMultiMessageBuilder AppendContentNewline(string content)
    {
        try
        {
            CurrentBuilder.AddTextDisplayComponent(content);
        }
        catch (InvalidOperationException)
        {
            NewMessage();
            CurrentBuilder.AddTextDisplayComponent(content);
        }

        return this;
    }
}