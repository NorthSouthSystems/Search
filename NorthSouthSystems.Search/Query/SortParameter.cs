namespace NorthSouthSystems.Search;

public static class SortParameter
{
    public static SortParameter<TKey> Create<TKey>(ICatalogHandle<TKey> catalog, bool ascending)
        where TKey : IEquatable<TKey>, IComparable<TKey> =>
        new(catalog, ascending);

    internal static ISortParameter Create(IEngine engine, string catalogName, bool ascending) =>
        engine.GetCatalog(catalogName).CreateSortParameter(ascending);
}

public sealed class SortParameter<TKey> : ISortParameter
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    internal SortParameter(ICatalogHandle<TKey> catalog, bool ascending)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        Ascending = ascending;
    }

    public ICatalogHandle Catalog { get; }
    public bool Ascending { get; }
}

public interface ISortParameter : IParameter
{
    bool Ascending { get; }
}
