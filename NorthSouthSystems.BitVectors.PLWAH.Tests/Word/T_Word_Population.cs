#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;

#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;

#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;

#else
using NorthSouthSystems.BitVectors.WAH;
#endif

public class T_Word_Population
{
    [Fact]
    public void NotCompressed()
    {
        new Word(Word.ZERO).Population.Should().Be(0);
        new Word(Word.COMPRESSIBLEMASK).Population.Should().Be(Word.SIZE - 1);
        new Word(Word.ONE).Population.Should().Be(1);
        new Word(Word.FILLBITMASK).Population.Should().Be(1);
        new Word(Word.COMPRESSIBLEMASK - Word.ONE).Population.Should().Be(Word.SIZE - 2);
        new Word(Word.COMPRESSIBLEMASK - Word.FILLBITMASK).Population.Should().Be(Word.SIZE - 2);

#if WORDSIZE64
        new Word(0x1234_5678_1234_5678ul).Population.Should().Be(26);
        new Word(0x7FED_CBA9_7FED_CBA9ul).Population.Should().Be(44);
#else
        new Word(0x1234_5678u).Population.Should().Be(13);
        new Word(0x7FED_CBA9u).Population.Should().Be(22);
#endif

        for (int i = 0; i < Word.SIZE - 1; i++)
            new Word(Word.ONE << i).Population.Should().Be(1);
    }

    [Fact]
    public void CompressedFillBitFalseNoFill() => CompressedBase(false, 0x00000000);

    [Fact]
    public void CompressedFillBitFalse1Fill() => CompressedBase(false, 0x00000001);

    [Fact]
    public void CompressedFillBitFalseMaxFill() => CompressedBase(false, 0x01FFFFFF);

    [Fact]
    public void CompressedFillBitTrueNoFill() => CompressedBase(true, 0x00000000);

    [Fact]
    public void CompressedFillBitTrue1Fill() => CompressedBase(true, 0x00000001);

    [Fact]
    public void CompressedFillBitTrueMaxFill() => CompressedBase(true, 0x01FFFFFF);

    private void CompressedBase(bool fillBit, int fillCount)
    {
        var word = new Word(fillBit, fillCount);
        word.Population.Should().Be(fillBit ? ((Word.SIZE - 1) * fillCount) : 0);
    }

    [Fact]
    public void CompressedFullCoverage()
    {
        foreach (bool fillBit in new bool[] { false, true })
        {
            // This is only "full coverage" (it's psuedo anyways) when !WORDSIZE64.
            for (int i = 0; i >= 0 && i <= (int)Math.Min(Word.FILLCOUNTMASK, int.MaxValue); i += T_WordExtensionsForTests.LARGEPRIME)
            {
                var word = new Word(fillBit, i);
                word.Population.Should().Be(fillBit ? ((Word.SIZE - 1) * i) : 0, because: word.ToString());
            }
        }
    }

#if POSITIONLISTENABLED
    [Fact]
    public void Packed()
    {
        var word = new Word(false, 1);
        word.Population.Should().Be(0);
        word.Pack(new Word(Word.ONE));
        word.Population.Should().Be(1);

        word = new Word(true, 1);
        word.Population.Should().Be(Word.SIZE - 1);
        word.Pack(new Word(Word.ONE));
        word.Population.Should().Be(Word.SIZE);
    }
#endif
}
