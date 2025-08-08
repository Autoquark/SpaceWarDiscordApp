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

    // Not currently supported as this would require using a FollowupMessageBuilder to append any new content,
    // and we now need to support passing arbitrary builders into interaction handlers because they can be triggered
    // from within other game logic which already has a builder.
    // Might become possible again when I add support for multi-message 
    public bool DeleteOriginalMessage { get; set; }

    public DiscordMultiMessageBuilder? ReplyBuilder { get; set; }

    public void SetSimpleReply(string content)
    {
        ReplyBuilder = new DiscordMultiMessageBuilder(new DiscordWebhookBuilder(), () => new DiscordFollowupMessageBuilder())
            .AppendContentNewline(content);
    }
}