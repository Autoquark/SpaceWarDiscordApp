using DSharpPlus.Entities;

namespace SpaceWarDiscordApp.Discord;

public class SpaceWarCommandOutcome
{
    public SpaceWarCommandOutcome()
    {
    }

    public SpaceWarCommandOutcome(bool requiresSave, IDiscordMessageBuilder replyBuilder)
    {
        RequiresSave = requiresSave;
        ReplyBuilder = replyBuilder;
    }

    public bool? RequiresSave { get; set; } = null;

    public IDiscordMessageBuilder? ReplyBuilder { get; set; } = null;

    public void SetSimpleReply(string content)
    {
        ReplyBuilder = new DiscordMessageBuilder().EnableV2Components()
            .AppendContentNewline(content);
    }
}