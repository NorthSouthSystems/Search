using NorthSouthSystems.BitVectors;
using NorthSouthSystems.StackExchange;

[MemoryDiagnoser]
[GenericTypeArguments(typeof(NorthSouthSystems.BitVectors.PLWAH.PLWAHVectorFactory), typeof(NorthSouthSystems.BitVectors.PLWAH.Vector))]
[GenericTypeArguments(typeof(NorthSouthSystems.BitVectors.PLWAH64.PLWAH64VectorFactory), typeof(NorthSouthSystems.BitVectors.PLWAH64.Vector))]
[GenericTypeArguments(typeof(NorthSouthSystems.BitVectors.WAH.WAHVectorFactory), typeof(NorthSouthSystems.BitVectors.WAH.Vector))]
[GenericTypeArguments(typeof(NorthSouthSystems.BitVectors.WAH64.WAH64VectorFactory), typeof(NorthSouthSystems.BitVectors.WAH64.Vector))]
public class B_Engine_Query<TBitVectorFactory, TBitVector> : B_EngineBase<TBitVector>
    where TBitVectorFactory : IBitVectorFactory<TBitVector>
    where TBitVector : IBitVector<TBitVector>
{
    [GlobalSetup]
    public void GlobalSetup()
    {
        var bitVectorFactory = Activator.CreateInstance<TBitVectorFactory>();

        ConstructEngine(bitVectorFactory);

        var posts = new StackExchangeSiteSerializer(Program.StackExchangeDirectory, Program.StackExchangeSite)
            .DeserializeMemoryPack<Post>();

        Engine.Add(posts);
    }

    [GlobalCleanup]
    public void GlobalCleanup() => Engine?.Dispose();

    [Benchmark]
    public void Filter() => Filter(Engine.CreateQuery()).Execute(0, 100);

    [Benchmark]
    public void Sort() => Sort(Engine.CreateQuery()).Execute(0, 100);

    [Benchmark]
    public void Facet() => Facet(Engine.CreateQuery()).Execute(0, 100);

    [Benchmark]
    public void Full() => Facet(Sort(Filter(Engine.CreateQuery()))).Execute(0, 100);

    private Query<int> Filter(Query<int> query)
    {
        const int jonSkeetUserId = 22656;

        return query.Filter(FilterParameter.Create(PostTypeCatalog, (byte)1)
            && FilterParameter.Create(OwnerUserIdCatalog, jonSkeetUserId));
    }

    private Query<int> Sort(Query<int> query) =>
        query.Sort(SortParameter.Create(CreationDateCatalog, false));

    private Query<int> Facet(Query<int> query) =>
        query.Facet(FacetParameter.Create(PostTypeCatalog),
            FacetParameter.Create(CreationDateCatalog),
            FacetParameter.Create(LastActivityDateCatalog),
            FacetParameter.Create(ViewCountCatalog),
            FacetParameter.Create(TagsCatalog),
            FacetParameter.Create(AnswerCountCatalog),
            FacetParameter.Create(CommentCountCatalog),
            FacetParameter.Create(FavoriteCountCatalog));
}
