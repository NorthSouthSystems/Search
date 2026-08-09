#if WORDSIZE64
global using WordRawType = ulong;

#else
global using WordRawType = uint;
#endif

#if POSITIONLISTENABLED && WORDSIZE64
namespace NorthSouthSystems.BitVectors.PLWAH64;
#elif POSITIONLISTENABLED
namespace NorthSouthSystems.BitVectors.PLWAH;
#elif WORDSIZE64
namespace NorthSouthSystems.BitVectors.WAH64;
#else
namespace NorthSouthSystems.BitVectors.WAH;
#endif

using System.Numerics;

internal struct Word
{
#if WORDSIZE64
    public const int SIZE = 64;
#else
    public const int SIZE = 32;
#endif

#if POSITIONLISTENABLED && WORDSIZE64
    private const int PACKEDPOSITIONSIZE = 6;
#elif POSITIONLISTENABLED
    private const int PACKEDPOSITIONSIZE = 5;
#else
    private const int PACKEDPOSITIONSIZE = 0;
#endif

    internal const WordRawType ZERO = 0;
    internal const WordRawType ONE = 1;

    public WordRawType Raw;

    #region Construction

    public Word(WordRawType raw)
    {
        if (raw >= COMPRESSEDMASK)
            throw new ArgumentOutOfRangeException(nameof(raw), raw, FormattableString.Invariant($"Must be < COMPRESSEDMASK : 0x{COMPRESSEDMASK:X}."));

        Raw = raw;
    }

    public Word(bool fillBit, int fillCount)
    {
        if (fillCount < 0)
            throw new ArgumentOutOfRangeException(nameof(fillCount), fillCount, "Must be >= 0.");

        if ((WordRawType)fillCount > FILLCOUNTMASK)
            throw new ArgumentOutOfRangeException(nameof(fillCount), fillCount, FormattableString.Invariant($"Must be <= FILLCOUNTMASK : 0x{FILLCOUNTMASK:X}."));

        var fillCountTyped = (WordRawType)fillCount;

        Raw = COMPRESSEDMASK | (fillBit ? FILLBITMASK : ZERO) | fillCountTyped;
    }

    #endregion

    #region Indexers

    internal const WordRawType FIRSTBITPOSITIONMASK = ONE << (SIZE - 2);

    public bool this[int position]
    {
        readonly get => (Raw & ComputeIndexerMask(position)) > ZERO;
        set
        {
            WordRawType mask = ComputeIndexerMask(position);

            if (value)
                Raw |= mask;
            else
                Raw &= (~mask);
        }
    }

    private readonly WordRawType ComputeIndexerMask(int position)
    {
        if (IsCompressed)
            throw new NotSupportedException("Not supported for compressed Words.");

        if (position < 0)
            throw new ArgumentOutOfRangeException(nameof(position), position, "Must be >= 0.");

        if (position >= SIZE - 1)
            throw new ArgumentOutOfRangeException(nameof(position), position, FormattableString.Invariant($"Must be < SIZE - 1 : {SIZE - 1}."));

        return FIRSTBITPOSITIONMASK >> position;
    }

    public readonly bool[] Bits
    {
        get
        {
            if (IsCompressed)
                throw new NotSupportedException("Not supported for compressed Words.");

            bool[] bits = new bool[SIZE - 1];
            int current = 0;

            WordRawType mask = FIRSTBITPOSITIONMASK;

            for (int i = 0; i < SIZE - 1; i++)
            {
                bits[current++] = (Raw & mask) > ZERO;
                mask >>= 1;
            }

            return bits;
        }
    }

    public readonly bool HasBitPositions(bool value)
    {
        if (IsCompressed)
            throw new NotSupportedException("Not supported for compressed Words.");

        return (value && Raw != ZERO) || (!value && Raw != COMPRESSIBLEMASK);
    }

    public readonly int[] GetBitPositions(bool value)
    {
        if (IsCompressed)
            throw new NotSupportedException("Not supported for compressed Words.");

        if ((value && Raw == ZERO) || (!value && Raw == COMPRESSIBLEMASK))
            return _emptyBitPositions;

        int count = value ? Population : SIZE - 1 - Population;
        int[] bitPositions = new int[count];
        int current = 0;

        WordRawType mask = FIRSTBITPOSITIONMASK;

        for (int i = 0; i < SIZE - 1; i++)
        {
            if ((Raw & mask) > ZERO == value)
                bitPositions[current++] = i;

            mask >>= 1;
        }

        return bitPositions;
    }

    private static readonly int[] _emptyBitPositions = [];

