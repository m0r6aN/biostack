namespace BioStack.Api.Tests.Security;

using BioStack.Api.Security;
using Xunit;

public sealed class LogRedactionTests
{
    [Theory]
    [InlineData("User@Example.com", "example.com")]
    [InlineData("missing-at", "invalid")]
    [InlineData("", "missing")]
    public void EmailDomain_ReturnsDomainOnly(string email, string expected)
        => Assert.Equal(expected, LogRedaction.EmailDomain(email));
}
