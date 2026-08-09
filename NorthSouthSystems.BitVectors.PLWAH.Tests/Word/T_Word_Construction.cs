#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;

#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;

#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;

#else
using NorthSouthSystems.BitVectors.WAH;
#endif

public class T_Word_Construction
{
    [Fact]
    public void Bounds()
    {
        var word = new Word(Word.ZERO);
        word = new Word(Word.COMPRESSEDMASK - Word.ONE);
    }

    [Fact]
    public void Compressed()
    {
        var word = new Word(false, 0);
        word.FillBit.Should().BeFalse();
        word.FillCount.Should().Be(0);

        word = new Word(false, 1);
        word.FillBit.Should().BeFalse();
        word.FillCount.Should().Be(1);

        word = new Word(false, 22);
        word.FillBit.Should().BeFalse();
        word.FillCount.Should().Be(22);

        word = new Word(true, 0);
        word.FillBit.Should().BeTrue();
        word.FillCount.Should().Be(0);

        word = new Word(true, 1);
        word.FillBit.Should().BeTrue();
        word.FillCount.Should().Be(1);

        word = new Word(true, 22);
        word.FillBit.Should().BeTrue();
        word.FillCount.Should().Be(22);
    }

    [Fact]
    public void Exceptions()
    {
        Action act;

        act = () => new Word(Word.COMPRESSEDMASK);
        act.Should().ThrowExactly<ArgumentOutOfRangeException>(because: "RawArgumentOutOfRange");

        act = () => new Word(true, -1);
        act.Should().ThrowExactly<ArgumentOutOfRangeException>(because: "FillCountOutOfRange1");

#if !WORDSIZE64
        act = () => new Word(true, (int)(Word.FILLCOUNTMASK + Word.ONE));
        act.Should().ThrowExactly<ArgumentOutOfRangeException>(because: "FillCountOutOfRange2");
#endif
    }
}
