#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;

#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;

#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;

#else
using NorthSouthSystems.BitVectors.WAH;
#endif

public class T_Vector_OrOutOfPlace
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
    public void OrOutOfPlace(bool aIsCompressed, bool bIsCompressed, bool cIsCompressed)
    {
        int[] bitPositionsA = [0, 12, 16, 22, 34, 55, 110];
        int[] bitPositionsB = [0, 11, 16, 23, 34, 54, 110, 120];
        int[] bitPositionsC = [5, 10, 15, 20, 34, 53];

        var vectorA = new Vector(aIsCompressed);
        vectorA.SetBits(bitPositionsA, true);

        var vectorB = new Vector(bIsCompressed);
        vectorB.SetBits(bitPositionsB, true);

        var vectorC = new Vector(cIsCompressed);
        vectorC.SetBits(bitPositionsC, true);

        Vector.OrOutOfPlace(vectorA, vectorB).AssertBitPositions(bitPositionsA, bitPositionsB);
        Vector.OrOutOfPlace(vectorA, vectorB, vectorC).AssertBitPositions(bitPositionsA, bitPositionsB, bitPositionsC);
    }

    [Fact]
    public void Exceptions()
    {
        Action act;

        act = () =>
        {
            Vector[] vectors = null;
            Vector.OrOutOfPlace(vectors);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "OrOutOfPlaceArgumentNull1");

        act = () =>
        {
            var vector = new Vector(false);
            Vector.OrOutOfPlace(vector, null);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "OrOutOfPlaceArgumentNull2");

        act = () =>
        {
            var vector = Vector.OrOutOfPlace();
        };
        act.Should().ThrowExactly<ArgumentOutOfRangeException>(because: "OrOutOfPlaceArgumentOutOfRange1");

        act = () =>
        {
            var vector1 = new Vector(false);
            var vector = Vector.OrOutOfPlace(vector1);
        };
        act.Should().ThrowExactly<ArgumentOutOfRangeException>(because: "OrOutOfPlaceArgumentOutOfRange2");
    }
}
