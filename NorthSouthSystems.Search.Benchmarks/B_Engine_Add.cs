using NorthSouthSystems.BitVectors;
using NorthSouthSystems.StackExchange;

[MemoryDiagnoser]
[GenericTypeArguments(typeof(NorthSouthSystems.BitVectors.PLWAH.PLWAHVectorFactory), typeof(NorthSouthSystems.BitVectors.PLWAH.Vector))]
[GenericTypeArguments(typeof(NorthSouthSystems.BitVectors.PLWAH64.PLWAH64VectorFactory), typeof(NorthSouthSystems.BitVectors.PLWAH64.Vector))]
[GenericTypeArguments(typeof(NorthSouthSystems.BitVectors.WAH.WAHVectorFactory), typeof(NorthSouthSystems.BitVectors.WAH.Vector))]
[GenericTypeArguments(typeof(NorthSouthSystems.BitVectors.WAH64.WAH64VectorFactory), typeof(NorthSouthSystems.BitVectors.WAH64.Vector))]
public class B_Engine_Add<TBitVectorFactory, TBitVector> : B_EngineBase<TBitVector>
    where TBitVectorFactory : IBitVectorFactory<TBitVector>
    where TBitVector : IBitVector<TBitVector>
{
    [GlobalSetup]
    public void GlobalSetup()
    {
        _bitVectorFactory = Activator.CreateInstance<TBitVectorFactory>();

        _posts = new StackExchangeSiteSerializer(Program.StackExchangeDirectory, Program.StackExchangeSite)
            .DeserializeMemoryPackParallel<Post>()
            .Where(p => p.CreationDate.Year < 2011)
            .ToList();
    }

    private TBitVectorFactory _bitVectorFactory;
    private List<Post> _posts;

    [Benchmark]
    public void Add()
    {
        using var engine = ConstructEngine(_bitVectorFactory);

        engine.Add(_posts);
    }
}
