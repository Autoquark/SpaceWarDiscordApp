using System.Text;
using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic;

public class BackstoryGenerator
{
    private const double FixedNameChance = 0.1f;
    
    private static readonly string[] AgeSynonymAdjective = ["dawn", "end"];
    private static readonly string[] DramaticNth = ["First", "Second", "Third", "Seventh", "Final"];
    private static readonly string[] AgeSynonyms = ["Age", "Epoch", "Era"];
    
    /// <summary>
    /// e.g. has been *thrown into chaos* by...
    /// </summary>
    private static readonly string[] UniverseStateDescription = ["rocked", "thrown into chaos", "cast into anarchy"];
    
    /// <summary>
    /// e.g. ...by the *sudden* appearance of...
    /// </summary>
    private static readonly string[] GenericIncitingEventAdverb = ["sudden", "prophesied", "foretold", "unexpected"];
    
    /// <summary>
    /// e.g. ...the sudden *appearance* of...
    /// </summary>
    private static readonly string[] GenericIncitingEventVerb = [];
    
    /// <summary>
    /// e.g. ...of the *mysterious* planet
    /// </summary>
    private static readonly string[] GenericIncitingEventSubjectAdjective = ["mysterious", "ancient"];
    
    private static readonly string[] ManySynonym = ["countless", "numerous", "many"];
    private static readonly string[] FactionsSynonym = ["factions", "groups", "species", "races", "clans"];
    private static readonly string[] CompeteSynonym = ["vie", "fight", "compete", "struggle"];
    private static readonly string[] SupremacySynonym = ["supremacy", "dominance", "power"];
    private static readonly string[] AftermathSynonym = ["aftermath", "wake", "fallout"];
    private static readonly string[] SeismicSynonym = ["seismic", "tumultuous", "historic", "cataclysmic"];
    
    private static readonly string[] OneQuestion =
    [
        "Who will emerge victorious from the Space War?",
        "Who will claim dominion over the ancient throneworld of Recatol Mex?"
    ];
    
    private static readonly string[] CurrentEventDescription =
    [
        "warfleets are assembled",
        "alliances are made and broken",
        "ancient grudges are reignited",
        "weapons of terrifying power are primed for use",
        "ambitions grow",
        "the galaxy readies itself for what is to come",
        "the fragile peace wears thin"
    ];

    private static readonly string[] SpeciesName =
    [
        "Neptulon",
        "Vorlon",
        "Marlak",
        "Kal'dar",
        "Zotos",
        "Bentusi",
        "Kadaan",
        "Davos",
        "Garlac"
    ];
    
    private static readonly string[] PlanetSuffix =
    [
        "One",
        "Two",
        "Three",
        "Four",
        "Five",
        "Six",
        "Seven",
        "Eight",
        "Nine",
        "Prime",
        "I",
        "II",
        "III",
        "IV",
        "V",
        "Alpha",
        "Beta",
        "Epsilon",
        "Gamma",
        "Theta",
        "Omega",
    ];

    private static readonly string[] DramaticDescriptor =
    [
        "Gold",
        "Twilight",
        "Steel",
        "Iron",
        "Stellar",
        "Shadow"
    ];
    
    private static readonly string[] NameVowels = ["a", "aa", "e", "i", "ii", "o", "u"];
    private static readonly string[] NameConsonants = ["b", "d", "dh", "f", "g", "gh", "j", "k", "kh", "l", "m", "n", "n", "p", "r", "s", "t", "v", "x", "y", "z"];

