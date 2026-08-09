internal class T_EngineItem
{
    internal static T_EngineItem[] CreateItems(
        Func<int, int> someIntGenerator,
        Func<int, DateTime> someDateTimeGenerator,
        Func<int, string> someStringGenerator,
        Func<int, string[]> someTagGenerator,
        int count) =>
        Enumerable.Range(0, count)
            .Select(id =>
                new T_EngineItem
                {
                    Id = id,
                    SomeInt = someIntGenerator(id),
                    SomeDateTime = someDateTimeGenerator(id),
                    SomeString = someStringGenerator(id),
                    SomeTags = someTagGenerator(id)
                })
            .ToArray();

    internal static T_EngineItem[] CreateItems(Random random, int count,
        out int someIntMax, out int someDateTimeMax, out int someStringMax, out int someTagsMax, out int someTagsMaxCount)
    {
        // Cannot create closures around 'out' parameters.
        int closableSomeIntMax = someIntMax = Math.Max(random.Next(count), 1);
        int closableSomeDateTimeMax = someDateTimeMax = Math.Max(random.Next(count), 1);
        int closableSomeStringMax = someStringMax = Math.Max(random.Next(count), 1);
        int closableSomeTagsMax = someTagsMax = Math.Max(random.Next(count), 1);
        int closableSomeTagsMaxCount = someTagsMaxCount = Math.Max(random.Next(closableSomeTagsMax), 1);

        return CreateItems(
            id => random.Next(closableSomeIntMax),
            id => new DateTime(2011, 1, 1).AddDays(random.Next(closableSomeDateTimeMax)),
            id => random.Next(closableSomeStringMax).ToString(),
            id => Enumerable.Range(0, random.Next(closableSomeTagsMaxCount))
                .Select(i => random.Next(closableSomeTagsMax))
                .Distinct()
                .Select(tag => tag.ToString())
                .ToArray(),
            count);
    }

    internal int Id { get; private set; }
    internal int SomeInt { get; private set; }
    internal DateTime SomeDateTime { get; private set; }
    internal string SomeString { get; private set; }
    internal string[] SomeTags { get; private set; }

    public override string ToString() => Id.ToString();
}
