#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;

#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;

#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;

#else
using NorthSouthSystems.BitVectors.WAH;
#endif

public class T_Word_IndexersAndBits
{
    [Fact]
    public void GetSetPositionsSimple()
    {
        var word = new Word();

        for (int i = 0; i < Word.SIZE - 1; i++)
        {
            word[i] = true;

            for (int j = 0; j < Word.SIZE - 1; j++)
                word[j].Should().Be(i == j);

            word[i] = false;

            for (int j = 0; j < Word.SIZE - 1; j++)
                word[j].Should().BeFalse();
        }
    }

    private static readonly int[] BitPositions = new[] { 0, 3, 8, 12, 19, 24, 30, 31, 32, 33, 39, 45, 48, 55, 61, 62 }
        .Where(i => i < Word.SIZE - 1)
        .ToArray();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    private void BitsSimple(bool value)
    {
        var word = new Word();

        for (int i = 0; i < Word.SIZE - 1; i++)
            word[i] = BitPositions.Contains(i) ? value : !value;

        bool[] bits = word.Bits;

        bits.Count(bit => value ? bit : !bit).Should().Be(BitPositions.Length);

        for (int i = 0; i < Word.SIZE - 1; i++)
            (value ? bits[i] : !bits[i]).Should().Be(BitPositions.Contains(i));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    private void GetBitPositionsSimple(bool value)
    {
        var word = new Word();

        for (int i = 0; i < Word.SIZE - 1; i++)
            word[i] = BitPositions.Contains(i) ? value : !value;

        int[] getBitPositions = word.GetBitPositions(value);

        getBitPositions.Length.Should().Be(BitPositions.Length);

        for (int i = 0; i < BitPositions.Length; i++)
            getBitPositions[i].Should().Be(BitPositions[i]);
    }

    [Fact]
    public void Exceptions()
    {
        Action act;

        act = () =>
        {
            var word = new Word(true, 1);
            bool bit = word[0];
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "ComputeIndexerMaskNotSupported");

        act = () =>
        {
            var word = new Word(Word.ZERO);
            bool bit = word[-1];
        };
        act.Should().ThrowExactly<ArgumentOutOfRangeException>(because: "ComputeIndexerMaskArgumentOutOfRange1");

        act = () =>
        {
            var word = new Word(Word.ZERO);
            bool bit = word[Word.SIZE - 1];
        };
        act.Should().ThrowExactly<ArgumentOutOfRangeException>(because: "ComputeIndexerMaskArgumentOutOfRange2");

        act = () =>
        {
            var word = new Word(true, 1);
            bool[] bits = word.Bits;
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "BitsNotSupported");

        act = () =>
        {
            var word = new Word(true, 1);
            int[] bitPositions = word.GetBitPositions(true);
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "GetBitPositionsNotSupported");
    }
}
