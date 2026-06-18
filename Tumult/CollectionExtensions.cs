using System.Collections;

namespace Tumult;

public static class CollectionExtensions
{
    public static IEnumerable<int> Between(int start, int end) => Enumerable.Range(start, end - start + 1);

    public static IEnumerable<(T first, T second)> Combinations<T>(this IReadOnlyList<T> list)
    {
        for (var i = 0; i < list.Count; i++)
        {
            for (var j = i + 1; j < list.Count; j++)
            {
                yield return (list[i], list[j]);
            }
        }
    }
        
    public static IEnumerable<T> RandomUnique<T>(this IReadOnlyList<T> list, int count) => list.Shuffled().Take(count);

    public static IList<T> Shuffled<T>(this IEnumerable<T> list)
    {
        var copy = new List<T>(list);
        copy.Shuffle();
        return copy;
    }

    public static IEnumerable<int> Indices(this ICollection collection) => Enumerable.Range(0, collection.Count);

    public static IEnumerable<(T item, int index)> ZipWithIndices<T>(this IEnumerable<T> collection) => collection.Select((x, i) => (x, i));
        
    public static IEnumerable<T> Except<T>(this IEnumerable<T> enumerable, T item) => enumerable.Where(x => !EqualityComparer<T>.Default.Equals(x, item));
        
    public static IEnumerable<T> WhereNonNull<T>(this IEnumerable<T?> enumerable) => enumerable.Where(x => x != null)!;
    public static IAsyncEnumerable<T> WhereNonNull<T>(this IAsyncEnumerable<T?> enumerable) => enumerable.Where(x => x != null)!;
    public static IEnumerable<T1> WhereNonNull<T1, T2>(this IEnumerable<T1> enumerable, Func<T1, T2?> extractor) => enumerable.Where(x => extractor(x) != null);

    /// <summary>
    /// Combines two sequences into a single sequence of tuples. If one sequence is shorter, it is padded with
    /// default values to match the length other sequence
    /// </summary>
    public static IEnumerable<(T1?, T2?)> ZipLongest<T1, T2>(this IEnumerable<T1> enumerable,
        IEnumerable<T2> enumerable2)
    {
        using var enumerator1 = enumerable.GetEnumerator();
        using var enumerator2 = enumerable2.GetEnumerator();

        var hasLeft = enumerator1.MoveNext();
        var hasRight = enumerator2.MoveNext();

        while (hasLeft || hasRight)
        {
            if (hasLeft && hasRight)
            {
                yield return (enumerator1.Current, enumerator2.Current);
            }
            else if (hasLeft)
            {
                yield return (enumerator1.Current, default);
            }
            else if (hasRight)
            {
                yield return (default, enumerator2.Current);
            }

            hasLeft = enumerator1.MoveNext();
            hasRight = enumerator2.MoveNext();
        }
    }
}