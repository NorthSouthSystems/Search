#if POSITIONLISTENABLED && WORDSIZE64
namespace NorthSouthSystems.BitVectors.PLWAH64;
#elif POSITIONLISTENABLED
namespace NorthSouthSystems.BitVectors.PLWAH;
#elif WORDSIZE64
namespace NorthSouthSystems.BitVectors.WAH64;
#else
namespace NorthSouthSystems.BitVectors.WAH;
#endif

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Word-aligned hybrid bit vector.
/// </summary>
/// <remarks>
/// The implementation REQUIRES that <c>_words[WordCount - 1].IsCompressed == false</c> at all times and only allows
/// <c>Set</c>ting bits on that Word and forward. In code comments will refer to this as the LAW.
/// </remarks>
public sealed partial class Vector : IBitVector<Vector>
{
    #region Construction

    public Vector(bool isCompressed)
        : this(isCompressed, null, 0)
    { }

    public Vector(bool isCompressed, Vector vector)
        : this(isCompressed, vector, 0)
    { }

    private Vector(bool isCompressed, Vector? vector, int wordsLengthHint)
    {
        IsCompressed = isCompressed;

        if (vector == null)
        {
            WordsGrow(wordsLengthHint);
            _wordCountPhysical = 1;
            _wordCountLogical = 1;

            return;
        }

        // There are 4 possible "copy" combinations: IsCompressed x vector.IsCompressed
        // 1. IsCompressed == vector.IsCompressed : Handle the binary copies first.
        // 2. !IsCompressed (implicit vector.IsCompressed) : DecompressInPlace.
        // 3. IsCompressed (implicit !vector.IsCompressed) : Compress.
        if (IsCompressed == vector.IsCompressed)
        {
            WordsGrow(Math.Max(vector._wordCountPhysical, wordsLengthHint));
            _wordCountPhysical = vector._wordCountPhysical;
            _wordCountLogical = vector._wordCountLogical;

            Array.Copy(vector._words, _words, vector._wordCountPhysical);
        }
        else if (!IsCompressed)
        {
            WordsGrow(Math.Max(vector._wordCountLogical, wordsLengthHint));
            _wordCountPhysical = vector._wordCountLogical;
            _wordCountLogical = vector._wordCountLogical;

            DecompressInPlace(vector);
        }
        else
        {
            WordsGrow(wordsLengthHint);
            _wordCountPhysical = 1;
            _wordCountLogical = 1;

            Trace.Assert(vector._wordCountPhysical == vector._wordCountLogical);

            for (int i = 0; i < vector._wordCountPhysical; i++)
                SetWord(i, vector._words[i]);
        }
    }

    public bool IsUnused => _wordCountLogical == 1 && _words[0].Raw == Word.ZERO;

    #endregion

    #region Optimize

    public bool OptimizeReadPhase(int[] bitPositionShifts, out Vector optimized)
    {
        ArgumentNullException.ThrowIfNull(bitPositionShifts);

        optimized = new Vector(IsCompressed);

        foreach (int bitPosition in GetBitPositions(true))
        {
            int positionShift = bitPositionShifts[bitPosition];

            if (positionShift >= 0)
                optimized[bitPosition - positionShift] = true;
        }

        return optimized._wordCountLogical > 1 || optimized._words[0].Raw > Word.ZERO;
    }

    #endregion

    #region Words

    public bool IsCompressed { get; }

    private Word[] _words;
    private int _wordCountPhysical;
    private int _wordCountLogical;

    void IBitVector<Vector>.Clear() => WordsClear();

    public void WordsClear()
    {
        if (_words != null)
            ArrayPool<Word>.Shared.Return(_words);

        _words = null!;
        WordsGrow(0);
        _wordCountPhysical = 1;
        _wordCountLogical = 1;
    }

