using NorthSouthSystems.BitVectors;

public class T_Catalog_Construction
{
    [Theory]
    [ClassData(typeof(T_BitVectorFactories))]
    public void Public<TBitVector>(IBitVectorFactory<TBitVector> bitVectorFactory)
        where TBitVector : IBitVector<TBitVector>
    {
        var catalog = new Catalog<TBitVector, int>(bitVectorFactory, "SomeInt", true);
        catalog.Name.Should().Be("SomeInt");
    }
}
