#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;

#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;

#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;

#else
using NorthSouthSystems.BitVectors.WAH;
#endif

public class T_Vector_OrInPlace
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrInPlaceWithCompressibleTrueInput(bool isCompressed)
    {
        var vector = new Vector(false);
        vector[Word.SIZE * 2] = true;

        var compressibleTrue = new Vector(isCompressed);
        compressibleTrue.SetBits(Enumerable.Range(0, Word.SIZE).ToArray(), true);

        vector.OrInPlace(compressibleTrue);

        vector.AssertBitPositions(Enumerable.Range(0, Word.SIZE), [Word.SIZE * 2]);
    }

    [Fact]
    public void Exceptions()
    {
        Action act;

        act = () =>
        {
            var vector = new Vector(false);
            vector.OrInPlace(null);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "OrInPlaceArgumentNull");

        act = () =>
        {
            var vector = new Vector(true);
            var input = new Vector(false);
            vector.OrInPlace(input);
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "OrInPlaceNotSupported");
    }
}