    #endregion

    #region Compression

    internal const WordRawType COMPRESSIBLEMASK = WordRawType.MaxValue >> 1;
    internal const WordRawType COMPRESSEDMASK = ONE << (SIZE - 1);
    internal const WordRawType FILLBITMASK = ONE << (SIZE - 2);
    internal const WordRawType FILLCOUNTMASK = WordRawType.MaxValue >> (2 + PACKEDPOSITIONSIZE);

    public readonly bool IsCompressible => Raw == ZERO || Raw == COMPRESSIBLEMASK;
    public readonly bool CompressibleFillBit => Raw != ZERO;
    public readonly bool IsCompressed => Raw >= COMPRESSEDMASK;
    public readonly bool FillBit => (Raw & FILLBITMASK) > ZERO;
    public readonly int FillCount => (int)(Raw & FILLCOUNTMASK);

    public void Compress()
    {
        if (IsCompressible)
            Raw = CompressibleFillBit ? (COMPRESSEDMASK + FILLBITMASK + 1) : (COMPRESSEDMASK + 1);
    }

    #endregion

    #region Packing

#if POSITIONLISTENABLED
    internal const WordRawType PACKEDPOSITIONMASK = ((WordRawType)(SIZE - 1)) << (SIZE - 2 - PACKEDPOSITIONSIZE);

    public readonly bool HasPackedWord => IsCompressed && (Raw & PACKEDPOSITIONMASK) > ZERO;

    /// <summary>
    /// Position is relative to the global bit position.  More specifically, it ignores the most significant bit which is
    /// reserved for compression.  E.g. for a given word's most significant byte, X012 3456.  This follows the same convention
    /// as every Word and Vector function.  There are SIZE - 1 bit positions in a uncompressed Word using this convention,
    /// and therefore bit positions go from 0 to 30.  Because 5 bits represent the PACKEDPOSITIONMASK, values 0 through 31 can be
    /// represented.  Because 00000 = 0 should mean the absence of a PackedWord, we take the value stored under the
    /// PACKEDPOSITIONMASK and subtract 1 to get the real PackedPosition. This function will then return -1 should a PackedWord
    /// not be present. This offset is reflected in the PackedWord property and also the Pack function.
    /// </summary>
    public readonly int PackedPosition
    {
        get
        {
            if (!HasPackedWord)
                throw new NotSupportedException("Cannot retrieve the PackedPosition for a Word that does not contain a Packed Word.");

            return (int)((Raw & PACKEDPOSITIONMASK) >> (SIZE - 2 - PACKEDPOSITIONSIZE)) - 1;
        }
    }

    public readonly Word PackedWord
    {
        get
        {
            if (!HasPackedWord)
                throw new NotSupportedException("Cannot retrieve the PackedWord for a Word that does not contain a Packed Word.");

            return new Word(ONE << (SIZE - 2 - PackedPosition));
        }
    }

    internal void Pack(Word word)
    {
        if (!IsCompressed)
            throw new NotSupportedException("Cannot pack a Word into an uncompressed Word.");

        if (HasPackedWord)
            throw new NotSupportedException("Cannot pack a Word into a Word that already HasPackedWord.");

        if (word.IsCompressed)
            throw new NotSupportedException("Cannot pack a compressed Word.");

        if (word.Population != 1)
            throw new NotSupportedException("Can only pack a Word with exactly 1 bit set (Population = 1).");

        WordRawType packedPosition = (WordRawType)word.GetBitPositions(true)[0];
        Raw |= (packedPosition + 1) << (SIZE - 2 - PACKEDPOSITIONSIZE);
    }

    internal Word Unpack()
    {
        if (!IsCompressed)
            throw new NotSupportedException("Cannot unpack a Word from an uncompressed Word.");

        if (!HasPackedWord)
            throw new NotSupportedException("Cannot unpack a Word from a Word that does not HasPackedWord.");

        var packedWord = PackedWord;

        Raw &= ~PACKEDPOSITIONMASK;

        return packedWord;
    }

#endif

    #endregion

    #region Population

    public readonly int Population
    {
        get
        {
            if (Raw == ZERO)
                return 0;

            if (IsCompressed)
            {
                int population = 0;

                if (FillBit)
                    population = FillCount * (SIZE - 1);

#if POSITIONLISTENABLED
                if (HasPackedWord)
                    population++;
#endif

                return population;
            }
            else
                return BitOperations.PopCount(Raw);
        }
    }

    #endregion

    public override readonly string ToString() => FormattableString.Invariant($"0x{Raw:X}");
}
