#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;

#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;

#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;

#else
using NorthSouthSystems.BitVectors.WAH;
#endif

public class T_Word_Compress
{
    [Fact]
    public void Compressible()
    {
        var word = new Word(Word.ZERO);
        word.IsCompressible.Should().BeTrue();
        word.Compress();
        word.Raw.Should().Be(Word.COMPRESSEDMASK + Word.ONE);
        word.IsCompressed.Should().BeTrue();
        word.FillBit.Should().BeFalse();
        word.FillCount.Should().Be(1);

        word = new Word(Word.COMPRESSIBLEMASK);
        word.IsCompressible.Should().BeTrue();
        word.Compress();
        word.Raw.Should().Be(Word.COMPRESSEDMASK + Word.FILLBITMASK + Word.ONE);
        word.IsCompressed.Should().BeTrue();
        word.FillBit.Should().BeTrue();
        word.FillCount.Should().Be(1);
    }

    [Fact]
    public void NotCompressible()
    {
        foreach (WordRawType wordValue in new[]
#if WORDSIZE64
                     { Word.ONE, Word.FILLBITMASK, Word.COMPRESSIBLEMASK - Word.ONE, Word.COMPRESSIBLEMASK - Word.FILLBITMASK, 0x1234_5678_9ABC_DEFFul, 0x7FED_CBA9_8765_4321ul }
#else
                     { Word.ONE, Word.FILLBITMASK, Word.COMPRESSIBLEMASK - Word.ONE, Word.COMPRESSIBLEMASK - Word.FILLBITMASK, 0x12345678u, 0x7FED_CBA9u }
#endif
                )
        {
            var word = new Word(wordValue);
            word.Compress();
            word.Raw.Should().Be(wordValue);
            word.IsCompressible.Should().BeFalse();
            word.IsCompressed.Should().BeFalse();
        }
    }

    [Fact]
    public void NotCompressibleFullCoverage()
    {
        for (WordRawType i = Word.ONE; i < Word.COMPRESSIBLEMASK; i += T_WordExtensionsForTests.LARGEPRIME32ORFULLCOVERAGE64)
        {
            var word = new Word(i);
            word.Compress();
            word.Raw.Should().Be(i);
            word.IsCompressible.Should().BeFalse();
            word.IsCompressed.Should().BeFalse();
        }
    }

    [Fact]
    public void CompressedFullCoverage()
    {
        for (WordRawType i = Word.COMPRESSEDMASK; i > Word.COMPRESSEDMASK && i <= WordRawType.MaxValue; i += T_WordExtensionsForTests.LARGEPRIME32ORFULLCOVERAGE64)
        {
            var word = new Word(i);
            word.Compress();
            word.Raw.Should().Be(i, because: word.ToString());
            word.IsCompressed.Should().BeTrue(because: word.ToString());
        }
    }

#if POSITIONLISTENABLED
    [Fact]
    public void Pack()
    {
        var word = new Word(true, 1);

        word.IsCompressed.Should().BeTrue();
        word.FillBit.Should().BeTrue();
        word.FillCount.Should().Be(1);
        word.HasPackedWord.Should().BeFalse();

        word.Pack(new Word(Word.ONE));

        word.IsCompressed.Should().BeTrue();
        word.FillBit.Should().BeTrue();
        word.FillCount.Should().Be(1);
        word.HasPackedWord.Should().BeTrue();
        word.PackedPosition.Should().Be((Word.SIZE - 1) - 1);
        word.PackedWord.Raw.Should().Be(Word.ONE);

        word = new Word(true, 1);

        word.IsCompressed.Should().BeTrue();
        word.FillBit.Should().BeTrue();
        word.FillCount.Should().Be(1);
        word.HasPackedWord.Should().BeFalse();

        word.Pack(new Word(Word.ONE << Word.SIZE - 2));

        word.IsCompressed.Should().BeTrue();
        word.FillBit.Should().BeTrue();
        word.FillCount.Should().Be(1);
        word.HasPackedWord.Should().BeTrue();
        word.PackedPosition.Should().Be(0);
        word.PackedWord.Raw.Should().Be(Word.ONE << Word.SIZE - 2);
    }
#endif

#if POSITIONLISTENABLED
    [Fact]
    public void PackExceptions()
    {
        Action act;

        act = () =>
        {
            var word = new Word(Word.ZERO);
            int packedPositions = word.PackedPosition;
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "PackedPositionNotSupported");

        act = () =>
        {
            var word = new Word(Word.ZERO);
            Word packedWord = word.PackedWord;
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "PackedWordNotSupported");

        act = () =>
        {
            var word = new Word(Word.ZERO);
            word.Pack(new Word(Word.ONE));
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "PackNotSupported1");

        act = () =>
        {
            var word = new Word(true, 1);
            word.Pack(new Word(Word.ONE));
        };
        act.Should().NotThrow(because: "PackNotSupported2OK");

        act = () =>
        {
            var word = new Word(true, 1);
            word.Pack(new Word(Word.ONE));
            word.Pack(new Word(Word.ONE));
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "PackNotSupported2");

        act = () =>
        {
            var word = new Word(true, 1);
            word.Pack(new Word(true, 1));
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "PackNotSupported3");

        act = () =>
        {
            var word = new Word(true, 1);
            word.Pack(new Word(Word.ZERO));
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "PackNotSupported4_1");

        act = () =>
        {
            var word = new Word(true, 1);
            word.Pack(new Word(Word.ONE * 3));
        };
        act.Should().ThrowExactly<NotSupportedException>(because: "PackNotSupported4_2");
    }
#endif
}
