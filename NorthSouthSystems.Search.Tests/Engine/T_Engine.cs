using NorthSouthSystems.BitVectors;

public class T_Engine
{
    private class SimpleItem
    {
        public int Id;
        public int SomeInt;
    }

    [Theory]
    [ClassData(typeof(T_BitVectorFactories))]
    public void AmongstPrimaryKeyOutOfRange<TBitVector>(IBitVectorFactory<TBitVector> bitVectorFactory)
        where TBitVector : IBitVector<TBitVector>
    {
        using var engine1 = new Engine<TBitVector, SimpleItem, int>(bitVectorFactory, item => item.Id);

        var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

        engine1.Add(new SimpleItem { Id = 43, SomeInt = 0 });

        var query = engine1.CreateQuery();
        query.Amongst([43, 44]);

        var result = query.Execute(0, 10);

        result.TotalCount.Should().Be(1);
        result.PrimaryKeys.Length.Should().Be(1);
        result.PrimaryKeys[0].Should().Be(43);
    }

    [Theory]
    [ClassData(typeof(T_BitVectorFactories))]
    public void Exceptions<TBitVector>(IBitVectorFactory<TBitVector> bitVectorFactory)
        where TBitVector : IBitVector<TBitVector>
    {
        Action act;

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

            engine1.Add(T_EngineItem.CreateItems(id => id, id => DateTime.Now, id => id.ToString(), id => Array.Empty<string>(), 1).Single());

            var catalog2 = engine1.CreateCatalog("SomeString", item => item.SomeString);
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "CreateCatalogNotInitializing");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("Name", item => item.SomeInt);
            var catalog2 = engine1.CreateCatalog("Name", item => item.SomeString);
        };
        act.Should().ThrowExactly<ArgumentException>(because: "CreateCatalogDuplicateName");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, SimpleItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

            engine1.Add(new SimpleItem { Id = 0, SomeInt = 0 });
            engine1.Add(new SimpleItem { Id = 0, SomeInt = 1 });
        };
        act.Should().ThrowExactly<ArgumentException>(because: "AddDuplicatePrimaryKey");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, SimpleItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);
            engine1.Add((SimpleItem[])null);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "AddRangeNull");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, SimpleItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

            engine1.Add(new SimpleItem { Id = 0, SomeInt = 0 });
            engine1.Update(new SimpleItem { Id = 1, SomeInt = 1 });
        };
        act.Should().ThrowExactly<ArgumentException>(because: "UpdateNoPrimaryKey");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, SimpleItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);
            engine1.Update((SimpleItem[])null);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "UpdateRangeNull");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, SimpleItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

            engine1.Add(new SimpleItem { Id = 0, SomeInt = 0 });
            engine1.Remove(new SimpleItem { Id = 1, SomeInt = 1 });
        };
        act.Should().ThrowExactly<ArgumentException>(because: "RemoveNoPrimaryKey");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, SimpleItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);

            engine1.Add(new SimpleItem { Id = 0, SomeInt = 0 });
            engine1.Remove(new SimpleItem { Id = 0, SomeInt = 0 });
            engine1.Add(new SimpleItem { Id = 0, SomeInt = 0 });
        };
        act.Should().NotThrow(because: "RemoveReAddPrimaryKey");

        act = () =>
        {
            using var engine1 = new Engine<TBitVector, SimpleItem, int>(bitVectorFactory, item => item.Id);

            var catalog1 = engine1.CreateCatalog("SomeInt", item => item.SomeInt);
            engine1.Remove((SimpleItem[])null);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "RemoveRangeNull");
    }
}
