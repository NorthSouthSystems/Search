#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;

#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;

#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;

#else
using NorthSouthSystems.BitVectors.WAH;
#endif

public class T_Vector_AndInPlace
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AndInPlaceRandom(bool isCompressed)
    {
        const int randomSeed = 22;

        T_VectorHelpersForRandomTests.LogicInPlaceBase(randomSeed, (Word.SIZE - 1) * T_WordExtensionsForTests.WORDCOUNTFORRANDOMTESTS + 1,
            isCompressed,
            (left, right) => left.AndInPlace(right),
            Enumerable.Intersect);
    }

    [Fact]
    public void Exceptions()
    {
        Action act;

        act = () =>
        {
            var vector = new Vector(false);
            vector.AndInPlace(null);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "AndInPlaceArgumentNull");

        act = () =>
        {
            var vector = new Vector(true);
            var input = new Vector(false);
            vector.AndInPlace(input);
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "AndInPlaceNotSupported");
    }
}
