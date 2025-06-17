using DSharpPlus.Entities;

namespace SpaceWarDiscordApp.Discord;

public class SpaceWarInteractionOutcome
{
    public SpaceWarInteractionOutcome(bool requiresSave, IDiscordMessageBuilder? replyBuilder, bool deleteOriginalMessage = false)
    {
        RequiresSave = requiresSave;
        ReplyBuilder = replyBuilder;
        DeleteOriginalMessage = deleteOriginalMessage;
    }

    public bool RequiresSave { get; set; }

    public bool DeleteOriginalMessage { get; set; }

    public IDiscordMessageBuilder? ReplyBuilder { get; set; }

    public void SetSimpleReply(string content)
    {
        ReplyBuilder = new DiscordWebhookBuilder().EnableV2Components()
            .AppendContentNewline(content);
    }
}