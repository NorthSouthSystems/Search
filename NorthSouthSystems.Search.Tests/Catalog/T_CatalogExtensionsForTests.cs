using NorthSouthSystems.BitVectors;

internal static class T_CatalogExtensionsForTests
{
    internal static void Fill<TBitVector, TKey>(this Catalog<TBitVector, TKey> catalog, TKey key, int[] bitPositions, bool value)
        where TBitVector : IBitVector<TBitVector>
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        foreach (int bitPosition in bitPositions)
            catalog.Set(key, bitPosition, value);
    }
}
