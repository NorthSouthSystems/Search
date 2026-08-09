#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;
#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;

#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;
#else
using NorthSouthSystems.BitVectors.WAH;
#endif

public class T_Word_Constants
{
    [Fact]
    public void Constants()
    {
#if WORDSIZE64
        Word.SIZE.Should().Be(64);

        Word.ZERO.Should().Be(0ul);
        Word.ONE.Should().Be(1ul);

        Word.FIRSTBITMASK.Should().Be(0x4000_0000_0000_0000ul);

        Word.COMPRESSIBLEMASK.Should().Be(0x7FFF_FFFF_FFFF_FFFFul);
        Word.COMPRESSEDMASK.Should().Be(0x8000_0000_0000_0000ul);
        Word.FILLBITMASK.Should().Be(0x4000_0000_0000_0000ul);
        Word.FILLCOUNTMASK.Should().Be(0x01FF_FFFF_FFFF_FFFFul);

        (Word.COMPRESSEDMASK + Word.FILLBITMASK + 1).Should().Be(0xC000_0000_0000_0001ul);
        (Word.COMPRESSEDMASK + 1).Should().Be(0x8000_0000_0000_0001ul);

#if POSITIONLISTENABLED
        Word.PACKEDPOSITIONMASK.Should().Be(0x3E00_0000_0000_0000ul);
#endif
#else
        Word.SIZE.Should().Be(32);

        Word.ZERO.Should().Be(0u);
        Word.ONE.Should().Be(1u);

        Word.FIRSTBITPOSITIONMASK.Should().Be(0x4000_0000u);

        Word.COMPRESSIBLEMASK.Should().Be(0x7FFF_FFFFu);
        Word.COMPRESSEDMASK.Should().Be(0x8000_0000u);
        Word.FILLBITMASK.Should().Be(0x4000_0000u);
        Word.FILLCOUNTMASK.Should().Be(0x01FF_FFFFu);

        (Word.COMPRESSEDMASK + Word.FILLBITMASK + 1).Should().Be(0xC000_0001u);
        (Word.COMPRESSEDMASK + 1).Should().Be(0x8000_0001u);

#if POSITIONLISTENABLED
        Word.PACKEDPOSITIONMASK.Should().Be(0x3E00_0000u);
#endif
#endif
    }
}
