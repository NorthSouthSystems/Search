using NorthSouthSystems.BitVectors;

public class T_Query
{
    [Theory]
    [ClassData(typeof(T_BitVectorFactories))]
    public void Exceptions<TBitVector>(IBitVectorFactory<TBitVector> bitVectorFactory)
        where TBitVector : IBitVector<TBitVector>
    {
        Action act;

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);
            using var engine2 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);
            var catalog2 = engine2.CreateCatalog("SomeInt", item => item.SomeInt);

            engine1.CreateQuery().Filter(FilterParameter.Create(catalog2, 1));
        };
        act.Should().ThrowExactly<ArgumentException>(because: "EngineMismatch");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

            engine1.CreateQuery().Filter(FilterParameter.Create((ICatalogHandle<int>)null, 1));
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "AddFilterExactNull");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

            engine1.CreateQuery().Filter(FilterParameter.Create((ICatalogHandle<int>)null, new[] { 1, 2 }));
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "AddFilterEnumerableNull");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

            engine1.CreateQuery().Filter(FilterParameter.Create((ICatalogHandle<int>)null, 1, 3));
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "AddFilterRangeNull");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

            engine1.CreateQuery().Sort(SortParameter.Create((ICatalogHandle<int>)null, true));
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "AddSortNull");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

            var query = engine1.CreateQuery()
                .Filter(FilterParameter.Create(catalog1, 1))
                .SortPrimaryKey(true)
                .Sort(SortParameter.Create(catalog1, true));
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "PrimaryKeySortExists");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

            var query = engine1.CreateQuery()
                .Filter(FilterParameter.Create(catalog1, 1))
                .Sort(SortParameter.Create(catalog1, true),
                    SortParameter.Create(catalog1, true));
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "DuplicateSort");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

            engine1.CreateQuery().Facet(FacetParameter.Create((ICatalogHandle<int>)null));
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "AddFacetNull");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);
            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);
            var catalog2 = engine1.CreateCatalog("SomeString", item => item.SomeString);

            var query = engine1.CreateQuery()
                .Filter(FilterParameter.Create(catalog1, 1))
                .Facet(FacetParameter.Create(catalog2),
                    FacetParameter.Create(catalog2));
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "DuplicateFacet");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

            var query = engine1.CreateQuery();

            query.Filter(FilterParameter.Create(catalog1, 1))
                .Execute(0, 1);

            query.Execute(0, 1);
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "QueryAlreadyExecuted");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);
            var catalog2 = engine1.CreateCatalog("SomeString", item => item.SomeString);

            var query = engine1.CreateQuery()
                .Filter(FilterParameter.Create(catalog1, 1))
                .Sort(SortParameter.Create(catalog2, true))
                .SortPrimaryKey(true)
                .Facet(FacetParameter.Create(catalog2))
                .Execute(0, 1);
        };
        act.Should().NotThrow(because: "SanityNoExceptions");
    }
}
