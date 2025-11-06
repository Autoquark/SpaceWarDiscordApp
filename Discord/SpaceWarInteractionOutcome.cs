using DSharpPlus.Entities;

namespace SpaceWarDiscordApp.Discord;

public class SpaceWarInteractionOutcome
{
    public SpaceWarInteractionOutcome(bool requiresSave, DiscordMultiMessageBuilder? replyBuilder)
    {
        RequiresSave = requiresSave;
        ReplyBuilder = replyBuilder;
    }

    public bool RequiresSave { get; set; }
    
    public bool DeleteOriginalMessage { get; set; }

    public DiscordMultiMessageBuilder? ReplyBuilder { get; set; }

    public void SetSimpleReply(string content)
    {
        ReplyBuilder = new DiscordMultiMessageBuilder(new DiscordWebhookBuilder(), () => new DiscordFollowupMessageBuilder())
            .AppendContentNewline(content);
    }
}