    private static readonly IncitingEventSubject[] IncitingEventSubjects =
    [
        new()
        {
            Nouns = ["planet", "world", "anomaly", "entity"],
            Adjectives = ["forbidden", "legendary"],
            Adverbs = [],
            Verbs = ["disappearance", "reappearance", "destruction", "appearance", "discovery", "rediscovery"],
            NameGenerator = () =>
            {
                var builder = new StringBuilder();
                builder.Append(SpeciesName.Random());
                if (Program.Random.NextBool())
                {
                    builder.Append(' ');
                    builder.Append(PlanetSuffix.Random());
                }
                
                return builder.ToString();
            }
        },
        new()
        {
            Nouns = ["Empire", "Confederacy", "Potentate", "Dominion", "Imperium"],
            Adjectives = ["mighty", "oppressive", "powerful", "sprawling"],
            Adverbs = ["violent", "bloody"],
            Verbs = ["collapse", "overthrow"],
            NameAdjectiveCombiner = (noun, adjective, name) => $"{adjective} {name} {noun}",
            NameGenerator = () => Program.Random.NextBool() ? SpeciesName.Random() : DramaticDescriptor.Random()
        },
        new()
        {
            Nouns = ["race", "species", "beings"],
            Adjectives = ["reclusive", "powerful", "benevolent", "malevolent", "legendary"],
            Adverbs = [],
            Verbs = ["arrival", "reappearance", "disappearance", "destruction", "discovery", "rediscovery"],
            NameAdjectiveCombiner = (noun, adjective, name) => $"{adjective} {noun} known as the {name}",
            NameGenerator = () => SpeciesName.Random()
        },
        new()
        {
            Nouns = ["artefact", "device", "relic"],
            Adjectives = ["legendary", "powerful"],
            Adverbs = [],
            FixedNames = ["Anathema", "Apocalypse", "Babylon", "Shadow", "Twilight", "Veritas", "Epsilon", "Theta", "Omega"],
            Verbs = ["appearance", "reappearance", "disappearance", "destruction", "discovery", "rediscovery", "theft", "creation"],
            NameAdjectiveCombiner = (noun, adjective, name) => $"{adjective} {noun} known as the {name}",
            // TODO: Silly mode with options: Gadget, doodad, thingy etc.
            NameGenerator = () =>
            {
                var name = new StringBuilder(Program.Random.NextBool() ? SpeciesName.Random() : DramaticDescriptor.Random());
                if (Program.Random.Next(0, 2) == 0)
                {
                    string[] prefixNameComponents = ["Scepter", "Crown", "Jewel", "Orb"];
                    return $"{prefixNameComponents.Random()} of {name}";
                }
                
                string[] suffixComponents = ["Device", "Mechanism", "Apparatus", "Engine", "Generator", "Gate", "Singularity", "Key", "Vortex"];
                return $"{name} {suffixComponents.Random()}";
            }
        }
    ];
    
    class IncitingEventSubject
    {
        public delegate string NameNounAdjectiveCombiner(string noun, string adjective, string name);
        
        public required string[] Nouns { get; init; }
        public string[] FixedNames { get; init; } = [];
        public required string[] Adverbs { get; init; }
        public required string[] Adjectives { get; init; }
        public required string[] Verbs { get; init; }
        
        public NameNounAdjectiveCombiner NameAdjectiveCombiner { get; init; } = (noun, adjective, name) => $"{adjective} {noun} known as {name}";
        public required Func<string> NameGenerator { get; init; }
    }
    
    
    public string GenerateBackstory(Game? game)
    {
        var backstory = new StringBuilder($"It is the {AgeSynonymAdjective.Random()} of the {DramaticNth.Random()} {AgeSynonyms.Random()}. ");

        var incitingEvent = IncitingEventSubjects.Random();

        var adjective = GenericIncitingEventSubjectAdjective.Concat(incitingEvent.Adjectives).ToList().Random();
        var noun = incitingEvent.Nouns.Random();
        
        var name = incitingEvent.FixedNames.Length > 0 && Program.Random.NextBool(FixedNameChance) ? incitingEvent.FixedNames.Random() : incitingEvent.NameGenerator().Capitalise();
        var currentEvents = CurrentEventDescription.RandomUnique(2);
        
        backstory.AppendLine($"The universe has been {UniverseStateDescription.Random()} by the " +
                             // sudden
                             $"{GenericIncitingEventAdverb.Concat(incitingEvent.Adverbs).ToList().Random()} " +
                             // disappearance
                             $"{GenericIncitingEventVerb.Concat(incitingEvent.Verbs).ToList().Random()} " +
                             // of the powerful Kal'Dari Empire
                             $"of the {incitingEvent.NameAdjectiveCombiner(noun, adjective, name)}. " +
                             $"{ManySynonym.Random().Capitalise()} {FactionsSynonym.Random()} {CompeteSynonym.Random()} for {SupremacySynonym.Random()}" +
                             $" in the {AftermathSynonym.Random()} of these {SeismicSynonym.Random()} events. " +
                             $"But as {string.Join(" and ", currentEvents)}, one question " +
                             $"rises above all others: {OneQuestion.Random()}");
        
        
        return backstory.ToString();
    }

    private static string GenerateName()
    {
        var length = Program.Random.Next(1, 2);
        
        var builder = new StringBuilder();
        return builder.AppendJoin('\'', Enumerable.Range(0, length).Select(x => GenerateNameFragment())).Capitalise().ToString();
    }

    private static string GenerateNameFragment()
    {
        //return NameParts.Random();
        var length = Program.Random.Next(4, 6);
        
        var builder = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            builder.Append(i % 2 == 0 ? NameConsonants.Random() : NameVowels.Random());
            if (i != length - 1 && Program.Random.NextBool(0.05f))
            {
                builder.Append('\'');
            }
        }
        
        return builder.ToString();
    }
}