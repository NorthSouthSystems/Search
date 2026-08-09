#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;

#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;

#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;

#else
using NorthSouthSystems.BitVectors.WAH;
#endif

public class T_Vector_AndOutOfPlace
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void AndOutOfPlaceRandom(bool leftIsCompressed, bool rightIsCompressed, bool resultIsCompressed)
    {
        const int randomSeed = 22;

        T_VectorHelpersForRandomTests.LogicOutOfPlaceBase(randomSeed, (Word.SIZE - 1) * T_WordExtensionsForTests.WORDCOUNTFORRANDOMTESTS + 1,
            leftIsCompressed, rightIsCompressed,
            (left, right) => left.AndOutOfPlace(right, resultIsCompressed),
            Enumerable.Intersect);
    }
}
