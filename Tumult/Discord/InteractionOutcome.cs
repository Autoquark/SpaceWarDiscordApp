namespace Tumult.Discord;

public class InteractionOutcome
{
    public InteractionOutcome(bool requiresSave)
    {
        RequiresSave = requiresSave;
    }

    public bool RequiresSave { get; set; }

    public bool DeleteOriginalMessage { get; set; }
}
