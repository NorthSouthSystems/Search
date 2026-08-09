namespace NorthSouthSystems.Search;

using NorthSouthSystems.BitVectors;
using System.Collections;
using System.Globalization;

public sealed partial class Catalog<TBitVector, TKey> : ICatalogInEngine<TBitVector>, ICatalogHandle<TKey>
    where TBitVector : IBitVector<TBitVector>
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    public Catalog(IBitVectorFactory<TBitVector> bitVectorFactory, string name, bool isOneToOne)
    {
        _bitVectorFactory = bitVectorFactory;

        Name = name;
        IsOneToOne = isOneToOne;
    }

    private readonly IBitVectorFactory<TBitVector> _bitVectorFactory;

    public string Name { get; }
    public bool IsOneToOne { get; }

    private readonly SortedSet<TKey> _keys = new();
    private readonly Dictionary<TKey, Entry> _keyToEntryMap = new();

    // TODO : Better OOP... right now this is just serving as a mutable Tuple which is probably
    // not the best design.
    private class Entry
    {
        internal Entry(TBitVector vector) => Vector = vector;

        internal TBitVector Vector;
        internal TBitVector? VectorOptimized;
        internal bool IsVectorOptimizedAlive;
    }

    #region Optimize

    void ICatalogInEngine.OptimizeReadPhase(int[] bitPositionShifts)
    {
        foreach (Entry entry in _keyToEntryMap.Values)
            entry.IsVectorOptimizedAlive = entry.Vector.OptimizeReadPhase(bitPositionShifts, out entry.VectorOptimized);
    }

    void ICatalogInEngine.OptimizeWritePhase()
    {
        var deadKeys = new List<TKey>();

        foreach (var keyAndEntry in _keyToEntryMap)
        {
            if (keyAndEntry.Value.IsVectorOptimizedAlive)
                keyAndEntry.Value.Vector = keyAndEntry.Value.VectorOptimized!;
            else
                deadKeys.Add(keyAndEntry.Key);

            keyAndEntry.Value.VectorOptimized = default;
            keyAndEntry.Value.IsVectorOptimizedAlive = false;
        }

        foreach (TKey key in deadKeys)
        {
            _keys.Remove(key);
            _keyToEntryMap.Remove(key);
        }
    }

    #endregion

    #region Set

    void ICatalogInEngine.Set(object key, int bitPosition, bool value)
    {
        if (key is TKey tkey)
            Set(tkey, bitPosition, value);
        else
            Set((IEnumerable<TKey>)key, bitPosition, value);
    }

    public void Set(TKey key, int bitPosition, bool value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (!_keyToEntryMap.TryGetValue(key, out Entry? entry))
        {
            entry = new Entry(_bitVectorFactory.Create(true));

            _keys.Add(key);
            _keyToEntryMap.Add(key, entry);
        }

        entry.Vector[bitPosition] = value;
    }

    public void Set(IEnumerable<TKey> keys, int bitPosition, bool value)
    {
        ArgumentNullException.ThrowIfNull(keys);

        if (IsOneToOne)
            throw new NotSupportedException("One-to-one Catalogs must use Set(TKey key, ...) instead.");

        foreach (TKey key in keys)
            Set(key, bitPosition, value);
    }

    #endregion

    #region Filter

    IFilterParameter ICatalogInEngine.CreateFilterParameter(object exact) =>
        new FilterParameter<TKey>(this, ConvertToTKey(exact));

    IFilterParameter ICatalogInEngine.CreateFilterParameter(IEnumerable enumerable) =>
        new FilterParameter<TKey>(this, enumerable.Cast<object>().Select(ConvertToTKey));

    IFilterParameter ICatalogInEngine.CreateFilterParameter(object rangeMin, object rangeMax) =>
        new FilterParameter<TKey>(this, ConvertToTKey(rangeMin), ConvertToTKey(rangeMax));

    private static TKey ConvertToTKey(object obj) => (TKey)Convert.ChangeType(obj, typeof(TKey), CultureInfo.InvariantCulture);

    void ICatalogInEngine<TBitVector>.FilterExact(TBitVector vector, object key) => Filter(vector, (TKey)key);

    public void Filter(TBitVector vector, TKey key)
    {
        if (vector == null)
            throw new ArgumentNullException(nameof(vector));

        if (key == null)
            throw new ArgumentNullException(nameof(key));

        FilterImpl(vector, [Lookup(key)]);
    }

    void ICatalogInEngine<TBitVector>.FilterEnumerable(TBitVector vector, IEnumerable keys) => Filter(vector, (IEnumerable<TKey>)keys);

    public void Filter(TBitVector vector, IEnumerable<TKey> keys)
    {
        if (vector == null)
            throw new ArgumentNullException(nameof(vector));

        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Any(key => key == null))
            throw new ArgumentNullException(nameof(keys), "All keys must be non-null.");

        FilterImpl(vector, keys.Distinct().Select(Lookup));
    }

    void ICatalogInEngine<TBitVector>.FilterRange(TBitVector vector, object keyMin, object keyMax) => Filter(vector, (TKey)keyMin, (TKey)keyMax);

    public void Filter(TBitVector vector, TKey? keyMin, TKey? keyMax)
    {
        if (vector == null)
            throw new ArgumentNullException(nameof(vector));

        if (keyMin == null && keyMax == null)
            throw new ArgumentNullException(nameof(keyMin), "Either keyMin or keyMax must be non-null.");

        if (keyMin != null && keyMax != null && keyMin.CompareTo(keyMax) > 0)
            throw new ArgumentOutOfRangeException(nameof(keyMin), "keyMin must be <= keyMax.");

        keyMin ??= _keys.Min;
        keyMax ??= _keys.Max;

        FilterImpl(vector, _keys.Count == 0 ? Array.Empty<TBitVector>() : _keys.GetViewBetween(keyMin, keyMax).Select(key => _keyToEntryMap[key].Vector));
    }

    private void FilterImpl(TBitVector vector, IEnumerable<TBitVector?> lookups)
    {
        TBitVector[] lookupsArray = lookups.Where(lookup => lookup != null).Select(lookup => lookup!).ToArray();

        if (lookupsArray.Length == 0)
            vector.Clear();
        else if (lookupsArray.Length == 1)
            vector.AndInPlace(lookupsArray[0]);
        else
            vector.AndInPlace(_bitVectorFactory.CreateUncompressedUnion(lookupsArray));
    }

    private TBitVector? Lookup(TKey key) => _keyToEntryMap.TryGetValue(key, out Entry? entry) ? entry.Vector : default;

    #endregion

    #region Sort

    ISortParameter ICatalogInEngine.CreateSortParameter(bool ascending) => new SortParameter<TKey>(this, ascending);

    CatalogSortResult<TBitVector> ICatalogInEngine<TBitVector>.Sort(TBitVector vector, bool value, bool ascending, bool disableParallel) =>
        Sort(vector, value, ascending, disableParallel);

    public CatalogSortResult<TBitVector> Sort(TBitVector vector, bool value, bool ascending, bool disableParallel)
    {
        if (vector == null)
            throw new ArgumentNullException(nameof(vector));

        // TODO : Scope value entirely? Is value = false a legitimate use-case?
        if (!value)
            throw new NotImplementedException();

        // This uses SortedSet<T>.Reverse() and not the IEnumerable<T> extension method that suffers from greedy enumeration.
        var keys = ascending ? _keys : _keys.Reverse();

        // TODO : Support parallelization. In initial testing (with the admittedly small unit tests), parallelization was significantly slower.
        // TODO : Optimize the resultCompression?
        var partialSorts = keys.Select(key => vector.AndOutOfPlace(_keyToEntryMap[key].Vector, true))
            .Where(partialSort => !partialSort.IsUnused);

        return new CatalogSortResult<TBitVector>(partialSorts);
    }

    CatalogSortResult<TBitVector> ICatalogInEngine<TBitVector>.ThenSort(CatalogSortResult<TBitVector> sortResult, bool value, bool ascending, bool disableParallel) =>
        ThenSort(sortResult, value, ascending, disableParallel);

    public CatalogSortResult<TBitVector> ThenSort(CatalogSortResult<TBitVector> sortResult, bool value, bool ascending, bool disableParallel)
    {
        ArgumentNullException.ThrowIfNull(sortResult);

        // TODO : Scope value entirely? Is value = false a legitimate use-case?
        if (!value)
            throw new NotImplementedException();

        // This uses SortedSet<T>.Reverse() and not the IEnumerable<T> extension method that suffers from greedy enumeration.
        var keys = ascending ? _keys : _keys.Reverse();

        // TODO : Support parallelization. In initial testing (with the admittedly small unit tests), parallelization was significantly slower.
        // TODO : Optimize the resultCompression?
        var partialSorts = sortResult.PartialSorts
            .SelectMany(partialSort => keys.Select(key => partialSort.AndOutOfPlace(_keyToEntryMap[key].Vector, true)))
            .Where(partialSort => !partialSort.IsUnused);

        return new CatalogSortResult<TBitVector>(partialSorts);
    }

    #endregion

    #region Facet

    IFacetParameterInternal ICatalogInEngine.CreateFacetParameter() => new FacetParameter<TKey>(this);

    IFacet ICatalogInEngine<TBitVector>.Facet(TBitVector vector, bool disableParallel, bool shortCircuitCounting) => Facet(vector, disableParallel, shortCircuitCounting);

    public Facet<TKey> Facet(TBitVector vector, bool disableParallel = false, bool shortCircuitCounting = false)
    {
        if (vector == null)
            throw new ArgumentNullException(nameof(vector));

        var keyAndEntries = _keyToEntryMap.AsParallel();

        if (disableParallel)
            keyAndEntries = keyAndEntries.WithDegreeOfParallelism(1);

        var categories = shortCircuitCounting
            ? keyAndEntries.Where(keyAndEntry => vector.AndPopulationAny(keyAndEntry.Value.Vector)).Select(keyAndEntry => new FacetCategory<TKey>(keyAndEntry.Key, 1))
            : keyAndEntries.Select(keyAndEntry => new FacetCategory<TKey>(keyAndEntry.Key, vector.AndPopulation(keyAndEntry.Value.Vector)));

        return new Facet<TKey>(categories);
    }

    #endregion
}

