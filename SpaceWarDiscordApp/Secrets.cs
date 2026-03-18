namespace SpaceWarDiscordApp;

internal class Secrets
{
    public bool IsTestEnvironment { get; set; } = false;
    public string FirestoreProjectId { get; set; } = "";
    public string DiscordToken { get; set; } = "";
    public string OpenRouterApiKey { get; set; } = "";
    public ulong TestGuildId { get; set; } = 0;
    public ulong UserToMessageErrorsTo { get; set; } = 148093858914369536;
}