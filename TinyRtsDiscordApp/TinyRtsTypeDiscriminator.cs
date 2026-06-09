using Tumult.Database.Converters;

namespace TinyRtsDiscordApp;

public class TinyRtsTypeDiscriminator : TypeDiscriminatorBase
{
    protected override IReadOnlyDictionary<string, string> TypeMappings { get; } = new Dictionary<string, string>();
}