internal interface ICatalogInEngine<TBitVector> : ICatalogInEngine
    where TBitVector : IBitVector<TBitVector>
{
    void FilterExact(TBitVector vector, object key);
    void FilterEnumerable(TBitVector vector, IEnumerable keys);
    void FilterRange(TBitVector vector, object keyMin, object keyMax);

    CatalogSortResult<TBitVector> Sort(TBitVector vector, bool value, bool ascending, bool disableParallel);
    CatalogSortResult<TBitVector> ThenSort(CatalogSortResult<TBitVector> sortResult, bool value, bool ascending, bool disableParallel);

    IFacet Facet(TBitVector vector, bool disableParallel, bool shortCircuitCounting);
}

internal interface ICatalogInEngine : ICatalogHandle
{
    void OptimizeReadPhase(int[] bitPositionShifts);
    void OptimizeWritePhase();

    void Set(object key, int bitPosition, bool value);

    IFilterParameter CreateFilterParameter(object exact);
    IFilterParameter CreateFilterParameter(IEnumerable enumerable);
    IFilterParameter CreateFilterParameter(object rangeMin, object rangeMax);

    ISortParameter CreateSortParameter(bool ascending);

    IFacetParameterInternal CreateFacetParameter();
}

public interface ICatalogHandle<TKey> : ICatalogHandle;

public interface ICatalogHandle
{
    string Name { get; }
    bool IsOneToOne { get; }
}
