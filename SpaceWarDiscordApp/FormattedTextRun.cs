namespace SpaceWarDiscordApp;

public class FormattedTextRun
{
    public FormattedTextRun() {}
    
    public FormattedTextRun(string text)
    {
        Text = text;
    }
    
    public bool IsBold { get; set; }
    
    public bool IsItalic { get; set; }
    
    public bool IsStrikethrough { get; set; }
    
    public string Text { get; set; } = "";
}