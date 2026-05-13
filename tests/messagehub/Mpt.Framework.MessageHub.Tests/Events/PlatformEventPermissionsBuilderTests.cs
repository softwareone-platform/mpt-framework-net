using System.ComponentModel.DataAnnotations;
using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests.Events;

public class PlatformEventPermissionsBuilderTests
{
    [Fact]
    public void AddAccountPrincipalAccess_RegistersAccountPrincipal()
    {
        var builder = new PlatformEventPermissionsBuilder();

        builder.AddAccountPrincipalAccess("acct-1", "Tenant", name: "Acme", icon: "icon.png");

        builder.PrincipalAccess.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new EventMessagePrincipalAccess
            {
                Id = "acct-1",
                Name = "Acme",
                Icon = "icon.png",
                PrincipalType = EventMessagePrincipalType.Account,
                AccountType = "Tenant",
            });
    }

    [Fact]
    public void AddUserPrincipalAccess_RegistersUserPrincipal()
    {
        var builder = new PlatformEventPermissionsBuilder();

        builder.AddUserPrincipalAccess("user-1", name: "Alice");

        builder.PrincipalAccess.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new EventMessagePrincipalAccess
            {
                Id = "user-1",
                Name = "Alice",
                Icon = null,
                PrincipalType = EventMessagePrincipalType.User,
                AccountType = null,
            });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAccountPrincipalAccess_RejectsBlankId(string id)
    {
        var builder = new PlatformEventPermissionsBuilder();

        var act = () => builder.AddAccountPrincipalAccess(id, "Tenant");

        act.Should().Throw<ValidationException>().WithMessage("*Account ID*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddUserPrincipalAccess_RejectsBlankId(string id)
    {
        var builder = new PlatformEventPermissionsBuilder();

        var act = () => builder.AddUserPrincipalAccess(id);

        act.Should().Throw<ValidationException>().WithMessage("*User ID*");
    }

    [Fact]
    public void Clear_EmptiesPrincipalList()
    {
        var builder = new PlatformEventPermissionsBuilder()
            .AddAccountPrincipalAccess("acct-1", "Tenant")
            .AddUserPrincipalAccess("user-1");

        builder.Clear();

        builder.PrincipalAccess.Should().BeEmpty();
    }

    [Fact]
    public void CompletedTask_ReturnsCompletedValueTask()
    {
        var builder = new PlatformEventPermissionsBuilder();

        var result = builder.CompletedTask();

        result.IsCompletedSuccessfully.Should().BeTrue();
    }
}
