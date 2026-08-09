#if POSITIONLISTENABLED && WORDSIZE64
namespace NorthSouthSystems.BitVectors.PLWAH64;
#elif POSITIONLISTENABLED
namespace NorthSouthSystems.BitVectors.PLWAH;
#elif WORDSIZE64
namespace NorthSouthSystems.BitVectors.WAH64;
#else
namespace NorthSouthSystems.BitVectors.WAH;
#endif

#if POSITIONLISTENABLED && WORDSIZE64
public class PLWAH64VectorFactory
#elif POSITIONLISTENABLED
public class PLWAHVectorFactory
#elif WORDSIZE64
public class WAH64VectorFactory
#else
public class WAHVectorFactory
#endif
    : IBitVectorFactory<Vector>
{
    public Vector Create(bool isCompressed) =>
        new(isCompressed);

    public Vector Create(bool isCompressed, Vector copy) =>
        new(isCompressed, copy);

    public Vector CreateUncompressedUnion(params Vector[] bitVectors) =>
        Vector.OrOutOfPlace(bitVectors);

    public int WordSize => Word.SIZE;
}
