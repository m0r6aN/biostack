namespace BioStack.Api.Tests.Security;

using BioStack.Api.Security;
using Xunit;

public sealed class AuthStartAccountBudgetTests
{
    [Fact]
    public void TryConsume_AllowsThreeThenRejectsUntilWindowExpires()
    {
        var budget = new AuthStartAccountBudget();
        var now = DateTime.UtcNow;

        Assert.True(budget.TryConsume("user@example.com", now));
        Assert.True(budget.TryConsume("user@example.com", now));
        Assert.True(budget.TryConsume("user@example.com", now));
        Assert.False(budget.TryConsume("user@example.com", now));
        Assert.True(budget.TryConsume("other@example.com", now));
        Assert.True(budget.TryConsume("user@example.com", now.Add(AuthStartAccountBudget.Window)));
    }
}
