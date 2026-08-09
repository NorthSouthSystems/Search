#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;
#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;
#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;
#else
using NorthSouthSystems.BitVectors.WAH;
#endif

using System.Reflection;

internal static class T_VectorExtensionsForTests
{
    #region Assert

    internal static void AssertWordCounts(this Vector vector, int expectedWordCountPhysical, int expectedWordCountLogical)
    {
        ((int)_wordCountPhysicalField.GetValue(vector)).Should().Be(expectedWordCountPhysical);
        ((int)_wordCountLogicalField.GetValue(vector)).Should().Be(expectedWordCountLogical);
    }

    private static readonly FieldInfo _wordCountPhysicalField = typeof(Vector).GetField("_wordCountPhysical", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo _wordCountLogicalField = typeof(Vector).GetField("_wordCountLogical", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static void AssertWordLogicalValues(this Vector vector, params uint[] expectedWordLogicalValues)
    {
        for (int i = 0; i < expectedWordLogicalValues.Length; i++)
            vector.GetWordLogical(i).Raw.Should().Be(expectedWordLogicalValues[i], because: "i=" + i.ToString());
    }

    internal static void AssertBitPositions(this Vector vector, params IEnumerable<int>[] expectedBitPositionses)
    {
        int[] expectedBitPositions = expectedBitPositionses.SelectMany(bps => bps)
            .Distinct()
            .OrderBy(bitPosition => bitPosition)
            .ToArray();

        bool[] expectedBits = expectedBitPositions.Length == 0 ? [] : new bool[expectedBitPositions.Max() + 1];

        foreach (int expectedBitPosition in expectedBitPositions)
            expectedBits[expectedBitPosition] = true;

        vector.GetBitPositions(true).Should().Equal(expectedBitPositions);

        if (!vector.IsCompressed)
            vector.Bits.Reverse().SkipWhile(bit => !bit).Reverse().Should().Equal(expectedBits);

        // FluentAssertions in this loop had a noticible negative performance impact.
        foreach (int expectedBitPosition in expectedBitPositions)
            Assert.True(vector[expectedBitPosition]);

        vector.Population.Should().Be(expectedBitPositions.Length);
        vector.PopulationAny.Should().Be(expectedBitPositions.Length > 0);
    }

    #endregion

    #region Setting

    internal static void SetBits(this Vector vector, int[] bitPositions, bool value)
    {
        foreach (int bitPosition in bitPositions)
            vector[bitPosition] = value;
    }

    internal static int[] SetBitsRandom(this Vector vector, int maxBitPosition, int count, bool value)
    {
        int[] bitPositions = Enumerable.Range(0, maxBitPosition + 1)
            .Select(bitPosition => new { BitPosition = bitPosition, RandomDouble = _random.NextDouble() })
            .OrderBy(bitPosition => bitPosition.RandomDouble)
            .Take(count)
            .Select(bitPosition => bitPosition.BitPosition)
            .OrderBy(bitPosition => bitPosition)
            .ToArray();

        vector.SetBits(bitPositions, value);
        return bitPositions;
    }

    private static readonly Random _random = new(4438);

    #endregion
}
