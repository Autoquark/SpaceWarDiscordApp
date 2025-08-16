using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public static class PlayerTechExtensions
{
    public static Tech GetTech(this PlayerTech playerTech) => Tech.TechsById[playerTech.TechId];
}