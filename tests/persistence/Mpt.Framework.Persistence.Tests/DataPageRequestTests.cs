using FluentAssertions;
using Mpt.Rql;

namespace Mpt.Framework.Persistence.Tests;

public class DataPageRequestTests
{
    [Fact]
    public void ConvenienceConstructor_AssignsEmptyCustomFiltersAndFunctions()
    {
        var request = new DataPageRequest(new RqlRequest(), limit: 50, offset: 10, countAll: true);

        request.Limit.Should().Be(50);
        request.Offset.Should().Be(10);
        request.CountAll.Should().BeTrue();
        request.CustomFilters.Get().Should().BeEmpty();
        request.CustomFunctions.Get().Should().BeEmpty();
    }

    [Fact]
    public void FullConstructor_KeepsTheCustomCollections()
    {
        var filters = new CustomFilters().Add("status,active");
        var functions = new CustomFunctions().AddFunction("normalize", ["lower"]);

        var request = new DataPageRequest(new RqlRequest(), 10, 0, false, filters, functions);

        request.CustomFilters.Should().BeSameAs(filters);
        request.CustomFunctions.Should().BeSameAs(functions);
    }

    [Fact]
    public void CustomFilters_Add_ParsesCommaSeparatedNameAndArguments()
    {
        var filters = new CustomFilters().Add("status, active, pending");

        filters.Get().Should().ContainSingle();
        var filter = filters.Get().Single();
        filter.Key.Should().Be("status");
        filter.Args.Should().BeEquivalentTo(["active", "pending"]);
    }

    [Fact]
    public void CustomFilters_Add_LowercasesTheFilterName()
    {
        var filters = new CustomFilters().Add("Status,Active");

        filters.Get().Single().Key.Should().Be("status");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CustomFilters_Add_IgnoresBlankNames(string? name)
    {
        var filters = new CustomFilters().Add(name!);

        filters.Get().Should().BeEmpty();
    }

    [Fact]
    public void CustomFunctions_AddFunction_StoresLowercasedKeyAndArgs()
    {
        var functions = new CustomFunctions().AddFunction("Normalize", ["TrimEnd", "ToLower"]);

        var fn = functions.Get().Single();
        fn.Key.Should().Be("normalize");
        fn.Args.Should().BeEquivalentTo(["TrimEnd", "ToLower"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CustomFunctions_AddFunction_IgnoresBlankNames(string? name)
    {
        var functions = new CustomFunctions().AddFunction(name!, ["x"]);

        functions.Get().Should().BeEmpty();
    }
}
