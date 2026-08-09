namespace NorthSouthSystems.Search;

public sealed class Facet<TKey> : IFacet
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    internal Facet(IEnumerable<FacetCategory<TKey>> categories) =>
        Categories = categories.Where(category => category.Count > 0).ToArray();

    public IEnumerable<FacetCategory<TKey>> Categories { get; }

    #region IFacet

    IEnumerable<IFacetCategory> IFacet.Categories => Categories.Cast<IFacetCategory>();

    #endregion
}

public interface IFacet
{
    IEnumerable<IFacetCategory> Categories { get; }
}
