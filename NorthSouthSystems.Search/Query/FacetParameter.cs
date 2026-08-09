namespace NorthSouthSystems.Search;

public static class FacetParameter
{
    public static FacetParameter<TKey> Create<TKey>(ICatalogHandle<TKey> catalog)
        where TKey : IEquatable<TKey>, IComparable<TKey> =>
        new(catalog);

    internal static IFacetParameter Create(IEngine engine, string catalogName) =>
        engine.GetCatalog(catalogName).CreateFacetParameter();
}

public sealed class FacetParameter<TKey> : IFacetParameterInternal
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    internal FacetParameter(ICatalogHandle<TKey> catalog) =>
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    public ICatalogHandle Catalog { get; }

    public Facet<TKey> Facet
    {
        get
        {
            if (!_facetSet)
                throw new NotSupportedException("Query must be executed before Facet is available.");

            return _facet!;
        }
        private set
        {
            _facet = value;
            _facetSet = true;
        }
    }

    private Facet<TKey>? _facet;
    private bool _facetSet;

    #region IFacetParameterInternal

    IFacet IFacetParameterInternal.Facet
    {
        get => Facet;
        set => Facet = (Facet<TKey>)value;
    }

    #endregion

    #region IFacetParameter

    IFacet IFacetParameter.Facet => Facet;

    #endregion
}

internal interface IFacetParameterInternal : IFacetParameter
{
    new IFacet Facet { get; set; }
}

public interface IFacetParameter : IParameter
{
    IFacet Facet { get; }
}
