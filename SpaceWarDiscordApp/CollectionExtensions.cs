using System.Collections;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp;

internal static class CollectionExtensions
{
    public static T Random<T>(this IReadOnlyList<T> list) => list[Program.Random.Next(list.Count)];
    public static int RandomIndex<T>(this IReadOnlyList<T> list) => Program.Random.Next(list.Count);
    public static T Random<T>(this IReadOnlyList<T> list, Func<T, float> weightSelector)
    {
        var totalWeight = list.Sum(weightSelector);
        var selected = Program.Random.NextDouble() * totalWeight;
        var skipped = 0f;
        return list.SkipWhile(x => (skipped += weightSelector(x)) <= selected).FirstOrDefault() ?? list[^1];
    }
    public static IEnumerable<T> RandomUnique<T>(this IReadOnlyList<T> list, int count) => list.Shuffled().Take(count);
    
    public static IList<T> Shuffled<T>(this IEnumerable<T> list)
    {
        var copy = new List<T>(list);
        copy.Shuffle();
        return copy;
    }
    
    /// <summary>
    /// Randomly reorders a list in place
    /// </summary>
    public static void Shuffle<T>(this IList<T> list)
    {
        /* To shuffle an array a of n elements (indices 0..n-1):
        for i from n−1 downto 1 do
                j ← random integer such that 0 ≤ j ≤ i
                exchange a[j] and a[i]*/
    
        for (int i = list.Count - 1; i > 0; i--)
        {
            var j = Program.Random.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public static IEnumerable<Tech> ToTechsById(this IEnumerable<string> techIds) => techIds.Select(x => Tech.TechsById[x]);
    public static IEnumerable<Tech?> ToTechsByIdNullable(this IEnumerable<string?> techIds) => techIds.Select(x => x == null ? null : Tech.TechsById[x]);
}