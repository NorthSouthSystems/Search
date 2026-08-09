#nullable disable
namespace NorthSouthSystems.Search;

using NorthSouthSystems.BitVectors;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

public sealed partial class Engine<TBitVector, TItem, TPrimaryKey> : IEngine<TPrimaryKey>, IDisposable
    where TBitVector : IBitVector<TBitVector>
{
    public Engine(IBitVectorFactory<TBitVector> bitVectorFactory, Func<TItem, TPrimaryKey> primaryKeyExtractor)
    {
        _bitVectorFactory = bitVectorFactory;
        _primaryKeyExtractor = primaryKeyExtractor;
        _activeItems = _bitVectorFactory.Create(false);
    }

    private bool _configuring = true;
    private readonly ReaderWriterLockSlim _rwLock = new();

    private readonly IBitVectorFactory<TBitVector> _bitVectorFactory;
    private readonly Func<TItem, TPrimaryKey> _primaryKeyExtractor;
    private List<TPrimaryKey> _primaryKeys = new();
    private Dictionary<TPrimaryKey, int> _primaryKeyToActiveBitPositionMap = new();

    private TBitVector _activeItems;

    private List<CatalogPlusExtractor> _catalogsPlusExtractors = new();

    private class CatalogPlusExtractor
    {
        internal CatalogPlusExtractor(ICatalogInEngine<TBitVector> catalog, Func<TItem, object> keysOrKeyExtractor)
        {
            Catalog = catalog;
            KeysOrKeyExtractor = keysOrKeyExtractor;
        }

        internal readonly ICatalogInEngine<TBitVector> Catalog;
        internal readonly Func<TItem, object> KeysOrKeyExtractor;
    }

    public void Dispose() => _rwLock.Dispose();

    #region Catalog Management

    public ICatalogHandle<TKey> CreateCatalog<TKey>(string name, Func<TItem, TKey> keyExtractor)
        where TKey : IEquatable<TKey>, IComparable<TKey> =>
        CreateCatalogImpl<TKey>(name, true, item => (object)keyExtractor(item));

    public ICatalogHandle<TKey> CreateCatalog<TKey>(string name, Func<TItem, IEnumerable<TKey>> keysExtractor)
        where TKey : IEquatable<TKey>, IComparable<TKey> =>
        CreateCatalogImpl<TKey>(name, false, item => (object)keysExtractor(item));

    private Catalog<TBitVector, TKey> CreateCatalogImpl<TKey>(string name, bool isOneToOne, Func<TItem, object> keyOrKeysExtractor)
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        Catalog<TBitVector, TKey> catalog;

        try
        {
            _rwLock.EnterWriteLock();

            if (!_configuring)
                throw new NotSupportedException("Cannot create a Catalog in an Engine that has already called Add or CreateQuery.");

            if (_catalogsPlusExtractors.Any(cpe => cpe.Catalog.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "A Catalog already exists with the case-insensitive name : {0}.", name));

            catalog = new Catalog<TBitVector, TKey>(_bitVectorFactory, name, isOneToOne);
            _catalogsPlusExtractors.Add(new CatalogPlusExtractor(catalog, keyOrKeysExtractor));
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }

        return catalog;
    }

    // NOTE : No locking is necessary for the following Catalog methods because they are only called from the Query class, and in order to CreateQuery,
    // _configuring is stopped which prevents the addition of Catalogs.
    bool IEngine.HasCatalog(ICatalogHandle catalog) => _catalogsPlusExtractors.Any(cpe => cpe.Catalog == catalog);

    IEnumerable<ICatalogInEngine> IEngine.GetCatalogs() => GetCatalogs();

    internal IEnumerable<ICatalogInEngine<TBitVector>> GetCatalogs() => _catalogsPlusExtractors.Select(cpe => cpe.Catalog);

    ICatalogInEngine IEngine.GetCatalog(string name) => GetCatalog(name);

    internal ICatalogInEngine<TBitVector> GetCatalog(string name) =>
        _catalogsPlusExtractors.Single(cpe => cpe.Catalog.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Catalog;

    #endregion

    #region Optimize

    public void Optimize()
    {
        try
        {
            _rwLock.EnterUpgradeableReadLock();

            int[] bitPositionShifts = new int[_primaryKeys.Count];
            int index = 0;
            int exclusionCounter = 0;

            foreach (bool bit in _activeItems.Bits)
            {
                if (bit)
                {
                    bitPositionShifts[index] = exclusionCounter;
                }
                else
                {
                    bitPositionShifts[index] = -1;
                    exclusionCounter++;
                }

                index++;

                // GetBits will return the trailing 0s on the Vector, so we must break out before
                // we go out of bounds.
                if (index >= bitPositionShifts.Length)
                    break;
            }

            // A potential optimization would be to construct this Vector in an efficient manner able to leverage
            // the fact that we need exactly _activeItems.Population consecutive 1's.
            TBitVector optimizedActiveItems = default;

            var readActions = new List<Action>();
            readActions.Add(() => _activeItems.OptimizeReadPhase(bitPositionShifts, out optimizedActiveItems));
            readActions.AddRange(_catalogsPlusExtractors.Select(cpe => new Action(() => cpe.Catalog.OptimizeReadPhase(bitPositionShifts))));

            Parallel.Invoke(readActions.ToArray());

            var writeActions = new List<Action>();
            writeActions.Add(() => OptimizePrimaryKeys(bitPositionShifts));
            writeActions.AddRange(_catalogsPlusExtractors.Select(cpe => new Action(() => cpe.Catalog.OptimizeWritePhase())));

            _rwLock.EnterWriteLock();

            _activeItems = optimizedActiveItems;
            Parallel.Invoke(writeActions.ToArray());
        }
        finally
        {
            if (_rwLock.IsWriteLockHeld)
                _rwLock.ExitWriteLock();

            if (_rwLock.IsUpgradeableReadLockHeld)
                _rwLock.ExitUpgradeableReadLock();
        }
    }

    private void OptimizePrimaryKeys(int[] bitPositionShifts)
    {
        // PERF : Null the Dictionary instance member. In cases of local variables, I know that this behavior is unneccessary
        // because the garbage collector knows an instruction pointer for after which a given variable is no longer used.
        // For instances members, I do not know this to be the case. So, simply null the now unused parameter here to ensure that
        // it can be garbage collected if needed during this operation which will allocate a significant amount of memory.
        _primaryKeyToActiveBitPositionMap = null;

        _primaryKeys = _primaryKeys.Where((primaryKey, bitPosition) => bitPositionShifts[bitPosition] >= 0)
            .ToList();

        _primaryKeyToActiveBitPositionMap = _primaryKeys.Select((primaryKey, bitPosition) => new { PrimaryKey = primaryKey, BitPosition = bitPosition })
            .ToDictionary(pkbi => pkbi.PrimaryKey, pkbi => pkbi.BitPosition);
    }

    #endregion

    #region Add

    public void Add(TItem item)
    {
        try
        {
            _rwLock.EnterWriteLock();

            _configuring = false;

            AddItem(item);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    public void Add(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        try
        {
            _rwLock.EnterWriteLock();

            _configuring = false;

            foreach (TItem item in items)
                AddItem(item);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    private void AddItem(TItem item)
    {
        TPrimaryKey primaryKey = _primaryKeyExtractor(item);

        if (_primaryKeyToActiveBitPositionMap.ContainsKey(primaryKey))
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "An item already exists in this Engine with the primary key : {0}.", primaryKey));

        int bitPosition = _primaryKeys.Count;
        _primaryKeys.Add(primaryKey);
        _primaryKeyToActiveBitPositionMap.Add(primaryKey, bitPosition);
        _activeItems[bitPosition] = true;

        foreach (var cpe in _catalogsPlusExtractors)
            cpe.Catalog.Set(cpe.KeysOrKeyExtractor(item), bitPosition, true);
    }

    #endregion

    #region Update

    public void Update(TItem item)
    {
        try
        {
            _rwLock.EnterWriteLock();

            UpdateItem(item);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    public void Update(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        try
        {
            _rwLock.EnterWriteLock();

            foreach (TItem item in items)
                UpdateItem(item);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    private void UpdateItem(TItem item)
    {
        TPrimaryKey primaryKey = _primaryKeyExtractor(item);

        if (!_primaryKeyToActiveBitPositionMap.TryGetValue(primaryKey, out int fromBitPosition))
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "No item exists in this Engine with the primary key : {0}.", primaryKey));

        _activeItems[fromBitPosition] = false;

        int toBitPosition = _primaryKeys.Count;
        _primaryKeys.Add(primaryKey);
        _primaryKeyToActiveBitPositionMap[primaryKey] = toBitPosition;
        _activeItems[toBitPosition] = true;

        foreach (var cpe in _catalogsPlusExtractors)
            cpe.Catalog.Set(cpe.KeysOrKeyExtractor(item), toBitPosition, true);
    }

    #endregion

    #region Remove

    public void Remove(TItem item)
    {
        try
        {
            _rwLock.EnterWriteLock();

            RemoveItem(item);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    public void Remove(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        try
        {
            _rwLock.EnterWriteLock();

            foreach (TItem item in items)
                RemoveItem(item);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    private void RemoveItem(TItem item)
    {
        TPrimaryKey primaryKey = _primaryKeyExtractor(item);

        if (!_primaryKeyToActiveBitPositionMap.Remove(primaryKey, out int bitPosition))
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "No item exists in this Engine with the primary key : {0}.", primaryKey));

        _activeItems[bitPosition] = false;
    }

    #endregion

    #region Query

    public Query<TPrimaryKey> CreateQuery()
    {
        if (_configuring)
        {
            try
            {
                _rwLock.EnterWriteLock();
                _configuring = false;
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        return new(this);
    }

    ImmutableArray<TPrimaryKey> IEngine<TPrimaryKey>.ExecuteQuery(Query<TPrimaryKey> query, int skip, int take, out int totalCount)
    {
        try
        {
            _rwLock.EnterReadLock();

            var result = InitializeResult(query.AmongstPrimaryKeys);

            Filter(query, result);
            totalCount = result.Population;

            Facet(query, result);

            // Distinct is required because of Catalogs created from multi-key columns: e.g. think post/blog tags
            return
            [
                .. Sort(query, skip + take, result, totalCount)
                    .Distinct()
                    .Skip(skip)
                    .Take(take)
                    .Select(bitPosition => _primaryKeys[bitPosition])
            ];
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    private TBitVector InitializeResult(IReadOnlyList<TPrimaryKey> amongstPrimaryKeys)
    {
        TBitVector result;

        if (amongstPrimaryKeys.Count > 0)
        {
            result = _bitVectorFactory.Create(false);

            // No benchmarking was done to justify the OrderByDescending; however, the rationale
            // is that if we start by setting the maximum position, the Vector's underlying Array
            // will only have to undergo a single resizing. Obviously it comes at the price of a
            // QuickSort O(n log n).  However, we are already in an n sized loop so resizing  which
            // costs n each time could theoretically cost us O(n^2).
            foreach (int bitPosition in amongstPrimaryKeys
                         .Select(primaryKey => _primaryKeyToActiveBitPositionMap.GetValueOrDefault(primaryKey, -1))
                         .Where(position => position >= 0)
                         .OrderByDescending(position => position))
            {
                result[bitPosition] = true;
            }
        }
        else
            result = _bitVectorFactory.Create(false, _activeItems);

        return result;
    }

    private static void Filter(Query<TPrimaryKey> query, TBitVector result)
    {
        // TODO : Support for nested boolean logic.
        Trace.Assert(query.FilterClause == null || query.FilterClause.Operation == BooleanOperation.And);
        Trace.Assert(query.FilterClause == null || query.FilterClause.SubClauses.All(clause => clause is IFilterParameter));
        var filterParameters = query.FilterClause == null ? Enumerable.Empty<IFilterParameter>() : query.FilterClause.SubClauses.Cast<IFilterParameter>();

        foreach (IFilterParameter filterParameter in filterParameters)
        {
            var catalog = (ICatalogInEngine<TBitVector>)filterParameter.Catalog;

            switch (filterParameter.ParameterType)
            {
                case FilterParameterType.Exact:
                    catalog.FilterExact(result, filterParameter.Exact);
                    break;
                case FilterParameterType.Enumerable:
                    catalog.FilterEnumerable(result, filterParameter.Enumerable);
                    break;
                case FilterParameterType.Range:
                    catalog.FilterRange(result, filterParameter.RangeMin, filterParameter.RangeMax);
                    break;
                default:
                    throw new NotImplementedException(string.Format(CultureInfo.InvariantCulture, "Unrecognized filter parameter type : {0}.", filterParameter.ParameterType));
            }
        }
    }

    private static void Facet(Query<TPrimaryKey> query, TBitVector filterResult) =>
        Parallel.ForEach(query.FacetParametersInternal, new ParallelOptions { MaxDegreeOfParallelism = query.FacetDisableParallel ? 1 : -1 },
            facetParameter => facetParameter.Facet = ((ICatalogInEngine<TBitVector>)facetParameter.Catalog).Facet(filterResult, query.FacetDisableParallel, query.FacetShortCircuitCounting));

    private IEnumerable<int> Sort(Query<TPrimaryKey> query, int skipPlusTake, TBitVector filterResult, int totalCount)
    {
        var sortParameters = query.SortParameters.ToArray();
        int sortCount = sortParameters.Length + (query.SortPrimaryKeyAscending.HasValue ? 1 : 0);

        if (sortParameters.Length > 0)
        {
            CatalogSortResult<TBitVector> sortResult = null;

            for (int sortParameterIndex = 0; sortParameterIndex < sortParameters.Length; sortParameterIndex++)
            {
                // Disable parallel sorting if the user has specified to do so or if this is the last sort and we aren't going to have to iterate the entire result.
                var sortParameter = sortParameters[sortParameterIndex];
                bool sortDisableParallel = query.SortDisableParallel || (sortCount == (sortParameterIndex + 1) && skipPlusTake < totalCount);

                if (sortParameterIndex == 0)
                    sortResult = ((ICatalogInEngine<TBitVector>)sortParameter.Catalog).Sort(filterResult, true, sortParameter.Ascending, sortDisableParallel);
                else
                    sortResult = ((ICatalogInEngine<TBitVector>)sortParameter.Catalog).ThenSort(sortResult, true, sortParameter.Ascending, sortDisableParallel);
            }

            if (query.SortPrimaryKeyAscending.HasValue)
                return sortResult.PartialSorts.SelectMany(partialSort => SortBitPositionsByPrimaryKey(partialSort.GetBitPositions(true), query.SortPrimaryKeyAscending.Value));
            else
                return sortResult.PartialSorts.SelectMany(partialSort => partialSort.GetBitPositions(true));
        }
        else if (query.SortPrimaryKeyAscending.HasValue)
            return SortBitPositionsByPrimaryKey(filterResult.GetBitPositions(true), query.SortPrimaryKeyAscending.Value);
        else
            return filterResult.GetBitPositions(true);
    }

    private IEnumerable<int> SortBitPositionsByPrimaryKey(IEnumerable<int> bitPositions, bool ascending) =>
        ascending
            ? bitPositions.OrderBy(bitPosition => _primaryKeys[bitPosition])
            : bitPositions.OrderByDescending(bitPosition => _primaryKeys[bitPosition]);

    #endregion
}

internal interface IEngine<TPrimaryKey> : IEngine
{
    ImmutableArray<TPrimaryKey> ExecuteQuery(Query<TPrimaryKey> query, int skip, int take, out int totalCount);
}

internal interface IEngine
{
    bool HasCatalog(ICatalogHandle catalog);
    IEnumerable<ICatalogInEngine> GetCatalogs();
    ICatalogInEngine GetCatalog(string name);
}