    [MemberNotNull(nameof(_words))]
    private void WordsGrow(int length)
    {
        length = Math.Max(length, 2);

        if (_words == null)
        {
            _words = ArrayPool<Word>.Shared.Rent(length);

            // We choose to explicitly Clear (rather than use the Rent overload) so that in the "else if" case below
            // we don't waste cycles Clearing parts of the Array that are going to receive a Copy.
            Array.Clear(_words, 0, _words.Length);
        }
        else if (_words.Length < length)
        {
            length = Convert.ToInt32(length * WORDGROWTHFACTOR);

            Word[] tempWords = _words;
            _words = ArrayPool<Word>.Shared.Rent(length);
            Array.Copy(tempWords, _words, _wordCountPhysical);

            // We choose to explicitly Clear (rather than use the Rent overload) so that
            // we don't waste cycles Clearing parts of the Array that received a Copy.
            //
            // We know that _wordCountPhysical < _words.Length <= length, so we don't need to check it here.
            Array.Clear(_words, _wordCountPhysical, _words.Length - _wordCountPhysical);

            ArrayPool<Word>.Shared.Return(tempWords);
        }
    }

    private const double WORDGROWTHFACTOR = 1.1;

    private Span<Word> GetWordsSpanPhysical() => new(_words, 0, _wordCountPhysical);

    #endregion

    #region Indexers

    public bool this[int bitPosition]
    {
        get
        {
            if (bitPosition < 0)
                throw new ArgumentOutOfRangeException(nameof(bitPosition), bitPosition, "Must be > 0.");

            int wordPositionLogical = WordPositionLogical(bitPosition);
            Word word = GetWordLogical(wordPositionLogical);
            int wordBitPosition = WordBitPosition(bitPosition);
            return word[wordBitPosition];
        }
        set
        {
            if (bitPosition < 0)
                throw new ArgumentOutOfRangeException(nameof(bitPosition), bitPosition, "Must be > 0.");

            SetBit(bitPosition, value);
        }
    }

    internal Word GetWordLogical(int wordPositionLogical)
    {
        if (wordPositionLogical < 0)
            throw new ArgumentOutOfRangeException(nameof(wordPositionLogical), wordPositionLogical, "Must be > 0.");

        if (wordPositionLogical >= _wordCountLogical)
            return new Word(Word.ZERO);

#if POSITIONLISTENABLED
        int wordPositionPhysical = WordPositionPhysical(wordPositionLogical, out bool isPacked);
#else
        int wordPositionPhysical = WordPositionPhysical(wordPositionLogical);
#endif
        Word word = _words[wordPositionPhysical];

        if (word.IsCompressed)
        {
#if POSITIONLISTENABLED
            if (isPacked)
                return word.PackedWord;
            else
#endif
                return new Word((word.FillBit && word.FillCount > 0) ? Word.COMPRESSIBLEMASK : Word.ZERO);
        }
        else
            return word;
    }

    private static int WordPositionLogical(int bitPosition) => bitPosition / (Word.SIZE - 1);

