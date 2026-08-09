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

public class T_Vector_Construction
{
    [Fact]
    public void ConstructCopy()
    {
        int[] fillMaxBitPositions = [99, 499];
        int[] fillCounts = [0, 1, 2, 5, 10, 20, 30, 40, 50, 100, 200, 300, 400, 450, 460, 470, 480, 490, 495, 498, 499, 500];

        var instructions =
            from sourceIsCompressed in new[] { false, true }
            from resultIsCompressed in new[] { false, true }
            from fillMaxBitPosition in fillMaxBitPositions
            from fillCount in fillCounts
            where fillCount <= fillMaxBitPosition + 1
            select new
            {
                SourceIsCompressed = sourceIsCompressed,
                ResultIsCompressed = resultIsCompressed,
                FillMaxBitPosition = fillMaxBitPosition,
                FillCount = fillCount
            };

        foreach (var instruction in instructions)
        {
            var source = new Vector(instruction.SourceIsCompressed);
            source.IsCompressed.Should().Be(instruction.SourceIsCompressed);

            int[] bitPositions = source.SetBitsRandom(instruction.FillMaxBitPosition, instruction.FillCount, true);

            var result = new Vector(instruction.ResultIsCompressed, source);
            result.IsCompressed.Should().Be(instruction.ResultIsCompressed);

            result.AssertBitPositions(bitPositions);

            var resultWords = (Word[])typeof(Vector).GetField("_words", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(result);

            if (!instruction.ResultIsCompressed)
                resultWords.Any(word => word.IsCompressed).Should().BeFalse();

            source.WordsClear();
            source.Population.Should().Be(0);
            source.AssertBitPositions();

            result.WordsClear();
            result.Population.Should().Be(0);
            result.AssertBitPositions();
        }
    }
}
