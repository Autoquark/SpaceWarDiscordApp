using System.Collections;

namespace SpaceWarDiscordApp
{
    internal static class CollectionExtensions
    {
        public static T Random<T>(this IReadOnlyList<T> list) => list[Program.Random.Next(list.Count)];
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

        public static IEnumerable<int> Indices(this ICollection collection) => Enumerable.Range(0, collection.Count);

        public static IEnumerable<(T item, int index)> ZipWithIndices<T>(this IEnumerable<T> collection) => collection.Select((x, i) => (x, i));
    }
}
