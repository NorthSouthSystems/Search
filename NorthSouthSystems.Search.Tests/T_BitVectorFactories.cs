using System.Collections;

public class T_BitVectorFactories : IEnumerable<object[]>
{
    private static readonly object[] _bitVectorFactories =
    [
        new NorthSouthSystems.BitVectors.PLWAH.PLWAHVectorFactory(),
        new NorthSouthSystems.BitVectors.PLWAH64.PLWAH64VectorFactory(),
        new NorthSouthSystems.BitVectors.WAH.WAHVectorFactory(),
        new NorthSouthSystems.BitVectors.WAH64.WAH64VectorFactory()
    ];

    private static readonly List<object[]> BitVectorFactoriesWrapped =
        _bitVectorFactories.Select(bvf => new object[] { bvf }).ToList();

    public IEnumerator<object[]> GetEnumerator() => BitVectorFactoriesWrapped.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => BitVectorFactoriesWrapped.GetEnumerator();
}
