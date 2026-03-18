using DSharpPlus.Commands.Converters;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Discord.ArgumentConverters;

public class HexCoordinatesArgumentConverter : ISlashArgumentConverter<HexCoordinates>
{
    public Task<Optional<HexCoordinates>> ConvertAsync(ConverterContext context)
    {
        if (context.Argument is string argument)
        {
            if (HexCoordinates.TryParse(argument, out var result))
            {
                return Task.FromResult(Optional.FromValue(result));
            }
            else if(int.TryParse(argument, out var asInt))
            {
                return HexCoordinates.TryFromHexNumber(asInt, out result)
                    ? Task.FromResult(Optional.FromValue(result))
                    : Task.FromResult(Optional.FromNoValue<HexCoordinates>());
            }
        }
            
        return Task.FromResult(Optional.FromNoValue<HexCoordinates>());
    }

    public string ReadableName => "Hex Coordinates";

    public DiscordApplicationCommandOptionType ParameterType => DiscordApplicationCommandOptionType.String;
}