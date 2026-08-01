using FluentAssertions;
using PepperX.QueryForge.Querying;

namespace PepperX.QueryForge.Tests.Querying;

public class HierarchyBuilderTests
{
    private sealed record Row(string? Country, string? City, int Population);

    private static readonly Row[] Sample =
    [
        new("Iran", "Tehran", 9),
        new("Iran", "Shiraz", 2),
        new("Iran", "Tehran", 1),
        new("Canada", "Toronto", 3),
        new("Canada", "Ottawa", 1)
    ];

    [Fact]
    public void Build_WithNoGroups_ShouldReturnEmpty()
    {
        var result = HierarchyBuilder.Build(Sample, Array.Empty<GroupByDescriptor>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void Build_WithNoRows_ShouldReturnEmpty()
    {
        var result = HierarchyBuilder.Build(
            Array.Empty<Row>(),
            [new GroupByDescriptor("Country")]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Build_SingleLevel_ShouldGroupAndCountLeafRows()
    {
        var result = HierarchyBuilder.Build(Sample, [new GroupByDescriptor("Country")]);

        result.Should().HaveCount(2);
        result[0].Key.Should().Be("Canada");
        result[0].Count.Should().Be(2);
        result[1].Key.Should().Be("Iran");
        result[1].Count.Should().Be(3);
    }

    [Fact]
    public void Build_LeafLevel_ShouldCarryItemsAndNoSubGroups()
    {
        var result = HierarchyBuilder.Build(Sample, [new GroupByDescriptor("Country")]);

        foreach (var node in result)
        {
            node.Items.Should().NotBeNull();
            node.SubGroups.Should().BeNull();
        }
    }

    [Fact]
    public void Build_ParentLevel_ShouldCarrySubGroupsAndNoItems()
    {
        var result = HierarchyBuilder.Build(
            Sample,
            [new GroupByDescriptor("Country"), new GroupByDescriptor("City")]);

        foreach (var node in result)
        {
            node.SubGroups.Should().NotBeNull();
            node.Items.Should().BeNull();
        }
    }

    [Fact]
    public void Build_MultiLevel_ParentCountShouldBeTotalLeafRowsNotChildCount()
    {
        var result = HierarchyBuilder.Build(
            Sample,
            [new GroupByDescriptor("Country"), new GroupByDescriptor("City")]);

        var iran = result.Single(n => Equals(n.Key, "Iran"));

        // Two distinct cities, but three underlying rows.
        iran.SubGroups!.Should().HaveCount(2);
        iran.Count.Should().Be(3);

        var tehran = iran.SubGroups!.Single(n => Equals(n.Key, "Tehran"));
        tehran.Count.Should().Be(2);
        tehran.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Build_Descending_ShouldReverseKeyOrder()
    {
        var result = HierarchyBuilder.Build(
            Sample,
            [new GroupByDescriptor("Country", SortOrder.Descending)]);

        result.Select(n => n.Key).Should().ContainInOrder("Iran", "Canada");
    }

    [Fact]
    public void Build_WithNullKeys_ShouldFormTheirOwnGroupFirstWhenAscending()
    {
        Row[] rows =
        [
            new("Iran", "Tehran", 1),
            new(null, "Unknown", 1),
            new(null, "Nowhere", 1)
        ];

        var result = HierarchyBuilder.Build(rows, [new GroupByDescriptor("Country")]);

        result.Should().HaveCount(2);
        result[0].Key.Should().BeNull();
        result[0].Count.Should().Be(2);
        result[1].Key.Should().Be("Iran");
    }

    [Fact]
    public void Build_WithNullKeys_ShouldSortNullsLastWhenDescending()
    {
        Row[] rows =
        [
            new(null, "Unknown", 1),
            new("Iran", "Tehran", 1)
        ];

        var result = HierarchyBuilder.Build(
            rows,
            [new GroupByDescriptor("Country", SortOrder.Descending)]);

        result[0].Key.Should().Be("Iran");
        result[1].Key.Should().BeNull();
    }

    [Fact]
    public void Build_ShouldPreserveIncomingRowOrderWithinLeafItems()
    {
        var result = HierarchyBuilder.Build(Sample, [new GroupByDescriptor("Country")]);

        var iran = result.Single(n => Equals(n.Key, "Iran"));

        // Sample order for Iran is Tehran(9), Shiraz(2), Tehran(1).
        iran.Items!.Select(r => r.Population).Should().ContainInOrder(9, 2, 1);
    }

    [Fact]
    public void Build_ShouldOrderNumericKeysNumericallyNotLexically()
    {
        var rows = new[] { 2, 10, 1 }.Select(i => new Row("X", "Y", i)).ToArray();

        var result = HierarchyBuilder.Build(rows, [new GroupByDescriptor("Population")]);

        result.Select(n => n.Key).Should().ContainInOrder(1, 2, 10);
    }

    [Fact]
    public void Build_WithCustomAccessor_ShouldBypassReflection()
    {
        var result = HierarchyBuilder.Build(
            Sample,
            [new GroupByDescriptor("whatever")],
            (row, _) => row.Country);

        result.Select(n => n.Key).Should().ContainInOrder("Canada", "Iran");
    }

    [Fact]
    public void Build_WithUnknownColumn_ShouldCollapseIntoASingleNullGroup()
    {
        var result = HierarchyBuilder.Build(Sample, [new GroupByDescriptor("DoesNotExist")]);

        result.Should().HaveCount(1);
        result[0].Key.Should().BeNull();
        result[0].Count.Should().Be(Sample.Length);
    }

    [Fact]
    public void Build_ThreeLevels_ShouldNestAndCountAtEveryDepth()
    {
        Row[] rows =
        [
            new("Iran", "Tehran", 1),
            new("Iran", "Tehran", 1),
            new("Iran", "Shiraz", 2),
            new("Canada", "Toronto", 3)
        ];

        var result = HierarchyBuilder.Build(
            rows,
            [
                new GroupByDescriptor("Country"),
                new GroupByDescriptor("City"),
                new GroupByDescriptor("Population")
            ]);

        var iran = result.Single(n => Equals(n.Key, "Iran"));
        iran.Count.Should().Be(3);

        var tehran = iran.SubGroups!.Single(n => Equals(n.Key, "Tehran"));
        tehran.Count.Should().Be(2);
        tehran.SubGroups.Should().NotBeNull();

        var population = tehran.SubGroups!.Single();
        population.Key.Should().Be(1);
        population.Count.Should().Be(2);
        population.Items.Should().HaveCount(2);
    }
}
