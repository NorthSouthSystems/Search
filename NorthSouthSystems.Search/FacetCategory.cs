namespace NorthSouthSystems.Search;

public readonly struct FacetCategory<TKey> : IFacetCategory, IEquatable<FacetCategory<TKey>>
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    internal FacetCategory(TKey key, int count)
    {
        Key = key;
        Count = count;
    }

    public TKey Key { get; }
    public int Count { get; }

    #region IFacetCategory

    object IFacetCategory.Key => Key;

    #endregion

    #region Equality & Hashing

    public bool Equals(FacetCategory<TKey> other) =>
        Key.Equals(other.Key) && Count.Equals(other.Count);

    public override bool Equals(object? obj) => obj is FacetCategory<TKey> category && Equals(category);

    public static bool operator ==(FacetCategory<TKey> left, FacetCategory<TKey> right) => left.Equals(right);
    public static bool operator !=(FacetCategory<TKey> left, FacetCategory<TKey> right) => !left.Equals(right);

    public override int GetHashCode() => Key.GetHashCode() ^ Count.GetHashCode();

    #endregion
}

public interface IFacetCategory
{
    object Key { get; }
    int Count { get; }
}
