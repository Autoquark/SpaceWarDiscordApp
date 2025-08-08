using DSharpPlus.Entities;

namespace SpaceWarDiscordApp.Discord;

public class SpaceWarCommandOutcome
{
    public SpaceWarCommandOutcome()
    {
    }

    public SpaceWarCommandOutcome(bool requiresSave, DiscordMultiMessageBuilder replyBuilder)
    {
        RequiresSave = requiresSave;
        ReplyBuilder = replyBuilder;
    }

    public bool? RequiresSave { get; set; } = null;

    public DiscordMultiMessageBuilder ReplyBuilder { get; set; }

    public void SetSimpleReply(string content)
    {
        ReplyBuilder = new DiscordMultiMessageBuilder(new DiscordWebhookBuilder(), () => new DiscordFollowupMessageBuilder())
            .AppendContentNewline(content);
    }
}