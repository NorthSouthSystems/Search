#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;

#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;

#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;

#else
using NorthSouthSystems.BitVectors.WAH;
#endif

public class T_Vector_GetSetBits
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Full(bool isCompressed)
    {
        var vector = new Vector(isCompressed);
        int[] bitPositions = vector.SetBitsRandom(999, 100, true);
        vector[2000] = false;
        vector.AssertBitPositions(bitPositions);
    }

    [Fact]
    public void Exceptions()
    {
        Action act;

        act = () =>
        {
            var vector = new Vector(false);
            vector[-1] = true;
        };
        act.Should().ThrowExactly<ArgumentOutOfRangeException>(because: "IndexArgumentOutOfRange1");

        act = () =>
        {
            var vector = new Vector(false);
            bool value = vector[-1];
        };
        act.Should().ThrowExactly<ArgumentOutOfRangeException>(because: "IndexArgumentOutOfRange2");

        act = () =>
        {
            var vector = new Vector(true);
            vector[(Word.SIZE - 1) - 1] = true;
            vector[((Word.SIZE - 1) * 2) - 1] = true;
        };
        act.Should().NotThrow(because: "SetBitSupportedForwardOnly");

        act = () =>
        {
            var vector = new Vector(true);
            vector[(Word.SIZE - 1) - 1] = true;
            vector[(Word.SIZE - 1)] = true;
            vector[(Word.SIZE - 1) - 1] = true;
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "SetBitNotSupportedForwardOnly");

        act = () =>
        {
            var vector = new Vector(true);
            vector.Bits.ToArray();
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "GetBitsCompressedNotSupported");
    }

#if !WORDSIZE64
    [Fact]
    public void ExceptionsFillCountOverflow()
    {
        // See comment above Vector.ThrowIfFillCountOverflow.

#if POSITIONLISTENABLED
        Action act;

        act = () =>
        {
            var vector = new Vector(true);
            vector[(int)((Word.FILLCOUNTMASK + 1) * (Word.SIZE - 1)) - 1] = true;
            vector.AssertWordCounts(2, (int)Word.FILLCOUNTMASK + 1);
            vector.GetWordLogical((int)Word.FILLCOUNTMASK - 1).GetBitPositions(true).Should().Equal([]);
            vector.GetWordLogical((int)Word.FILLCOUNTMASK).GetBitPositions(true).Should().Equal(30);
        };
        act.Should().NotThrow();

        act = () =>
        {
            var vector = new Vector(true);
            vector[(int)((Word.FILLCOUNTMASK + 1) * (Word.SIZE - 1))] = true;
        };
        act.Should().ThrowExactly<NotSupportedException>();
#else
        // CS0220 - The operation overflows at compile time in checked mode
        /*
        Action act;
        WordRawType bitPosition;

        act = () => bitPosition = (Word.FILLCOUNTMASK + 1) * (Word.SIZE - 1);
        act.Should().ThrowExactly<OverflowException>();
        */
#endif
    }
#endif
}
