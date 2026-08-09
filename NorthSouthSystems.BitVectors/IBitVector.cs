namespace NorthSouthSystems.BitVectors;

public interface IBitVector<TBitVector>
    where TBitVector : IBitVector<TBitVector>
{
    // TODO : Eliminate the "need" for this, or at a minimum find a better name.
    bool IsUnused { get; }

    bool OptimizeReadPhase(int[] bitPositionShifts, out TBitVector optimized);

    void Clear();

    bool this[int bitPosition] { get; set; }

    int Population { get; }

    IEnumerable<bool> Bits { get; }
    IEnumerable<int> GetBitPositions(bool value);

    void DecompressInPlace(TBitVector vector);
    void AndInPlace(TBitVector vector);
    TBitVector AndOutOfPlace(TBitVector vector, bool resultIsCompressed);
    int AndPopulation(TBitVector vector);
    bool AndPopulationAny(TBitVector vector);
    void OrInPlace(TBitVector vector);
}
