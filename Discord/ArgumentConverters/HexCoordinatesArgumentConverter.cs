using DSharpPlus.Commands.Converters;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Discord.ArgumentConverters;

public class HexCoordinatesArgumentConverter : ISlashArgumentConverter<HexCoordinates>
{
    public Task<Optional<HexCoordinates>> ConvertAsync(ConverterContext context) =>
        context.Argument is string argument ?
            Task.FromResult<Optional<HexCoordinates>>(HexCoordinates.Parse(argument))
            : Task.FromResult(Optional.FromNoValue<HexCoordinates>());

    public string ReadableName => "Hex Coordinates";

    public DiscordApplicationCommandOptionType ParameterType => DiscordApplicationCommandOptionType.String;
}