    private static int WordBitPosition(int bitPosition) => bitPosition % (Word.SIZE - 1);

#if POSITIONLISTENABLED
    private int WordPositionPhysical(int wordPositionLogical, out bool isPacked)
    {
        isPacked = false;
#else
    private int WordPositionPhysical(int wordPositionLogical)
    {
#endif
        // PERF : Short circuits. When !IsCompressed wordPositionLogical === wordPositionPhysical, ALWAYS.
        // When it IsCompressed, we take advantage of the fact that if you are asking for the last logical word,
        // that word must reside on the last physical word.
        if (!IsCompressed)
            return wordPositionLogical;
        else if (wordPositionLogical == _wordCountLogical - 1)
            return _wordCountPhysical - 1;

        int logical = 0;

        for (int i = 0; i < _wordCountPhysical; i++)
        {
            Word word = _words[i];
#if POSITIONLISTENABLED
            isPacked = false;
#endif

            if (word.IsCompressed)
            {
                logical += word.FillCount;

                if (logical > wordPositionLogical)
                    return i;

#if POSITIONLISTENABLED
                if (word.HasPackedWord)
                {
                    isPacked = true;
                    logical++;

                    if (logical > wordPositionLogical)
                        return i;
                }
#endif
            }
            else
            {
                logical++;

                if (logical > wordPositionLogical)
                    return i;
            }
        }

        throw new NotImplementedException("Error in algorithm.");
    }

    #endregion

    #region Setting

    internal void SetWord(int wordPositionLogical, Word word)
    {
        if (wordPositionLogical < 0)
            throw new ArgumentOutOfRangeException(nameof(wordPositionLogical), wordPositionLogical, "Must be >= 0.");

        if (IsCompressed && wordPositionLogical < (_wordCountLogical - 1))
            throw new NotSupportedException("Writing is forward-only for a compressed Vector.");

        bool isZero = word.Raw == Word.ZERO
            || (word.IsCompressed
                && !word.FillBit
#if POSITIONLISTENABLED
                && !word.HasPackedWord
#endif
            );

        if (isZero && ZeroFillCount(wordPositionLogical) > 0)
            return;

        ZeroFill(wordPositionLogical);

#if POSITIONLISTENABLED
        // IsPacked can be safely ignored here because of the LAW.
        int wordPositionPhysical = WordPositionPhysical(wordPositionLogical, out _);
#else
        int wordPositionPhysical = WordPositionPhysical(wordPositionLogical);
#endif

        if (isZero)
        {
            _words[wordPositionPhysical].Raw = Word.ZERO;
        }
        else if (!word.IsCompressed)
        {
            _words[wordPositionPhysical] = word;
        }
        else
        {
            if (!IsCompressed)
            {
                WordsGrow(_wordCountPhysical + word.FillCount - 1);
                _wordCountPhysical += word.FillCount - 1;
                _wordCountLogical += word.FillCount - 1;

                if (word.FillBit)
                    for (int i = wordPositionPhysical; i < _wordCountPhysical; i++)
                        _words[i].Raw = Word.COMPRESSIBLEMASK;

#if POSITIONLISTENABLED
                if (word.HasPackedWord)
                {
                    WordsGrow(_wordCountPhysical + 1);
                    _wordCountPhysical++;
                    _wordCountLogical++;

                    _words[_wordCountPhysical - 1] = word.PackedWord;
                }
#endif
            }
            else
            {
                // We're breaking the LAW here; however, we will NOT enter any other Vector functions until it is restored.
                _words[wordPositionPhysical] = word;
                _wordCountLogical += word.FillCount - 1;

#if POSITIONLISTENABLED
                if (word.HasPackedWord)
                    _wordCountLogical++;
#endif

                // Immediately restore the LAW.
                WordsGrow(_wordCountPhysical + 1);
                _wordCountPhysical++;
                _wordCountLogical++;
            }
        }
    }

    private void SetBit(int bitPosition, bool value)
    {
        if (bitPosition < 0)
            throw new ArgumentOutOfRangeException(nameof(bitPosition), bitPosition, "Must be >= 0.");

        int wordPositionLogical = WordPositionLogical(bitPosition);
        int wordBitPosition = WordBitPosition(bitPosition);

        if (IsCompressed && wordPositionLogical < _wordCountLogical - 1)
            throw new NotSupportedException("Writing is forward-only for a compressed Vector.");

        if (!value && ZeroFillCount(wordPositionLogical) > 0)
            return;

        ZeroFill(wordPositionLogical);

#if POSITIONLISTENABLED
        // IsPacked can be safely ignored here because of the LAW.
        int wordPositionPhysical = WordPositionPhysical(wordPositionLogical, out _);
#else
        int wordPositionPhysical = WordPositionPhysical(wordPositionLogical);
#endif
        _words[wordPositionPhysical][wordBitPosition] = value;
    }

    #endregion

    #region Zero Filling

    private int ZeroFillCount(int wordPositionLogical) => wordPositionLogical - (_wordCountLogical - 1);

    private void ZeroFill(int wordPositionLogical)
    {
        if (IsCompressed)
        {
            ZeroFillWhenCompressedAndSingleWord(wordPositionLogical);
            ZeroFillWhenCompressedAndTailIsCompressedAndCompressible(wordPositionLogical);
#if POSITIONLISTENABLED
            ZeroFillWhenCompressedAndTailIsPackable(wordPositionLogical);
#endif
            ZeroFillWhenCompressedAndTailIsZero(wordPositionLogical);
            ZeroFillWhenCompressedAndLargeFill(wordPositionLogical);
        }

        ZeroFillFinish(wordPositionLogical);
    }

    private void ZeroFillWhenCompressedAndSingleWord(int wordPositionLogical)
    {
        int zeroFillCount = ZeroFillCount(wordPositionLogical);

        if (zeroFillCount > 0 && _wordCountPhysical == 1)
        {
            WordsGrow(2);
            _wordCountPhysical++;
            _wordCountLogical++;

            _words[_wordCountPhysical - 2].Compress();
            _words[_wordCountPhysical - 1].Raw = Word.ZERO;
        }
    }

    private void ZeroFillWhenCompressedAndTailIsCompressedAndCompressible(int wordPositionLogical)
    {
        int zeroFillCount = ZeroFillCount(wordPositionLogical);

        if (zeroFillCount > 0
            && _words[_wordCountPhysical - 2].IsCompressed
#if POSITIONLISTENABLED
            && !_words[_wordCountPhysical - 2].HasPackedWord
#endif
            && _words[_wordCountPhysical - 1].IsCompressible
            && _words[_wordCountPhysical - 2].FillBit == _words[_wordCountPhysical - 1].CompressibleFillBit)
        {
            if (_words[_wordCountPhysical - 2].FillBit)
            {
#if POSITIONLISTENABLED
                ThrowIfFillCountOverflow(_words[_wordCountPhysical - 2].FillCount + 1);
#endif

                _wordCountLogical++;

                _words[_wordCountPhysical - 2].Raw++;
                _words[_wordCountPhysical - 1].Raw = Word.ZERO;
            }
            else
            {
#if POSITIONLISTENABLED
                ThrowIfFillCountOverflow(_words[_wordCountPhysical - 2].FillCount + zeroFillCount);
#endif

                _wordCountLogical += zeroFillCount;

                _words[_wordCountPhysical - 2].Raw += (WordRawType)zeroFillCount;
                _words[_wordCountPhysical - 1].Raw = Word.ZERO;
            }
        }
    }

#if POSITIONLISTENABLED
    private void ZeroFillWhenCompressedAndTailIsPackable(int wordPositionLogical)
    {
        int zeroFillCount = ZeroFillCount(wordPositionLogical);

        if (zeroFillCount > 0
            && _words[_wordCountPhysical - 2].IsCompressed
            && !_words[_wordCountPhysical - 2].HasPackedWord
            && _words[_wordCountPhysical - 1].Population == 1)
        {
            _wordCountLogical++;

            _words[_wordCountPhysical - 2].Pack(_words[_wordCountPhysical - 1]);
            _words[_wordCountPhysical - 1].Raw = Word.ZERO;
        }
    }
#endif

    private void ZeroFillWhenCompressedAndTailIsZero(int wordPositionLogical)
    {
        int zeroFillCount = ZeroFillCount(wordPositionLogical);

        if (zeroFillCount > 0 && _words[_wordCountPhysical - 1].Raw == 0)
        {
#if POSITIONLISTENABLED
            ThrowIfFillCountOverflow(zeroFillCount);
#endif

            WordsGrow(_wordCountPhysical + 1);
            _wordCountPhysical++;
            _wordCountLogical += zeroFillCount;

            _words[_wordCountPhysical - 2].Compress();
            _words[_wordCountPhysical - 2].Raw += (WordRawType)(zeroFillCount - 1);
            _words[_wordCountPhysical - 1].Raw = Word.ZERO;
        }
    }

    private void ZeroFillWhenCompressedAndLargeFill(int wordPositionLogical)
    {
        int zeroFillCount = ZeroFillCount(wordPositionLogical);

        if (zeroFillCount > 1)
        {
#if POSITIONLISTENABLED
            ThrowIfFillCountOverflow(zeroFillCount - 1);
#endif

            _words[_wordCountPhysical - 1].Compress();

            WordsGrow(_wordCountPhysical + 2);
            _wordCountPhysical += 2;
            _wordCountLogical += zeroFillCount;

            _words[_wordCountPhysical - 2].Compress();
            _words[_wordCountPhysical - 2].Raw += (WordRawType)(zeroFillCount - 2);
            _words[_wordCountPhysical - 1].Raw = Word.ZERO;
        }
    }

    private void ZeroFillFinish(int wordPositionLogical)
    {
        int zeroFillCount = ZeroFillCount(wordPositionLogical);

        if (zeroFillCount > 0)
        {
            if (IsCompressed)
                _words[_wordCountPhysical - 1].Compress();

            WordsGrow(_wordCountPhysical + zeroFillCount);
            _wordCountPhysical += zeroFillCount;
            _wordCountLogical += zeroFillCount;
        }
    }

#if POSITIONLISTENABLED
    // Due to the bit size of Word.FillCount and its multiplicative nature, runtime FillCount overflow is only possible when
    // POSITIONLISTENABLED because it reduces the bit size of FillCount by log2(Word.SIZE) (...and because the first 2 bits
    // of Word are reserved for Compression tracking). Thus, the first bitPosition that could cause FillCount overflow for a
    // 32-bit Word with POSITIONLISTENABLED is:
    // (2^(32 - 2 - 5) + 1) * (2^5 - 1) = approx 1.04e9 = 1.04B.
    //
    // Furthermore, it is only practically possible for a 32-bit Word with POSITIONLISTENABLED to runtime FillCount overflow because
    // a 64-bit Word with POSITIONLISTENABLED can still represent an astronomical first bitPosition FillCount overflow of:
    // (2^(64 - 2 - 6) + 1) * (2^6 - 1) = approx 4.54e18. = 4.5B * 1B.
    //
    // Technically, we could avoid all overflows by splitting Words when reaching max FillCount; however, the effort is not worth
    // the value. The Word constructor that takes a fillCount parameter and is used by VectorOperations would also need a completely
    // different implementation for Word splitting that would require even more effort for little value.
    private static void ThrowIfFillCountOverflow(int fillCount)
    {
        if (fillCount < 0)
            throw new ArgumentOutOfRangeException(nameof(fillCount), fillCount, "Must be >= 0.");

        if ((WordRawType)fillCount > Word.FILLCOUNTMASK)
            throw new NotSupportedException("Word.FillCount overflow detected, and Word splitting is not supported.");
    }
#endif

    #endregion

    #region Population

    public int Population
    {
        get
        {
            int population = 0;

            for (int i = 0; i < _wordCountPhysical; i++)
                population += _words[i].Population;

            return population;
        }
    }

    public bool PopulationAny
    {
        get
        {
            for (int i = 0; i < _wordCountPhysical; i++)
                if (_words[i].Population > 0)
                    return true;

            return false;
        }
    }

    #endregion

    #region Bit Positions

    public IEnumerable<bool> Bits
    {
        get
        {
            if (IsCompressed)
                throw new NotSupportedException("Not supported for a compressed Vector.");

            for (int i = 0; i < _wordCountPhysical; i++)
                foreach (bool bit in _words[i].Bits)
                    yield return bit;
        }
    }

    public IEnumerable<int> GetBitPositions(bool value)
    {
        int bitPositionOffset = 0;

        for (int i = 0; i < _wordCountPhysical; i++)
        {
            Word word = _words[i];

            if (word.IsCompressed)
            {
                if (word.FillBit == value)
                {
                    int population = word.FillCount * (Word.SIZE - 1);
                    int stopPosition = bitPositionOffset + population;

                    while (bitPositionOffset < stopPosition)
                        yield return bitPositionOffset++;
                }
                else
                    bitPositionOffset += word.FillCount * (Word.SIZE - 1);

#if POSITIONLISTENABLED
                if (word.HasPackedWord)
                {
                    yield return word.PackedPosition + bitPositionOffset;

                    bitPositionOffset += (Word.SIZE - 1);
                }
#endif
            }
            else
            {
                // PERF : Always check HasBitPositions before enumerating on GetBitPositions.
                if (word.HasBitPositions(value))
                    foreach (int bitPosition in word.GetBitPositions(value))
                        yield return bitPosition + bitPositionOffset;

                bitPositionOffset += (Word.SIZE - 1);
            }
        }
    }

    #endregion

    #region VectorOperations

    public void DecompressInPlace(Vector vector)
    {
        ArgumentNullException.ThrowIfNull(vector);

        if (IsCompressed)
            throw new NotSupportedException("Not supported for a compressed Vector.");

        if (!vector.IsCompressed)
            throw new NotSupportedException("Not supported for two uncompressed Vectors.");

        WordsGrow(vector._wordCountLogical);
        _wordCountPhysical = vector._wordCountLogical;
        _wordCountLogical = vector._wordCountLogical;

        DecompressInPlaceNoneCompressed(this, vector);
    }

    public void AndInPlace(Vector vector)
    {
        ArgumentNullException.ThrowIfNull(vector);

        if (IsCompressed)
            throw new NotSupportedException("Not supported for a compressed Vector.");

        if (!vector.IsCompressed)
            AndInPlaceNoneNone(this, vector);
        else
            AndInPlaceNoneCompressed(this, vector);
    }

    public Vector AndOutOfPlace(Vector vector, bool resultIsCompressed)
    {
        ArgumentNullException.ThrowIfNull(vector);

        var (lessCompression, moreCompression) = OrderByCompression(vector);

        if (!moreCompression.IsCompressed)
            return AndOutOfPlaceNoneNone(lessCompression, moreCompression, resultIsCompressed);
        else if (!lessCompression.IsCompressed)
            return AndOutOfPlaceNoneCompressed(lessCompression, moreCompression, resultIsCompressed);
        else
            return AndOutOfPlaceCompressedCompressed(lessCompression, moreCompression, resultIsCompressed);
    }

    public int AndPopulation(Vector vector)
    {
        ArgumentNullException.ThrowIfNull(vector);

        var (lessCompression, moreCompression) = OrderByCompression(vector);

        if (!moreCompression.IsCompressed)
            return AndPopulationNoneNone(lessCompression, moreCompression);
        else if (!lessCompression.IsCompressed)
            return AndPopulationNoneCompressed(lessCompression, moreCompression);
        else
            return AndPopulationCompressedCompressed(lessCompression, moreCompression);
    }

    public bool AndPopulationAny(Vector vector)
    {
        ArgumentNullException.ThrowIfNull(vector);

        if (IsCompressed && vector.IsCompressed)
            throw new NotImplementedException("Not implemented for two compressed Vector.");

        var (lessCompression, moreCompression) = OrderByCompression(vector);

        if (!moreCompression.IsCompressed)
            return AndPopulationAnyNoneNone(lessCompression, moreCompression);
        else if (!lessCompression.IsCompressed)
            return AndPopulationAnyNoneCompressed(lessCompression, moreCompression);
        else
            throw new NotImplementedException("See above. This code will never execute.");
    }

    public void OrInPlace(Vector vector)
    {
        ArgumentNullException.ThrowIfNull(vector);

        if (IsCompressed)
            throw new NotSupportedException("Not supported for a compressed Vector.");

        WordsGrow(vector._wordCountLogical);
        _wordCountPhysical = Math.Max(_wordCountPhysical, vector._wordCountLogical);
        _wordCountLogical = Math.Max(_wordCountLogical, vector._wordCountLogical);

        if (!vector.IsCompressed)
            OrInPlaceNoneNone(this, vector);
        else
            OrInPlaceNoneCompressed(this, vector);
    }

    private (Vector LessCompression, Vector MoreCompression) OrderByCompression(Vector vector) =>
        !IsCompressed ? (this, vector) : (vector, this);

    #endregion
}
