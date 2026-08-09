using System.Collections.Immutable;
using System.Diagnostics;

namespace NorthSouthSystems.Search;

public sealed class Query<TPrimaryKey>
{
    internal Query(IEngine<TPrimaryKey> engine) =>
        _engine = engine;

    private readonly IEngine<TPrimaryKey> _engine;

    private void ThrowIfEngineMismatch(ICatalogHandle catalog)
    {
        if (!_engine.HasCatalog(catalog))
            throw new ArgumentException("Catalog belongs to a different Engine.", nameof(catalog));
    }

    #region Amongst

    internal IReadOnlyList<TPrimaryKey> AmongstPrimaryKeys => _amongstPrimaryKeys;
    private readonly List<TPrimaryKey> _amongstPrimaryKeys = new();

    public Query<TPrimaryKey> Amongst(IEnumerable<TPrimaryKey> primaryKeys)
    {
        ThrowIfExecuted();

        if (_amongstPrimaryKeys.Count > 0)
            throw new NotSupportedException("Amongst may only be called once.");

        _amongstPrimaryKeys.AddRange(primaryKeys ?? Enumerable.Empty<TPrimaryKey>());

        return this;
    }

    #endregion

    #region Filter

    internal FilterClause? FilterClause { get; private set; }

    public Query<TPrimaryKey> Filter(FilterClause filterClause)
    {
        ThrowIfExecuted();

        if (FilterClause != null)
            throw new NotSupportedException("Filter may only be called once.");

        foreach (var filterParameter in (filterClause == null ? [] : filterClause.AllFilterParameters()))
            ThrowIfEngineMismatch(filterParameter.Catalog);

        FilterClause = filterClause;

        return this;
    }

    #endregion

    #region Sort

    internal IEnumerable<ISortParameter> SortParameters => _sortParameters;
    private readonly List<ISortParameter> _sortParameters = new();

    public Query<TPrimaryKey> Sort(params ISortParameter[] sortParameters)
    {
        ThrowIfExecuted();

        if (_sortParameters.Count > 0)
            throw new NotSupportedException("Sort may only be called once.");

        if (SortPrimaryKeyAscending.HasValue)
            throw new NotSupportedException("Sort must be called before SortPrimaryKey.");

        foreach (var sortParameter in (sortParameters ?? []).Where(p => p != null))
        {
            ThrowIfEngineMismatch(sortParameter.Catalog);

            if (_sortParameters.Any(parameter => parameter.Catalog == sortParameter.Catalog))
                throw new NotSupportedException("Can only add 1 Sort Parameter per Catalog.");

            _sortParameters.Add(sortParameter);
        }

        return this;
    }

    internal bool? SortPrimaryKeyAscending { get; private set; }

    public Query<TPrimaryKey> SortPrimaryKey(bool ascending)
    {
        ThrowIfExecuted();

        if (SortPrimaryKeyAscending.HasValue)
            throw new NotSupportedException("SortPrimaryKey may only be called once.");

        SortPrimaryKeyAscending = ascending;

        return this;
    }

    internal bool SortDisableParallel { get; private set; }

    public Query<TPrimaryKey> WithSortOptions(bool sortDisableParallel = false)
    {
        ThrowIfExecuted();

        SortDisableParallel = sortDisableParallel;

        return this;
    }

    #endregion

    #region Facet

    internal IEnumerable<IFacetParameterInternal> FacetParametersInternal => _facetParameters;
    private readonly List<IFacetParameterInternal> _facetParameters = new();

    public Query<TPrimaryKey> FacetAll()
    {
        var facetParameters = _engine.GetCatalogs()
            .Select(catalog => (IFacetParameter)catalog.CreateFacetParameter())
            .ToArray();

        return Facet(facetParameters);
    }

    public Query<TPrimaryKey> Facet(params IFacetParameter[] facetParameters)
    {
        ThrowIfExecuted();

        if (_facetParameters.Count > 0)
            throw new NotSupportedException("Facet may only be called once.");

        foreach (var facetParameter in (facetParameters ?? Enumerable.Empty<IFacetParameter>()).Where(p => p != null))
        {
            ThrowIfEngineMismatch(facetParameter.Catalog);

            if (_facetParameters.Any(parameter => parameter.Catalog == facetParameter.Catalog))
                throw new NotSupportedException("Can only add 1 Facet Parameter per Catalog.");

            _facetParameters.Add((IFacetParameterInternal)facetParameter);
        }

        return this;
    }

    internal bool FacetDisableParallel { get; private set; }
    internal bool FacetShortCircuitCounting { get; private set; }

    public Query<TPrimaryKey> WithFacetOptions(bool facetDisableParallel = false, bool facetShortCircuitCounting = false)
    {
        ThrowIfExecuted();

        FacetDisableParallel = facetDisableParallel;
        FacetShortCircuitCounting = facetShortCircuitCounting;

        return this;
    }

    #endregion

    #region Execution

    public bool Executed { get; private set; }

    private void ThrowIfExecuted()
    {
        if (Executed)
            throw new NotSupportedException("Query has already been executed.");
    }

    public QueryResult<TPrimaryKey> Execute(int skip, int take)
    {
        ThrowIfExecuted();

        Executed = true;

        var watch = new Stopwatch();
        watch.Start();

        var resultPrimaryKeys = _engine.ExecuteQuery(this, skip, take, out int totalCount);

        watch.Stop();

        return new(resultPrimaryKeys, totalCount, watch.Elapsed);
    }

    #endregion
}

public class QueryResult<TPrimaryKey>
{
    internal QueryResult(ImmutableArray<TPrimaryKey> primaryKeys, int totalCount, TimeSpan elapsed)
    {
        PrimaryKeys = primaryKeys;
        TotalCount = totalCount;
        Elapsed = elapsed;
    }

    public ImmutableArray<TPrimaryKey> PrimaryKeys { get; }
    public int TotalCount { get; }
    public TimeSpan Elapsed { get; }
}
