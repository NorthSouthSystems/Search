#if WORDSIZE64
global using WordRawType = ulong;
#else
global using WordRawType = uint;
#endif

#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;
#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;
#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;
#else
using NorthSouthSystems.BitVectors.WAH;
#endif

internal static class T_WordExtensionsForTests
{
    internal const int LARGEPRIME = 9973;

    internal const WordRawType LARGEPRIME32ORFULLCOVERAGE64
#if WORDSIZE64
        = (WordRawType)LARGEPRIME * uint.MaxValue;
#else
        = LARGEPRIME;
#endif

    internal const int WORDCOUNTFORRANDOMTESTS
#if WORDSIZE64
        = 5;
#else
        = 10;
#endif
}
