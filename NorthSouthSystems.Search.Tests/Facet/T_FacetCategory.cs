public class T_FacetCategory
{
    [Fact]
    public void EqualityAndHashing()
    {
        var category1 = new FacetCategory<int>(1, 1);
        var category2 = new FacetCategory<int>(1, 1);

        category1.Equals(category2).Should().BeTrue();
        category1.Equals((object)category2).Should().BeTrue();
        category1.Equals(null).Should().BeFalse();
        category1.Equals("test").Should().BeFalse();
        (category1 == category2).Should().BeTrue();
        (category1 != category2).Should().BeFalse();
        category2.GetHashCode().Should().Be(category1.GetHashCode());

        category2 = new FacetCategory<int>(2, 1);

        category1.Equals(category2).Should().BeFalse();
        category1.Equals((object)category2).Should().BeFalse();
        (category1 == category2).Should().BeFalse();
        (category1 != category2).Should().BeTrue();

        category2 = new FacetCategory<int>(1, 2);

        category1.Equals(category2).Should().BeFalse();
        category1.Equals((object)category2).Should().BeFalse();
        (category1 == category2).Should().BeFalse();
        (category1 != category2).Should().BeTrue();
    }
}
