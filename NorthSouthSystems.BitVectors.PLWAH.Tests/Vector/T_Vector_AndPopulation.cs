#if POSITIONLISTENABLED && WORDSIZE64
using NorthSouthSystems.BitVectors.PLWAH64;

#elif POSITIONLISTENABLED
using NorthSouthSystems.BitVectors.PLWAH;

#elif WORDSIZE64
using NorthSouthSystems.BitVectors.WAH64;

#else
using NorthSouthSystems.BitVectors.WAH;
#endif

public class T_Vector_AndPopulation
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void AndPopulation(bool vector1IsCompressed, bool vector2IsCompressed)
    {
        int[] fillCounts = [0, 1, 5, 10, 20, 30, 40, 50, 100, 200, 300, 400, 450, 460, 470, 480, 490, 495, 499, 500];

        foreach (int fillCount1 in fillCounts)
        {
            foreach (int fillCount2 in fillCounts)
            {
                var vector1 = new Vector(vector1IsCompressed);
                int[] bitPositions1 = vector1.SetBitsRandom(499, fillCount1, true);
                var vector2 = new Vector(vector2IsCompressed);
                int[] bitPositions2 = vector2.SetBitsRandom(499, fillCount2, true);

                var bitPositions = new HashSet<int>(bitPositions1);
                bitPositions.IntersectWith(bitPositions2);

                int andPopulation1 = vector1.AndPopulation(vector2);
                andPopulation1.Should().Be(bitPositions.Count);

                int andPopulation2 = vector2.AndPopulation(vector1);
                andPopulation2.Should().Be(bitPositions.Count);
            }
        }
    }

    [Fact]
    public void Exceptions()
    {
        Action act;

        act = () =>
        {
            var vector = new Vector(false);
            vector.AndPopulation(null);
        };
        act.Should().ThrowExactly<ArgumentNullException>(because: "AndPopulationArgumentNull");
    }
}
