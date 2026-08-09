using NorthSouthSystems.BitVectors;
using NorthSouthSystems.StackExchange;

public abstract class B_EngineBase<TBitVector>
    where TBitVector : IBitVector<TBitVector>
{
    private static readonly int[] _powersOfTen = [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000];

    protected Engine<TBitVector, Post, int> ConstructEngine(IBitVectorFactory<TBitVector> bitVectorFactory)
    {
        Engine = new(bitVectorFactory, post => post.Id);

        PostTypeCatalog = Engine.CreateCatalog(nameof(Post.PostTypeId), post => post.PostTypeId);
        CreationDateCatalog = Engine.CreateCatalog(nameof(Post.CreationDate), post => YearMonth(post.CreationDate));
        LastActivityDateCatalog = Engine.CreateCatalog(nameof(Post.LastActivityDate), post => YearMonth(post.LastActivityDate));
        ScoreCatalog = Engine.CreateCatalog(nameof(Post.Score), post => post.Score);
        ViewCountCatalog = Engine.CreateCatalog(nameof(Post.ViewCount), post => OneSigFig(post.ViewCount));
        OwnerUserIdCatalog = Engine.CreateCatalog(nameof(Post.OwnerUserId), post => post.OwnerUserId);
        TagsCatalog = Engine.CreateCatalog(nameof(Post.Tags), post => post.Tags);
        AnswerCountCatalog = Engine.CreateCatalog(nameof(Post.AnswerCount), post => OneSigFig(post.AnswerCount));
        CommentCountCatalog = Engine.CreateCatalog(nameof(Post.CommentCount), post => OneSigFig(post.CommentCount));
        FavoriteCountCatalog = Engine.CreateCatalog(nameof(Post.FavoriteCount), post => OneSigFig(post.FavoriteCount));

        return Engine;

        DateTime YearMonth(DateTime date) => new(date.Year, date.Month, 1);

        int OneSigFig(int count)
        {
            for (int i = 1; i < _powersOfTen.Length; i++)
                if (count < _powersOfTen[i])
                    return FloorToFactor(_powersOfTen[i - 1]);

            return FloorToFactor(_powersOfTen.Last());

            int FloorToFactor(int factor) => (count / factor) * factor;
        }
    }

    public Engine<TBitVector, Post, int> Engine { get; private set; }

    public ICatalogHandle<byte> PostTypeCatalog { get; private set; }
    public ICatalogHandle<DateTime> CreationDateCatalog { get; private set; }
    public ICatalogHandle<DateTime> LastActivityDateCatalog { get; private set; }
    public ICatalogHandle<int> ScoreCatalog { get; private set; }
    public ICatalogHandle<int> ViewCountCatalog { get; private set; }
    public ICatalogHandle<int> OwnerUserIdCatalog { get; private set; }
    public ICatalogHandle<string> TagsCatalog { get; private set; }
    public ICatalogHandle<int> AnswerCountCatalog { get; private set; }
    public ICatalogHandle<int> CommentCountCatalog { get; private set; }
    public ICatalogHandle<int> FavoriteCountCatalog { get; private set; }
}
