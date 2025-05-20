using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord.ChoiceProvider;

public class HexCoordsAutoCompleteProvider_WithPlanet : HexCoordsAutoCompleteProvider
{
    protected override IEnumerable<BoardHex> Filter(IEnumerable<BoardHex> boardHexes) =>
        boardHexes.Where(x => x.Planet != null);
}