using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;

namespace SpaceWarDiscordApp.Discord;

public class DiscordMultiMessageBuilder : IDisposable, IAsyncDisposable
{
    public static DiscordMultiMessageBuilder Create<T>() where T : BaseDiscordMessageBuilder<T>, new()
    => new DiscordMultiMessageBuilder(() => new T());
    
    public DiscordMultiMessageBuilder(Func<IDiscordMessageBuilder> followupBuilderFactory)
    {
        _followupBuilderFactory = () => followupBuilderFactory().EnableV2Components();
        _builders.Add(_followupBuilderFactory());
    }
    
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
    
    public DiscordMultiMessageBuilder AddFile(FileStream stream, AddFileOptions fileOptions)
    {
        CurrentBuilder.AddFile(stream, fileOptions);
        return this;
    }
    
    public DiscordMultiMessageBuilder AddFile(string fileName, Stream stream)
    {
        CurrentBuilder.AddFile(fileName, stream);
        return this;
    }

    public DiscordMultiMessageBuilder AddContainerComponent(DiscordContainerComponent component)
    {
        try
        {
            CurrentBuilder.AddContainerComponent(component);
        }
        catch (InvalidOperationException)
        {
            NewMessage();
            CurrentBuilder.AddContainerComponent(component);
        }
        
        return this;
    }

    public DiscordMultiMessageBuilder AddMediaGalleryComponent(params IEnumerable<DiscordMediaGalleryItem> items)
    {
        CurrentBuilder.AddMediaGalleryComponent(items);
        return this;
    }

    public DiscordMultiMessageBuilder WithAllowedMention(IMention mention) => WithAllowedMentions([mention]);

    public DiscordMultiMessageBuilder WithAllowedMentions(params IEnumerable<IMention> allowedMentions)
    {
        CurrentBuilder.AddMentions(allowedMentions);
        return this;
    }

    public DiscordMultiMessageBuilder WithAllowedMentions(IEnumerable<GamePlayer> players)
    {
        CurrentBuilder.AllowMentions(players);
        return this;
    }
    
    public DiscordMultiMessageBuilder WithAllowedMentions(GamePlayer first, params IEnumerable<GamePlayer> players)
    {
        CurrentBuilder.AllowMentions(first, players);
        return this;
    }

    public bool IsEmpty() => _builders.FirstOrDefault()?.Components.Any() != true;

    public DiscordMultiMessageBuilder AddActionRowComponent(params IEnumerable<DiscordButtonComponent> buttons)
    {
        buttons = buttons.ToList();
        try
        {
            CurrentBuilder.AddActionRowComponent(buttons);
        }
        catch (InvalidOperationException)
        {
            NewMessage();
            CurrentBuilder.AddActionRowComponent(buttons);
        }
        
        return this;
    }

    public DiscordMultiMessageBuilder AppendButtonRows(IEnumerable<DiscordButtonComponent> buttons)
    {
        CurrentBuilder.AppendButtonRows(buttons);
        return this;
    }
    
    public DiscordMultiMessageBuilder AppendButtonRows(string cancelId, IEnumerable<DiscordButtonComponent> buttons)
    {
        CurrentBuilder.AppendButtonRows(cancelId, buttons);
        return this;
    }

    public DiscordMultiMessageBuilder AppendHexButtons(Game game, IEnumerable<BoardHex> hexes,
        IEnumerable<string> interactionIds, string? cancelId = null)
    {
        CurrentBuilder.AppendHexButtons(game, hexes, interactionIds, cancelId);
        return this;   
    }

    public async Task<DiscordMultiMessageBuilder> AppendPlayerButtonsAsync(IEnumerable<GamePlayer> players,
        IEnumerable<string> interactionIds, string? cancelId = null)
    {
        await CurrentBuilder.AppendPlayerButtonsAsync(players, interactionIds, cancelId);
        return this;
    }

    public DiscordMultiMessageBuilder AppendCancelButton(string interactionId)
    {
        CurrentBuilder.AppendCancelButton(interactionId);
        return this;
    }
}