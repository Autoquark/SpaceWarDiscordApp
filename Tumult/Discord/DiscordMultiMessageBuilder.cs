using DSharpPlus.Entities;

namespace Tumult.Discord;

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
        DoWithNewMessageIfNecessary(x => x.AddTextDisplayComponent(content));
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
        DoWithNewMessageIfNecessary(x => x.AddContainerComponent(component));
        return this;
    }

    public DiscordMultiMessageBuilder AddMediaGalleryComponent(params IEnumerable<DiscordMediaGalleryItem> items)
    {
        CurrentBuilder.AddMediaGalleryComponent(items);
        return this;
    }

    public DiscordMultiMessageBuilder WithAllowedMention(IMention mention) => WithAllowedMentions(mention);

    public DiscordMultiMessageBuilder WithAllowedMentions(params IEnumerable<IMention> allowedMentions)
    {
        CurrentBuilder.AddMentions(allowedMentions);
        return this;
    }

    public bool IsEmpty() => _builders.FirstOrDefault()?.Components.Any() != true;

    public DiscordMultiMessageBuilder AddActionRowComponent(params IEnumerable<DiscordButtonComponent> buttons)
    {
        buttons = buttons.ToList();
        DoWithNewMessageIfNecessary(x => x.AddActionRowComponent(buttons));
        return this;
    }

    public DiscordMultiMessageBuilder AppendButtonRows(params IEnumerable<DiscordButtonComponent> buttons)
    {
        AppendButtonRows(null, buttons);
        return this;
    }

    public DiscordMultiMessageBuilder AppendButtonRows(string? cancelId, IEnumerable<DiscordButtonComponent> buttons)
    {
        var allButtons = buttons.ToList();
        if (cancelId != null)
        {
            allButtons.Add(new DiscordButtonComponent(DiscordButtonStyle.Danger, cancelId, "Cancel"));
        }

        foreach (var group in allButtons.Index().GroupBy(x => x.Index / 5))
        {
            var groupList = group.Select(x => x.Item).ToList();
            DoWithNewMessageIfNecessary(builder => builder.AddActionRowComponent(groupList));
        }

        return this;
    }

    public DiscordMultiMessageBuilder AppendCancelButton(string interactionId)
    {
        CurrentBuilder.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Danger, interactionId, "Cancel"));
        return this;
    }

    public DiscordMultiMessageBuilder? DoWithNewMessageIfNecessary(Action<IDiscordMessageBuilder> action)
    {
        try
        {
            action(CurrentBuilder);
        }
        catch (InvalidOperationException)
        {
            NewMessage();
            action(CurrentBuilder);
        }

        return this;
    }
}
