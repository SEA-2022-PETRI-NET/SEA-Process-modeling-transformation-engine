namespace ProcessModelingTransformationEngine.Domain.Utils;

public static class LinqExtensions
{
    // Intersect but preserves duplicates
    public static IEnumerable<TSource> IntersectAll<TSource>(
        this IEnumerable<TSource> first,
        IEnumerable<TSource> second,
        IEqualityComparer<TSource> comparer = null)
    {
        if (first == null)
        {
            throw new ArgumentNullException(nameof(first));   
        }

        if (second == null)
        {
            throw new ArgumentNullException(nameof(second));
        }
        
        var secondSet = second as HashSet<TSource> ?? 
                        second.ToHashSet(comparer ?? EqualityComparer<TSource>.Default);

        // Contains is O(1) for HashSet
        return first.Where(v => secondSet.Contains(v));
    }
    
    // Except but preserves duplicates
    public static IEnumerable<TSource> ExceptAll<TSource>(
        this IEnumerable<TSource> first,
        IEnumerable<TSource> second,
        IEqualityComparer<TSource> comparer = null)
    {
        if (first == null)
        {
            throw new ArgumentNullException(nameof(first));   
        }

        if (second == null)
        {
            throw new ArgumentNullException(nameof(second));
        }
        
        var secondSet = second as HashSet<TSource> ?? 
                        second.ToHashSet(comparer ?? EqualityComparer<TSource>.Default);

        // Contains is O(1) for HashSet
        return first.Where(v => !secondSet.Contains(v));
    }
}