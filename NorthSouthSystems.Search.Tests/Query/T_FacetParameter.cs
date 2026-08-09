using NorthSouthSystems.BitVectors;

public class T_FacetParameter
{
    [Theory]
    [ClassData(typeof(T_BitVectorFactories))]
    public void Exceptions<TBitVector>(IBitVectorFactory<TBitVector> bitVectorFactory)
        where TBitVector : IBitVector<TBitVector>
    {
        Action act;

        act = () =>
        {
            var engine = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);
            var someIntCatalog = engine.CreateCatalog("SomeInt", item => item.SomeInt);

            var someIntFacet = FacetParameter.Create(someIntCatalog);

            var facet = someIntFacet.Facet;
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "FacetQueryNotExecuted");
    }
}
