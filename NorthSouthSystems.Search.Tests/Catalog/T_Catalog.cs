using NorthSouthSystems.BitVectors;

public class T_Catalog
{
    [Theory]
    [ClassData(typeof(T_BitVectorFactories))]
    public void SortBitPositions<TBitVector>(IBitVectorFactory<TBitVector> bitVectorFactory)
        where TBitVector : IBitVector<TBitVector>
    {
        var catalog = new Catalog<TBitVector, int>(bitVectorFactory, "SomeInt", true);
        catalog.Set(0, 5, true);
        catalog.Set(1, 6, true);
        catalog.Set(2, 7, true);
        catalog.Set(3, 8, true);
        catalog.Set(4, 9, true);

        var vector = bitVectorFactory.Create(false);
        vector[4] = true;
        vector[5] = true;
        vector[6] = true;
        vector[7] = true;
        vector[8] = true;
        vector[9] = true;
        vector[10] = true;

        foreach (bool disableParallel in new[] { false, true })
        {
            var result = catalog.Sort(vector, true, false, disableParallel);
            int[] bitPositions = result.PartialSorts.SelectMany(partial => partial.GetBitPositions(true)).ToArray();
            bitPositions.Should().Equal(9, 8, 7, 6, 5);

            var partialSorts = result.PartialSorts.ToArray();
            partialSorts.Length.Should().Be(5);

            for (int i = 0; i < 5; i++)
                partialSorts[i].GetBitPositions(true).Single().Should().Be(9 - i);
        }
    }

    [Theory]
    [ClassData(typeof(T_BitVectorFactories))]
    public void Exceptions<TBitVector>(IBitVectorFactory<TBitVector> bitVectorFactory)
        where TBitVector : IBitVector<TBitVector>
    {
        Action act;

        act = () =>
        {
            var catalog = new Catalog<TBitVector, string>(bitVectorFactory, "SomeString", true);
            catalog.Set((string)null, 777, true);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "SetNull");

        act = () =>
        {
            var catalog = new Catalog<TBitVector, int>(bitVectorFactory, "SomeInt", true);
            catalog.Set((int[])null, 777, true);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "SetEnumerableNull");

        act = () =>
        {
            var catalog = new Catalog<TBitVector, string>(bitVectorFactory, "SomeString", true);
            catalog.Filter(default, "A");
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "FilterExactVectorNull");

        act = () =>
        {
            var vector = bitVectorFactory.Create(true);

            var catalog = new Catalog<TBitVector, string>(bitVectorFactory, "SomeString", true);
            catalog.Filter(vector, (string)null);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "FilterExactKeyNull");

        act = () =>
        {
            var catalog = new Catalog<TBitVector, string>(bitVectorFactory, "SomeString", true);
            catalog.Filter(default, new[] { "A", "B" });
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "FilterEnumerableVectorNull");

        act = () =>
        {
            var vector = bitVectorFactory.Create(true);

            var catalog = new Catalog<TBitVector, string>(bitVectorFactory, "SomeString", true);
            catalog.Filter(vector, (string[])null);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "FilterEnumerableKeysNull");

        act = () =>
        {
            var vector = bitVectorFactory.Create(true);

            var catalog = new Catalog<TBitVector, string>(bitVectorFactory, "SomeString", true);
            catalog.Filter(vector, new[] { "A", null });
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "FilterEnumerableKeysKeyNull");

        act = () =>
        {
            var catalog = new Catalog<TBitVector, string>(bitVectorFactory, "SomeString", true);
            catalog.Filter(default, "A", "B");
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "FilterRangeVectorNull");

        act = () =>
        {
            var vector = bitVectorFactory.Create(true);

            var catalog = new Catalog<TBitVector, string>(bitVectorFactory, "SomeString", true);
            catalog.Filter(vector, (string)null, "A");
            catalog.Filter(vector, "A", (string)null);
        };
        act.Should().NotThrow(because: "FilterRangeKeyMinMaxOK");

        act = () =>
        {
            var vector = bitVectorFactory.Create(true);

            var catalog = new Catalog<TBitVector, string>(bitVectorFactory, "SomeString", true);
            catalog.Filter(vector, (string)null, (string)null);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "FilterRangeKeyMinMaxNull");

        act = () =>
        {
            var vector = bitVectorFactory.Create(true);

            var catalog = new Catalog<TBitVector, string>(bitVectorFactory, "SomeString", true);
            catalog.Filter(vector, "B", "A");
        };
        act.Should().ThrowExactly<ArgumentOutOfRangeException>(because: "FilterRangeKeyMinMaxOutOfRange");

        act = () =>
        {
            var catalog = new Catalog<TBitVector, string>(bitVectorFactory, "SomeString", true);
            catalog.Facet(default);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "FacetVectorNull");

        act = () =>
        {
            var catalog = new Catalog<TBitVector, string>(bitVectorFactory, "SomeString", true);
            catalog.Sort(default, true, true, false);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "SortBitPositionsVectorNull");
    }
}
