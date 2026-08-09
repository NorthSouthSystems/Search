using NorthSouthSystems.BitVectors;

public class T_Engine_Fuzz
{
    private static readonly int[] _randomSeeds = [18873, -76, 5992, 917773, -6320001];

    [Theory]
    [ClassData(typeof(T_BitVectorFactories))]
    public void Full<TBitVector>(IBitVectorFactory<TBitVector> bitVectorFactory)
        where TBitVector : IBitVector<TBitVector>
    {
        foreach (int randomSeed in _randomSeeds)
        {
            var random = new Random(randomSeed);

            foreach (int size in Enumerable.Range(1, bitVectorFactory.WordSize * 10)
                         .Where(i => i % (bitVectorFactory.WordSize - 1) == 1 || random.Next(i / 100) == 0))
            {
                using var engine = new Engine<TBitVector, T_EngineItem, int>(bitVectorFactory, item => item.Id);

                var someIntCatalog = engine.CreateCatalog("SomeInt", item => item.SomeInt);
                var someDateTimeCatalog = engine.CreateCatalog("SomeDateTime", item => item.SomeDateTime);
                var someStringCatalog = engine.CreateCatalog("SomeString", item => item.SomeString);
                var someTagsCatalog = engine.CreateCatalog<string>("SomeTags", item => item.SomeTags);

                var items = T_EngineItem.CreateItems(random, size,
                    out int someIntMax, out int someDateTimeMax, out int someStringMax, out int someTagsMax, out int someTagsMaxCount);

                // OrderBy the lowest potential cardinality (max represents the potential cardinality) so that we increase our chances of having
                // compressed 1's in a Catalog.
                int minCardinality = new[] { someIntMax, someDateTimeMax, someStringMax, someTagsMax }.Min();

                if (someIntMax == minCardinality)
                    items = items.OrderBy(item => item.SomeInt).ToArray();
                else if (someDateTimeMax == minCardinality)
                    items = items.OrderBy(item => item.SomeDateTime).ToArray();
                else if (someStringMax == minCardinality)
                    items = items.OrderBy(item => item.SomeString).ToArray();
                else if (someTagsMax == minCardinality)
                    items = items.OrderBy(item => item.SomeTags.Min()).ToArray();

                int enumerableAddCount = random.Next(items.Length);

                engine.Add(items.Take(enumerableAddCount));

                foreach (var item in items.Skip(enumerableAddCount))
                    engine.Add(item);

                foreach (int iteration in Enumerable.Range(0, 2))
                {
                    ExecuteAndAssert(random, items, engine.CreateQuery());

                    ExecuteAndAssert(random, items, engine.CreateQuery()
                        .Amongst(items.Take((items.Length / 2) + random.Next(items.Length / 2)).Select(item => item.Id)));

                    ExecuteAndAssert(random, items, engine.CreateQuery()
                        .Filter(engine.CreateRandomFilterExactParameter(someIntCatalog, random, someIntMax)));

                    ExecuteAndAssert(random, items, engine.CreateQuery()
                        .Sort(SortParameter.Create(someIntCatalog, random.Next() % 2 == 0)));

                    ExecuteAndAssert(random, items, engine.CreateQuery()
                        .SortPrimaryKey(random.Next() % 2 == 0));

                    ExecuteAndAssert(random, items, engine.CreateQuery()
                        .Filter(engine.CreateRandomFilterExactParameter(someIntCatalog, random, someIntMax))
                        .Sort(SortParameter.Create(someStringCatalog, random.Next() % 2 == 0)));

                    ExecuteAndAssert(random, items, engine.CreateQuery()
                        .Filter(engine.CreateRandomFilterExactParameter(someIntCatalog, random, someIntMax))
                        .SortPrimaryKey(random.Next() % 2 == 0));

                    ExecuteAndAssert(random, items, engine.CreateQuery()
                        .Filter(engine.CreateRandomFilterEnumerableParameter(someIntCatalog, random, someIntMax))
                        .Sort(SortParameter.Create(engine, "SomeInt", random.Next() % 2 == 0)));

                    ExecuteAndAssert(random, items, engine.CreateQuery()
                        .Filter(engine.CreateRandomFilterRangeParameter(someIntCatalog, random, someIntMax))
                        .Sort(SortParameter.Create(someDateTimeCatalog, random.Next() % 2 == 0)));

                    ExecuteAndAssert(random, items, engine.CreateQuery()
                        .Filter(engine.CreateRandomFilterRangeParameter(someTagsCatalog, random, someTagsMax))
                        .Sort(SortParameter.Create(engine, "SomeTags", random.Next() % 2 == 0)));

                    ExecuteAndAssert(random, items, engine.CreateQuery()
                        .Filter(engine.CreateRandomFilterExactParameter(someStringCatalog, random, someStringMax)
                            && engine.CreateRandomFilterRangeParameter(someIntCatalog, random, someIntMax))
                        .Sort(SortParameter.Create(someDateTimeCatalog, random.Next() % 2 == 0),
                            SortParameter.Create(someIntCatalog, random.Next() % 2 == 0))
                        .SortPrimaryKey(random.Next() % 2 == 0));

                    ExecuteAndAssert(random, items, engine.CreateQuery()
                        .Filter(engine.CreateRandomFilterEnumerableParameter(someStringCatalog, random, someStringMax)
                            && engine.CreateRandomFilterRangeParameter(someDateTimeCatalog, random, someDateTimeMax))
                        .Sort(SortParameter.Create(engine, "SomeInt", random.Next() % 2 == 0),
                            SortParameter.Create(engine, "SomeDateTime", random.Next() % 2 == 0))
                        .SortPrimaryKey(random.Next() % 2 == 0));

                    ExecuteAndAssert(random, items, engine.CreateQuery()
                        .Amongst(items.Take((items.Length / 2) + random.Next(items.Length / 2)).Select(item => item.Id))
                        .Filter(engine.CreateRandomFilterRangeParameter(someDateTimeCatalog, random, someDateTimeMax)
                            && engine.CreateRandomFilterRangeParameter(someIntCatalog, random, someIntMax))
                        .Sort(SortParameter.Create(someStringCatalog, random.Next() % 2 == 0),
                            SortParameter.Create(someDateTimeCatalog, random.Next() % 2 == 0))
                        .SortPrimaryKey(random.Next() % 2 == 0));

                    ExecuteAndAssert(random, items, engine.CreateQuery()
                        .Filter(engine.CreateRandomFilterRangeParameter(someIntCatalog, random, someIntMax)
                            && engine.CreateRandomFilterRangeParameter(someTagsCatalog, random, someTagsMax)
                            && engine.CreateRandomFilterRangeParameter(someTagsCatalog, random, someTagsMax))
                        .Sort(SortParameter.Create(engine, "SomeDateTime", random.Next() % 2 == 0),
                            SortParameter.Create(engine, "SomeTags", random.Next() % 2 == 0))
                        .SortPrimaryKey(random.Next() % 2 == 0));

                    if (random.Next() % 2 == 0)
                        items = Update(engine, items, random);

                    if (random.Next() % 2 == 0)
                        items = RemoveReAdd(engine, items, random);

                    if (random.Next() % 2 == 0)
                        engine.Optimize();
                }
            }
        }
    }

