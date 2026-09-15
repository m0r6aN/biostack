namespace BioStack.Api.Tests.Auth;

using BioStack.Api.Auth;
using Xunit;

/// <summary>
/// Owner feedback 2026-09-15, finding #1: the sign-in email subject line read
/// "Your BioStack sign-in link" with nothing asserting it, so a copy change could silently
/// drift. This locks the checked-in fallback to the approved subject. A deployed
/// Smtp__MagicLinkSubject or AzureCommunicationEmail__MagicLinkSubject environment value
/// still overrides this default in production — that override is intentionally out of this
/// test's reach, since it lives in Azure configuration, not source control.
/// </summary>
public sealed class MagicLinkSubjectsTests
{
    [Fact]
    public void Default_IsTheApprovedQuickLoginSubject()
    {
        Assert.Equal("BioStack Quick Login", MagicLinkSubjects.Default);
    }
}
