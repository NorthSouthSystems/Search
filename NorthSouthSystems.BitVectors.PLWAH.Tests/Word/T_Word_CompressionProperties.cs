#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;

#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;

#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;

#else
using NorthSouthSystems.BitVectors.WAH;
#endif

public class T_Word_CompressionProperties
{
    [Fact]
    public void IsCompressibleTrue()
    {
        var word = new Word(Word.ZERO);
        word.IsCompressible.Should().BeTrue();
        word.CompressibleFillBit.Should().BeFalse();
        word.IsCompressed.Should().BeFalse();

        word = new Word(Word.COMPRESSIBLEMASK);
        word.IsCompressible.Should().BeTrue();
        word.CompressibleFillBit.Should().BeTrue();
        word.IsCompressed.Should().BeFalse();
    }

    [Fact]
    public void IsCompressibleFalse()
    {
        foreach (WordRawType u in new[]
#if WORDSIZE64
                     { Word.ONE, Word.FILLBITMASK, Word.COMPRESSIBLEMASK - Word.ONE, Word.COMPRESSIBLEMASK - Word.FILLBITMASK, 0x1234_5678_9ABC_DEFFul, 0x7FED_CBA9_8765_4321ul }
#else
                     { Word.ONE, Word.FILLBITMASK, Word.COMPRESSIBLEMASK - Word.ONE, Word.COMPRESSIBLEMASK - Word.FILLBITMASK, 0x12345678u, 0x7FED_CBA9u }
#endif
                )
        {
            var word = new Word(u);
            word.IsCompressible.Should().BeFalse();
            word.IsCompressed.Should().BeFalse();
        }
    }

    [Fact]
    public void IsCompressibleFalseFullCoverage()
    {
        for (WordRawType i = Word.ONE; i < Word.COMPRESSIBLEMASK; i += T_WordExtensionsForTests.LARGEPRIME32ORFULLCOVERAGE64)
        {
            var word = new Word(i);
            word.IsCompressible.Should().BeFalse();
            word.IsCompressed.Should().BeFalse();
        }
    }

    [Fact]
    public void CompressedFillBitFalseNoFill() => CompressedBase(false, 0x00000000, Word.COMPRESSEDMASK);

    [Fact]
    public void CompressedFillBitFalse1Fill() => CompressedBase(false, 0x00000001, Word.COMPRESSEDMASK + 1);

    [Fact]
    public void CompressedFillBitFalseMaxFill() => CompressedBase(false, 0x01FFFFFF, Word.COMPRESSEDMASK + 0x01FFFFFF);

    [Fact]
    public void CompressedFillBitTrueNoFill() => CompressedBase(true, 0x00000000, Word.COMPRESSEDMASK + Word.FILLBITMASK);

    [Fact]
    public void CompressedFillBitTrue1Fill() => CompressedBase(true, 0x00000001, Word.COMPRESSEDMASK + Word.FILLBITMASK + 1);

    [Fact]
    public void CompressedFillBitTrueMaxFill() => CompressedBase(true, 0x01FFFFFF, Word.COMPRESSEDMASK + Word.FILLBITMASK + 0x01FFFFFF);

    private void CompressedBase(bool fillBit, int fillCount, WordRawType wordValue)
    {
        var word = new Word(fillBit, fillCount);
        word.Raw.Should().Be(wordValue);
        word.IsCompressed.Should().BeTrue();
        word.FillBit.Should().Be(fillBit);
        word.FillCount.Should().Be(fillCount);
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
                word.Raw.Should().Be(Word.COMPRESSEDMASK + (fillBit ? Word.FILLBITMASK : Word.ZERO) + (WordRawType)i);
                word.IsCompressed.Should().BeTrue(because: word.ToString());
                word.FillBit.Should().Be(fillBit, because: word.ToString());
                word.FillCount.Should().Be(i, because: word.ToString());
            }
        }
    }
}