    private static void ExecuteAndAssert(Random random, T_EngineItem[] items, Query<int> query)
    {
        query = query
            .FacetAll()
            .WithFacetOptions(random.Next() % 2 == 0, random.Next() % 2 == 0);

        T_EngineAssert.ExecuteAndAssert(items, query, 0, items.Length);
    }

    private static T_EngineItem[] Update<TBitVector>(Engine<TBitVector, T_EngineItem, int> engine, T_EngineItem[] items, Random random)
        where TBitVector : IBitVector<TBitVector>
    {
        var updateItems = items.OrderBy(item => item.GetHashCode())
            .Take(random.Next(items.Length))
            .ToArray();

        int rangeUpdateCount = random.Next(updateItems.Length);

        engine.Update(updateItems.Take(rangeUpdateCount));

        foreach (var item in updateItems.Skip(rangeUpdateCount))
            engine.Update(item);

        return items.Except(updateItems)
            .Concat(updateItems)
            .ToArray();
    }

    private static T_EngineItem[] RemoveReAdd<TBitVector>(Engine<TBitVector, T_EngineItem, int> engine, T_EngineItem[] items, Random random)
        where TBitVector : IBitVector<TBitVector>
    {
        var removeItemsAsc = items.OrderBy(item => item.GetHashCode())
            .Take(random.Next(items.Length / 2));

        var removeItemsDesc = items.OrderByDescending(item => item.GetHashCode())
            .Take(random.Next(items.Length / 2));

        var removeItems = removeItemsAsc.Concat(removeItemsDesc)
            .Distinct()
            .ToArray();

        var reAddItems = new HashSet<T_EngineItem>(removeItems.Where(_ => random.Next() % 2 == 0));

        int rangeRemoveCount = random.Next(removeItems.Length);

        engine.Remove(removeItems.Take(rangeRemoveCount));
        engine.Add(removeItems.Take(rangeRemoveCount).Where(item => reAddItems.Contains(item)));

        foreach (var item in removeItems.Skip(rangeRemoveCount))
        {
            engine.Remove(item);

            if (reAddItems.Contains(item))
                engine.Add(item);
        }

        return items.Except(removeItems)
            .Concat(removeItems.Where(item => reAddItems.Contains(item)))
            .ToArray();
    }
}
