using NorthSouthSystems.BitVectors;

public class T_FilterParameter
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
            var someStringCatalog = engine.CreateCatalog("SomeString", item => item.SomeString);

            var someStringParameter = FilterParameter.Create(someStringCatalog, null, "A");
            someStringParameter = FilterParameter.Create(someStringCatalog, "A", null);
        };
        act.Should().NotThrow(because: "FilterParameterRangeArgumentNullOK");

        act = () =>
        {
            var engine = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);
            var someStringCatalog = engine.CreateCatalog("SomeString", item => item.SomeString);

            var someStringParameter = FilterParameter.Create(someStringCatalog, null, null);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "FilterParameterRangeArgumentNull");

        act = () =>
        {
            var engine = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);
            var someStringCatalog = engine.CreateCatalog("SomeString", item => item.SomeString);

            var someStringParameter = FilterParameter.Create(someStringCatalog, "B", "A");
        };
        act.Should().ThrowExactly<ArgumentOutOfRangeException>(because: "FilterParameterRangeArgumentOutOfRange");
    }
}
