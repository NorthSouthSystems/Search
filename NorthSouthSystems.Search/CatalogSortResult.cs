namespace NorthSouthSystems.Search;

using NorthSouthSystems.BitVectors;

public sealed class CatalogSortResult<TBitVector>
    where TBitVector : IBitVector<TBitVector>
{
    internal CatalogSortResult(IEnumerable<TBitVector> partialSorts) =>
        PartialSorts = partialSorts;

    public IEnumerable<TBitVector> PartialSorts { get; }
}
