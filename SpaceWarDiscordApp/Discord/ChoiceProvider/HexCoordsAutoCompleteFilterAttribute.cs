using System.Linq.Expressions;
using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord.ChoiceProvider;

[AttributeUsage(AttributeTargets.Parameter)]
public class HexCoordsAutoCompleteFilterAttribute : Attribute
{
    public bool? WithPlanet { get; set; }
}