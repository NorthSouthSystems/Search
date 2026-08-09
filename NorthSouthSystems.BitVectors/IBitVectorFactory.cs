namespace NorthSouthSystems.BitVectors;

public interface IBitVectorFactory<TBitVector>
    where TBitVector : IBitVector<TBitVector>
{
    TBitVector Create(bool isCompressed);
    TBitVector Create(bool isCompressed, TBitVector copy);
    TBitVector CreateUncompressedUnion(params TBitVector[] bitVectors);

    int WordSize { get; }
}
